using CoursVia.Data;
using CoursVia.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers;

// Eğitmen panelinde kurs bölümlerini ekleme, düzenleme, silme ve sıralama işlemlerini yönetir.
[Authorize(Roles = "Eğitmen")]
public class EgitmenBolumController : EgitmenBaseController
{
    public EgitmenBolumController(AppDbContext context) : base(context)
    {
    }

    // Eğitmenin kendi kursuna yeni bölüm eklemesini sağlar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BolumEkle(int kursId, string bolumAdi)
    {
        int kullaniciId = AktifKullaniciId;

        var kurs = await _context.Kurslar
            .Include(x => x.Bolumler)
            .FirstOrDefaultAsync(x => x.KursId == kursId && x.EgitmenId == kullaniciId);

        if (kurs == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Kurs onay bekliyorsa veya düzenlemeye kapalı durumdaysa bölüm eklenemez.
        if (!KursDuzenlenebilirMi(kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil (Örn: Onay bekliyor).";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
        }

        if (string.IsNullOrWhiteSpace(bolumAdi))
        {
            TempData["KursHata"] = "Bölüm adı boş olamaz.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
        }

        bolumAdi = bolumAdi.Trim();

        // Aynı kurs içinde aynı isimde bölüm oluşması engellenir.
        if (kurs.Bolumler.Any(x => x.BolumAdi.Equals(bolumAdi, StringComparison.OrdinalIgnoreCase)))
        {
            TempData["KursHata"] = "Bu kursta aynı isimde bir bölüm zaten mevcut.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
        }

        int yeniSiraNo = 1;

        // Yeni bölüm mevcut bölümlerin sonuna eklenir.
        if (kurs.Bolumler.Any())
        {
            yeniSiraNo = kurs.Bolumler.Max(x => x.SiraNo) + 1;
        }

        var yeniBolum = new Bolum
        {
            KursId = kursId,
            BolumAdi = bolumAdi,
            SiraNo = yeniSiraNo
        };

        _context.Bolumler.Add(yeniBolum);

        // Yayındaki kurs düzenlenirse tekrar taslak durumuna alınır.
        OnayliKursuTaslakYap(kurs);
        kurs.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["KursBasari"] = "Bölüm başarıyla eklendi.";
        return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
    }

    // Eğitmenin kendi kursundaki bölüm adını düzenlemesini sağlar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BolumDuzenle(int bolumId, string bolumAdi)
    {
        int kullaniciId = AktifKullaniciId;

        var bolum = await _context.Bolumler
            .Include(x => x.Kurs)
            .FirstOrDefaultAsync(x => x.BolumId == bolumId && x.Kurs.EgitmenId == kullaniciId);

        if (bolum == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!KursDuzenlenebilirMi(bolum.Kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = bolum.KursId });
        }

        if (string.IsNullOrWhiteSpace(bolumAdi))
        {
            TempData["KursHata"] = "Bölüm adı boş olamaz.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = bolum.KursId });
        }

        bolumAdi = bolumAdi.Trim();

        if (bolumAdi.Length > 100)
        {
            TempData["KursHata"] = "Bölüm adı en fazla 100 karakter olabilir.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = bolum.KursId });
        }

        // Aynı kurs içinde başka bir bölümün aynı adı kullanması engellenir.
        bool ayniIsimdeBaskaBolumVarMi = await _context.Bolumler
            .AnyAsync(x =>
                x.KursId == bolum.KursId &&
                x.BolumId != bolum.BolumId &&
                x.BolumAdi.ToLower() == bolumAdi.ToLower());

        if (ayniIsimdeBaskaBolumVarMi)
        {
            TempData["KursHata"] = "Bu kursta aynı isimde bir bölüm zaten mevcut.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = bolum.KursId });
        }

        // Bölüm adı gerçekten değiştiyse güncelleme yapılır.
        if (!string.Equals(bolum.BolumAdi, bolumAdi, StringComparison.Ordinal))
        {
            bolum.BolumAdi = bolumAdi;

            OnayliKursuTaslakYap(bolum.Kurs);
            bolum.Kurs.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["KursBasari"] = "Bölüm adı başarıyla güncellendi.";
        }

        return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = bolum.KursId });
    }

    // Eğitmenin kendi kursundaki boş bir bölümü silmesini sağlar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BolumSil(int bolumId)
    {
        int kullaniciId = AktifKullaniciId;

        var bolum = await _context.Bolumler
            .Include(x => x.Kurs)
            .Include(x => x.Dersler)
            .FirstOrDefaultAsync(x => x.BolumId == bolumId && x.Kurs.EgitmenId == kullaniciId);

        if (bolum == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!KursDuzenlenebilirMi(bolum.Kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil .";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = bolum.KursId });
        }

        // İçinde ders olan bölümler doğrudan silinmez.
        if (bolum.Dersler.Any())
        {
            TempData["KursHata"] = "İçerisinde ders bulunan bölümler silinemez. Önce dersleri silmeli veya başka bir bölüme taşımalısınız.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = bolum.KursId });
        }

        int kursId = bolum.KursId;
        var kurs = bolum.Kurs;

        _context.Bolumler.Remove(bolum);
        await _context.SaveChangesAsync();

        var digerBolumler = await _context.Bolumler
            .Where(x => x.KursId == kursId)
            .OrderBy(x => x.SiraNo)
            .ToListAsync();

        int yeniSira = 1;

        // Silme işleminden sonra kalan bölümlerin sıra numaraları yeniden düzenlenir.
        foreach (var b in digerBolumler)
        { 
            b.SiraNo = yeniSira++;
        }

        OnayliKursuTaslakYap(kurs);
        kurs.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["KursBasari"] = "Bölüm başarıyla silindi.";
        return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
    }

    // Bölümlerin sürükle-bırak gibi işlemlerle AJAX üzerinden yeniden sıralanmasını sağlar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BolumSiralaAjax([FromBody] List<int> bolumIds)
    {
        if (bolumIds == null || !bolumIds.Any())
        {
            return BadRequest(new { success = false, message = "Bölüm listesi boş olamaz." });
        }

        // Aynı bölüm Id'sinin tekrar gönderilmesi engellenir.
        bolumIds = bolumIds.Distinct().ToList();

        int kullaniciId = AktifKullaniciId;
        var ilkBolumId = bolumIds.First();

        var kurs = await _context.Kurslar
            .Include(x => x.Bolumler)
            .FirstOrDefaultAsync(x => x.EgitmenId == kullaniciId && x.Bolumler.Any(b => b.BolumId == ilkBolumId));

        if (kurs == null)
        {
            return NotFound(new { success = false, message = "Kurs bulunamadı veya yetkiniz yok." });
        }

        if (!KursDuzenlenebilirMi(kurs.DurumId))
        {
            return BadRequest(new { success = false, message = "Bu kurs şu an düzenlenebilir durumda değil." });
        }

        var guncellenecekBolumler = kurs.Bolumler
            .Where(x => bolumIds.Contains(x.BolumId))
            .ToList();

        // Gönderilen bölüm Id'lerinden herhangi biri bu kursa ait değilse işlem durdurulur.
        if (guncellenecekBolumler.Count != bolumIds.Count)
        {
            return BadRequest(new { success = false, message = "Geçersiz bölüm tespit edildi." });
        }

        // Unique sıra çakışması yaşanmaması için önce geçici negatif sıra değerleri atanır.
        foreach (var b in guncellenecekBolumler)
        {
            b.SiraNo = -b.BolumId;
        }

        await _context.SaveChangesAsync();

        int sira = 1;

        // Gelen Id sırasına göre bölümlere yeni sıra numarası verilir.
        foreach (var id in bolumIds)
        {
            var b = guncellenecekBolumler.First(x => x.BolumId == id);
            b.SiraNo = sira++;
        }

        OnayliKursuTaslakYap(kurs);
        kurs.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }
}