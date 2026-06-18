using System.Security.Claims;
using CoursVia.Data;
using CoursVia.Models;
using CoursVia.Services.Ai;
using CoursVia.ViewModels.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers;

// Öğrencinin sınav sonuçlarına göre yapay zekadan çalışma önerisi almasını yönetir.
[Authorize(Roles = "Öğrenci")]
public class OgrenciAiController : Controller
{
    private readonly AppDbContext _context;
    private readonly AiAnalizService _aiAnalizService;
    private readonly AiOneriService _aiOneriService;

    public OgrenciAiController(
        AppDbContext context,
        AiAnalizService aiAnalizService,
        AiOneriService aiOneriService)
    {
        _context = context;
        _aiAnalizService = aiAnalizService;
        _aiOneriService = aiOneriService;
    }

    // Öğrencinin tamamladığı sınavları ve daha önce aldığı AI önerilerini listeler.
    [HttpGet]
    public async Task<IActionResult> Oneriler(CancellationToken cancellationToken = default)
    {
        var ogrenciId = AktifKullaniciIdGetir();

        if (ogrenciId == null)
            return RedirectToAction("OgrenciLogin", "Account");

        // Öğrencinin tamamlanmış, bitiş tarihi ve puanı olan sınavları alınır.
        var tamamlananSinavlar = await _context.SinavKatilimlari
            .AsNoTracking()
            .Where(x =>
                x.KursKaydi.KullaniciId == ogrenciId.Value &&
                x.BitisTarihi != null &&
                x.AlinanPuan != null)
            .Select(x => new OgrenciAiSinavSecimItemViewModel
            {
                SinavKatilimId = x.SinavKatilimId,
                KursAdi = x.Sinav.Kurs.KursAdi,
                SinavAdi = x.Sinav.SinavAdi,
                AlinanPuan = x.AlinanPuan ?? 0,
                GecmeNotu = x.Sinav.GecmeNotu,
                BitisTarihi = x.BitisTarihi ?? DateTime.MinValue
            })
            .OrderByDescending(x => x.BitisTarihi)
            .ToListAsync(cancellationToken);

        // Öğrencinin daha önce aldığı AI önerileri geçmiş olarak listelenir.
        var gecmisOneriler = await _context.Oneriler
            .AsNoTracking()
            .Where(o => o.KullaniciId == ogrenciId.Value)
            .Include(o => o.OneriTipi)
            .Include(o => o.Kurs)
            .OrderByDescending(o => o.OlusturmaTarihi)
            .Select(o => new EgitmenGecmisOneriViewModel
            {
                OneriId = o.OneriId,
                KursAdi = o.Kurs != null ? o.Kurs.KursAdi : "Bilinmeyen Kurs",
                OneriTipAdi = o.OneriTipi.OneriTipAdi,
                OneriMetni = o.OneriMetni,
                OlusturmaTarihi = o.OlusturmaTarihi
            })
            .ToListAsync(cancellationToken);

        // Sayfada kullanılacak tamamlanan sınavlar ve öneri geçmişi ViewModel'e aktarılır.
        var viewModel = new OgrenciAiOnerilerViewModel
        {
            TamamlananSinavlar = tamamlananSinavlar,
            GecmisOneriler = gecmisOneriler
        };

        return View(viewModel);
    }

