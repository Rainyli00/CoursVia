using CoursVia.Data;
using CoursVia.Models;
using CoursVia.ViewModels.Ogrenci;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers;

// Öğrencinin kayıtlı olduğu kurslardaki dersleri izlemesini ve ders ilerlemesini yönetir.
[Authorize(Roles = "Öğrenci")]
public class OgrenciDersController : Controller
{
    private readonly AppDbContext _context;

    public OgrenciDersController(AppDbContext context)
    {
        _context = context;
    }

    // Öğrencinin seçilen kurs içerisindeki dersi izlemesini sağlar.
    [HttpGet]
    public async Task<IActionResult> Izle(int kursId, int? dersId)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Öğrencinin bu kursa aktif kayıtlı olup olmadığı kontrol edilir.
        var kursKaydi = await _context.KursKayitlari
            .AsNoTracking()
            .Include(x => x.Kurs)
                .ThenInclude(x => x.Egitmen)
            .Include(x => x.DersIlerlemeleri)
            .FirstOrDefaultAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == kursId &&
                x.AktifMi);

        if (kursKaydi == null)
        {
            TempData["OgrenciHata"] = "Bu kursa kayıtlı değilsiniz.";
            return RedirectToAction("Kesfet", "OgrenciKurs");
        }

        // Kurs düzeltme/güncelleme durumundaysa öğrencinin izleme ekranına erişmesi engellenir.
        if (kursKaydi.Kurs.DurumId == 7)
        {
            TempData["OgrenciHata"] = "Bu kurs şu anda güncelleniyor.";
            return RedirectToAction("Kurslarim", "OgrenciKurs");
        }

        // Kursa ait bölümler ve bölümlerin aktif normal dersleri sıralı şekilde alınır.
        var bolumler = await _context.Bolumler
            .AsNoTracking()
            .Where(x => x.KursId == kursId)
            .OrderBy(x => x.SiraNo)
            .Select(x => new
            {
                x.BolumId,
                x.BolumAdi,
                x.SiraNo,

                Dersler = x.Dersler
                    .Where(d => d.AktifMi && !d.SistemDersiMi)
                    .OrderBy(d => d.SiraNo)
                    .Select(d => new
                    {
                        d.DersId,
                        d.DersAdi,
                        d.Aciklama,
                        d.VideoUrl,
                        d.SiraNo
                    })
                    .ToList()
            })
            .ToListAsync();

        // Tüm bölümlerdeki dersler tek listeye alınır.
        var tumDersler = bolumler
            .SelectMany(x => x.Dersler)
            .OrderBy(x => x.SiraNo)
            .ToList();

        if (!tumDersler.Any())
        {
            TempData["OgrenciHata"] = "Bu kurs için izlenebilir ders bulunmuyor.";
            return RedirectToAction("Kurslarim", "OgrenciKurs");
        }

        // Öğrencinin daha önce tamamladığı derslerin Id bilgileri alınır.
        var tamamlananDersIdleri = kursKaydi.DersIlerlemeleri
            .Where(x => x.TamamlandiMi)
            .Select(x => x.DersId)
            .ToHashSet();

        // Eğer dersId gönderildiyse o ders seçilir.
        // Gönderilmediyse öğrencinin tamamlamadığı ilk ders seçilir.
        // Tüm dersler tamamlandıysa ilk ders seçilir.
        var seciliDers = dersId.HasValue
            ? tumDersler.FirstOrDefault(x => x.DersId == dersId.Value)
            : tumDersler.FirstOrDefault(x => !tamamlananDersIdleri.Contains(x.DersId)) ?? tumDersler.First();

        if (seciliDers == null)
        {
            TempData["OgrenciHata"] = "Ders bulunamadı.";
            return RedirectToAction("Kurslarim", "OgrenciKurs");
        }

        // Seçili dersin listedeki sırası bulunur.
        int seciliIndex = tumDersler.FindIndex(x => x.DersId == seciliDers.DersId);

        // Önceki ders varsa Id değeri hazırlanır.
        int? oncekiDersId = seciliIndex > 0
            ? tumDersler[seciliIndex - 1].DersId
            : null;

        // Sonraki ders varsa Id değeri hazırlanır.
        int? sonrakiDersId = seciliIndex < tumDersler.Count - 1
            ? tumDersler[seciliIndex + 1].DersId
            : null;

        int toplamDersSayisi = tumDersler.Count;

        // Sadece bu kursun aktif dersleri içinde tamamlanan ders sayısı hesaplanır.
        int tamamlananDersSayisi = tamamlananDersIdleri.Count(x =>
            tumDersler.Any(d => d.DersId == x));

        // Öğrencinin kurs ilerleme yüzdesi hesaplanır.
        // Toplam ders sayısı sıfır ise ilerleme yüzdesi 0 olarak atanır, aksi halde tamamlanan derslerin toplam ders sayısına oranı yüzdeye çevrilir ve yuvarlanır.
        int ilerlemeYuzdesi = toplamDersSayisi == 0
            ? 0 
            : (int)Math.Round((tamamlananDersSayisi * 100.0) / toplamDersSayisi); 

        // Seçili derse ait materyaller alınır.
        var materyaller = await _context.DersMateryalleri
            .AsNoTracking()
            .Where(x => x.DersId == seciliDers.DersId)
            .OrderBy(x => x.MateryalId)
            .Select(x => new OgrenciDersMateryalViewModel
            {
                DersMateryalId = x.MateryalId,
                MateryalAdi = x.Baslik,
                DosyaUrl = x.MateryalUrl,
                MateryalTipiAdi = x.MateryalTipi.MateryalTipAdi
            })
            .ToListAsync();

        // Kursa ait sınav olup olmadığı kontrol edilir.
        var sinavBilgisi = await _context.Sinavlar
            .AsNoTracking()
            .Where(x => x.KursId == kursId)
            .Select(x => new
            {
                x.SinavId
            })
            .FirstOrDefaultAsync();

        bool sinavVarMi = sinavBilgisi != null;

        bool sinavGecildiMi = false;

        // Sınav varsa öğrencinin bu sınavdan geçip geçmediği kontrol edilir.
        if (sinavBilgisi != null)
        {
            sinavGecildiMi = await _context.SinavKatilimlari
                .AsNoTracking()
                .AnyAsync(x =>
                    x.KursKayitId == kursKaydi.KursKayitId &&
                    x.SinavId == sinavBilgisi.SinavId &&
                    x.BitisTarihi != null &&
                    x.GectiMi == true);
        }

        // Ders izleme ekranında gösterilecek tüm bilgiler ViewModel'e aktarılır.
        var model = new OgrenciDersIzleViewModel
        {
            KursId = kursId,
            KursKayitId = kursKaydi.KursKayitId,

            KursAdi = kursKaydi.Kurs.KursAdi,
            EgitmenAdSoyad = $"{kursKaydi.Kurs.Egitmen.Ad} {kursKaydi.Kurs.Egitmen.Soyad}".Trim(),

            SeciliDersId = seciliDers.DersId,
            SeciliDersAdi = seciliDers.DersAdi,
            SeciliDersAciklama = seciliDers.Aciklama,
            VideoUrl = seciliDers.VideoUrl,

            SeciliDersTamamlandiMi = tamamlananDersIdleri.Contains(seciliDers.DersId),

            ToplamDersSayisi = toplamDersSayisi,
            TamamlananDersSayisi = tamamlananDersSayisi,
            IlerlemeYuzdesi = ilerlemeYuzdesi,

            OncekiDersId = oncekiDersId,
            SonrakiDersId = sonrakiDersId,

            TumDerslerTamamlandiMi = toplamDersSayisi > 0 &&
                                      tamamlananDersSayisi == toplamDersSayisi,

            SinavVarMi = sinavVarMi,
            SinavGecildiMi = sinavGecildiMi,

            Bolumler = bolumler
                .Select(b => new OgrenciDersBolumViewModel
                {
                    BolumId = b.BolumId,
                    BolumAdi = b.BolumAdi,
                    SiraNo = b.SiraNo,

                    Dersler = b.Dersler
                        .OrderBy(d => d.SiraNo)
                        .Select(d => new OgrenciDersListeItemViewModel
                        {
                            DersId = d.DersId,
                            DersAdi = d.DersAdi,
                            SiraNo = d.SiraNo,
                            TamamlandiMi = tamamlananDersIdleri.Contains(d.DersId),
                            SeciliMi = d.DersId == seciliDers.DersId
                        })
                        .ToList()
                })
                .ToList(),

            Materyaller = materyaller
        };

        return View(model);
    }

    // Öğrencinin izlediği dersi tamamlandı olarak işaretler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DersiTamamla(int kursId, int dersId)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Öğrencinin bu kursa aktif kaydı olup olmadığı kontrol edilir.
        var kursKaydi = await _context.KursKayitlari
            .Include(x => x.Kurs)
            .FirstOrDefaultAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == kursId &&
                x.AktifMi);

        if (kursKaydi == null)
        {
            TempData["OgrenciHata"] = "Bu kursa kayıtlı değilsiniz.";
            return RedirectToAction("Kesfet", "OgrenciKurs");
        }

        if (kursKaydi.Kurs.DurumId == 7)
        {
            TempData["OgrenciHata"] = "Bu kurs şu anda güncelleniyor.";
            return RedirectToAction("Kurslarim", "OgrenciKurs");
        }

        // Tamamlanacak dersin bu kursa ait aktif normal bir ders olduğu doğrulanır.
        bool dersGecerliMi = await _context.Dersler
            .AnyAsync(x =>
                x.DersId == dersId &&
                x.KursId == kursId &&
                x.AktifMi &&
                !x.SistemDersiMi);

        if (!dersGecerliMi)
        {
            TempData["OgrenciHata"] = "Ders bulunamadı veya artık aktif değil.";
            return RedirectToAction(nameof(Izle), new { kursId });
        }

        // Bu ders için daha önce ilerleme kaydı oluşturulmuş mu kontrol edilir.
        var dersIlerlemesi = await _context.DersIlerlemeleri
            .FirstOrDefaultAsync(x =>
                x.KursKayitId == kursKaydi.KursKayitId &&
                x.DersId == dersId);

        if (dersIlerlemesi == null)
        {
            // İlk kez tamamlanıyorsa yeni ilerleme kaydı oluşturulur.
            dersIlerlemesi = new DersIlerlemesi
            {
                KursKayitId = kursKaydi.KursKayitId,
                DersId = dersId,
                TamamlandiMi = true
            };

            _context.DersIlerlemeleri.Add(dersIlerlemesi);
        }
        else
        {
            // Daha önce kayıt varsa tekrar tamamlandı olarak işaretlenir.
            dersIlerlemesi.TamamlandiMi = true;
        }

        await _context.SaveChangesAsync();

        TempData["OgrenciBasari"] = "Ders tamamlandı olarak işaretlendi.";
        return RedirectToAction(nameof(Izle), new { kursId, dersId });
    }
}