    // Seçilen sınav sonucuna göre AJAX üzerinden AI çalışma önerisi üretir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CalismaOnerisiAjax(
        int sinavKatilimId,
        AiModelTipi modelTipi = AiModelTipi.Gemini,// İsteğe bağlı olarak farklı AI modeli seçilebilir, default Gemini'dir.
        CancellationToken cancellationToken = default)
    {
        var ogrenciId = AktifKullaniciIdGetir();

        if (ogrenciId == null)
            return Json(new { basarili = false, hata = "Oturum bulunamadı." });

        // Sınav katılımının giriş yapan öğrenciye ait olup olmadığı kontrol edilir.
        var sinavKatilimi = await _context.SinavKatilimlari
            .AsNoTracking()
            .Include(x => x.KursKaydi)
            .Include(x => x.Sinav)
                .ThenInclude(x => x.Kurs)
            .FirstOrDefaultAsync(x =>
                x.SinavKatilimId == sinavKatilimId &&
                x.KursKaydi.KullaniciId == ogrenciId.Value,
                cancellationToken);

        if (sinavKatilimi == null)
            return Json(new { basarili = false, hata = "Sınav sonucu bulunamadı veya yetkiniz yok." });

        // AI önerisi sadece tamamlanmış ve puanı oluşmuş sınavlar için üretilir.
        if (sinavKatilimi.BitisTarihi == null || sinavKatilimi.AlinanPuan == null)
            return Json(new { basarili = false, hata = "Sınavın tamamlanmış olması gerekir." });

        // AI servisine gönderilecek öğrenci sınav analizi verisi hazırlanır.
        var aiVerisi = await OgrenciAiVerisiHazirlaAsync(sinavKatilimi, cancellationToken);

        // Seçilen AI modeliyle çalışma önerisi üretilir.
        var sonuclar = await _aiAnalizService.OgrenciCalismaOnerisiAsync(
            aiVerisi, modelTipi, cancellationToken);

        // Üretilen AI önerileri daha sonra geçmişte gösterilebilmesi için veritabanına kaydedilir.
        await _aiOneriService.OnerileriKaydetAsync(
            kullaniciId: ogrenciId.Value,
            kursId: sinavKatilimi.Sinav.KursId,
            oneriTipAdi: "Öğrenci Çalışma Önerisi",
            sonuclar: sonuclar,
            cancellationToken: cancellationToken);

        // AJAX tarafına dönecek sade JSON sonuç listesi hazırlanır.
        var sonuclarJson = sonuclar.Select(x => new
        {
            modelTipi = x.ModelTipi.ToString(),
            modelAdi = x.ModelAdi,
            basariliMi = x.BasariliMi,
            cikti = x.TemizCikti,
            hataMesaji = x.HataMesaji,
            sureMs = x.SureMs,
            guvenlikFiltresiUygulandiMi = x.GuvenlikFiltresiUygulandiMi,
            sureText = FormatSure(x.SureMs)
        }).ToList();

        return Json(new { basarili = true, sonuclar = sonuclarJson });
    }

    // Öğrencinin kendi AI önerisini silmesini sağlar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OneriSil(int oneriId, CancellationToken cancellationToken = default)
    {
        var ogrenciId = AktifKullaniciIdGetir();

        if (ogrenciId == null)
        {
            return Unauthorized(new { basarili = false, mesaj = "Oturum bulunamadı." });
        }

        // Silme işlemi service tarafında kullanıcı Id ile kontrol edilerek yapılır.
        var silindiMi = await _aiOneriService.OneriSilAsync(
            kullaniciId: ogrenciId.Value,
            oneriId: oneriId,
            cancellationToken: cancellationToken);

        if (!silindiMi)
        {
            return NotFound(new { basarili = false, mesaj = "Silinecek öneri bulunamadı." });
        }

        return Json(new { basarili = true, mesaj = "Öneri silindi." });
    }

    // AI modelinin cevap üretme süresini okunabilir metne çevirir.
    private static string FormatSure(long sureMs)
    {
        if (sureMs <= 0) return "-";
        if (sureMs < 1000) return $"{sureMs} ms";
        return $"{sureMs / 1000.0:0.00} sn";
    }

    // Öğrencinin sınav sonucundan AI çalışma önerisi için gerekli analiz verisini hazırlar.
    private async Task<AiOgrenciCalismaVerisi> OgrenciAiVerisiHazirlaAsync(
        SinavKatilimi sinavKatilimi,
        CancellationToken cancellationToken)
    {
        // Öğrencinin yanlış yaptığı soruların en çok hangi bölümde yoğunlaştığı bulunur.
        var yanlislarinYogunlastigiBolum = await _context.OgrenciCevaplari
            .AsNoTracking()
            .Where(x =>
                x.SinavKatilimId == sinavKatilimi.SinavKatilimId &&
                !x.DogruMu)
            .SelectMany(x => x.Soru.SoruDersleri.Select(sd => new
            {
                sd.Ders.BolumId,
                sd.Ders.Bolum.BolumAdi
            }))
            .GroupBy(x => new { x.BolumId, x.BolumAdi })
            .Select(g => new { g.Key.BolumAdi, YanlisSayisi = g.Count() })
            .OrderByDescending(x => x.YanlisSayisi)
            .Select(x => x.BolumAdi)
            .FirstOrDefaultAsync(cancellationToken);

        // Yanlış yapılan soruların bağlı olduğu dersler yanlış sayısına göre listelenir.
        var yanlisYapilanDersler = await _context.OgrenciCevaplari
            .AsNoTracking()
            .Where(x =>
                x.SinavKatilimId == sinavKatilimi.SinavKatilimId &&
                !x.DogruMu)
            .SelectMany(x => x.Soru.SoruDersleri.Select(sd => new
            {
                sd.DersId,
                sd.Ders.DersAdi,
                sd.Ders.Bolum.BolumAdi
            }))
            .GroupBy(x => new { x.DersId, x.DersAdi, x.BolumAdi })
            .Select(g => new AiYanlisDersVerisi
            {
                DersId = g.Key.DersId,
                DersAdi = g.Key.DersAdi,
                BolumAdi = g.Key.BolumAdi,
                YanlisSayisi = g.Count()
            })
            .OrderByDescending(x => x.YanlisSayisi)
            .Take(5)
            .ToListAsync(cancellationToken);

        // Belirgin bir bölüm bulunamazsa AI çıktısında boş veri gitmemesi için varsayılan metin atanır.
        if (string.IsNullOrWhiteSpace(yanlislarinYogunlastigiBolum))
            yanlislarinYogunlastigiBolum = "Belirgin yoğunlaşan bölüm bulunamadı";

        // Yanlış ders bulunamadıysa yine AI için anlamlı bir varsayılan kayıt eklenir.
        if (!yanlisYapilanDersler.Any())
        {
            yanlisYapilanDersler.Add(new AiYanlisDersVerisi
            {
                DersId = 0,
                DersAdi = "Belirgin yanlış yapılan ders bulunamadı",
                BolumAdi = yanlislarinYogunlastigiBolum,
                YanlisSayisi = 0
            });
        }

        // AI analiz servisine gönderilecek ana veri nesnesi hazırlanır.
        return new AiOgrenciCalismaVerisi
        {
            SinavKatilimId = sinavKatilimi.SinavKatilimId,
            KursId = sinavKatilimi.Sinav.KursId,
            KursAdi = sinavKatilimi.Sinav.Kurs.KursAdi,
            SinavPuani = sinavKatilimi.AlinanPuan ?? 0,
            GecmePuani = sinavKatilimi.Sinav.GecmeNotu,
            YanlislarinYogunlastigiBolum = yanlislarinYogunlastigiBolum,
            YanlisYapilanDersler = yanlisYapilanDersler
        };
    }

    // Claim içinden giriş yapan kullanıcının Id değerini güvenli şekilde alır.
    private int? AktifKullaniciIdGetir()
    {
        var kullaniciIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(kullaniciIdText, out var kullaniciId))
            return kullaniciId;

        return null;
    }
}