using CoursVia.Data;
using CoursVia.Models;
using CoursVia.Services.Ai;
using CoursVia.ViewModels.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers;

[Authorize(Roles = "Eğitmen")]
public class EgitmenAiController : EgitmenBaseController
{
    private readonly AiAnalizService _aiAnalizService;
    private readonly AiOneriService _aiOneriService;

    public EgitmenAiController(
        AppDbContext context,
        AiAnalizService aiAnalizService,
        AiOneriService aiOneriService) : base(context)
    {
        _aiAnalizService = aiAnalizService;
        _aiOneriService = aiOneriService;
    }

    [HttpGet]
    public async Task<IActionResult> Oneriler(CancellationToken cancellationToken = default)
    {
        var egitmenId = AktifKullaniciId;

        var kurslar = await _context.Kurslar
            .AsNoTracking()
            .Where(k => k.EgitmenId == egitmenId)
            .Select(k => new EgitmenAiKursSecimItemViewModel
            {
                KursId = k.KursId,
                KursAdi = k.KursAdi,
                DurumAdi = k.Durum.DurumAdi,
                OlusturmaTarihi = k.OlusturmaTarihi,
                SinavVarMi = k.Sinav != null
            })
            .OrderByDescending(x => x.OlusturmaTarihi)
            .ToListAsync(cancellationToken);

        var gecmisOneriler = await _context.Oneriler
            .AsNoTracking()
            .Where(o => o.KullaniciId == egitmenId)
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

        var viewModel = new EgitmenAiOnerilerViewModel
        {
            Kurslar = kurslar,
            GecmisOneriler = gecmisOneriler
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KursAnaliziAjax(
        int kursId,
        AiModelTipi modelTipi = AiModelTipi.Gemini,
        CancellationToken cancellationToken = default)
    {
        var egitmenId = AktifKullaniciId;

        var kurs = await _context.Kurslar
            .AsNoTracking()
            .Include(k => k.Sinav)
            .FirstOrDefaultAsync(k =>
                k.KursId == kursId &&
                k.EgitmenId == egitmenId,
                cancellationToken);

        if (kurs == null)
        {
            return Json(new
            {
                basarili = false,
                hata = "Kurs bulunamadı veya bu kurs için yetkiniz yok."
            });
        }

        var aiVerisi = await EgitmenAiVerisiHazirlaAsync(
            kurs,
            cancellationToken);

        var sonuclar = await _aiAnalizService.EgitmenKursAnaliziAsync(
            aiVerisi,
            modelTipi,
            cancellationToken);

        await _aiOneriService.OnerileriKaydetAsync(
            kullaniciId: egitmenId,
            kursId: kurs.KursId,
            oneriTipAdi: "Eğitmen Kurs Analizi",
            sonuclar: sonuclar,
            cancellationToken: cancellationToken);

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

        return Json(new
        {
            basarili = true,
            sonuclar = sonuclarJson
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OneriSil(int oneriId, CancellationToken cancellationToken = default)
    {
        var egitmenId = AktifKullaniciId;

        var silindiMi = await _aiOneriService.OneriSilAsync(
            kullaniciId: egitmenId,
            oneriId: oneriId,
            cancellationToken: cancellationToken);

        if (!silindiMi)
        {
            return NotFound(new { basarili = false, mesaj = "Silinecek öneri bulunamadı." });
        }

        return Json(new { basarili = true, mesaj = "Öneri silindi." });
    }

    [HttpGet]
    public async Task<IActionResult> KursAnalizi(
        int kursId,
        AiModelTipi modelTipi = AiModelTipi.Gemini,
        CancellationToken cancellationToken = default)
    {
        var egitmenId = AktifKullaniciId;

        var kurs = await _context.Kurslar
            .AsNoTracking()
            .Include(k => k.Sinav)
            .FirstOrDefaultAsync(k =>
                k.KursId == kursId &&
                k.EgitmenId == egitmenId,
                cancellationToken);

        if (kurs == null)
        {
            TempData["Hata"] = "Kurs bulunamadı veya bu kurs için yetkiniz yok.";
            return RedirectToAction("Kurslarim", "EgitmenKurs");
        }

        var aiVerisi = await EgitmenAiVerisiHazirlaAsync(
            kurs,
            cancellationToken);

        var sonuclar = await _aiAnalizService.EgitmenKursAnaliziAsync(
            aiVerisi,
            modelTipi,
            cancellationToken);

        await _aiOneriService.OnerileriKaydetAsync(
            kullaniciId: egitmenId,
            kursId: kurs.KursId,
            oneriTipAdi: "Eğitmen Kurs Analizi",
            sonuclar: sonuclar,
            cancellationToken: cancellationToken);

        var viewModel = new AiKarsilastirmaViewModel
        {
            IstekTipi = AiIstekTipi.EgitmenKursAnalizi,
            SecilenModelTipi = modelTipi,
            KursId = kurs.KursId,
            Baslik = $"{kurs.KursAdi} - AI Kurs Analizi",
            Aciklama = "Kurs performans verilerine göre oluşturulan eğitmen önerileri.",
            Sonuclar = sonuclar.Select(x => new AiModelSonucViewModel
            {
                ModelTipi = x.ModelTipi,
                ModelAdi = x.ModelAdi,
                BasariliMi = x.BasariliMi,
                Cikti = x.TemizCikti,
                HataMesaji = x.HataMesaji,
                SureMs = x.SureMs,
                GuvenlikFiltresiUygulandiMi = x.GuvenlikFiltresiUygulandiMi
            }).ToList()
        };

        return View(viewModel);
    }

    private static string FormatSure(long sureMs)
    {
        if (sureMs <= 0)
            return "-";

        if (sureMs < 1000)
            return $"{sureMs} ms";

        return $"{sureMs / 1000.0:0.00} sn";
    }

    private async Task<AiEgitmenKursAnalizVerisi> EgitmenAiVerisiHazirlaAsync(
        Kurs kurs,
        CancellationToken cancellationToken)
    {
        var toplamOgrenciSayisi = await _context.KursKayitlari
            .AsNoTracking()
            .CountAsync(x =>
                x.KursId == kurs.KursId &&
                x.AktifMi,
                cancellationToken);

        var tamamlayanOgrenciSayisi = await _context.KursKayitlari
            .AsNoTracking()
            .CountAsync(x =>
                x.KursId == kurs.KursId &&
                x.AktifMi &&
                x.TamamlandiMi,
                cancellationToken);

        var genelTamamlanmaOrani = toplamOgrenciSayisi > 0
            ? Math.Round((decimal)tamamlayanOgrenciSayisi * 100 / toplamOgrenciSayisi, 2)
            : 0;

        var ortalamaPuan = await _context.KursDegerlendirmeleri
            .AsNoTracking()
            .Where(x => x.KursId == kurs.KursId)
            .Select(x => (decimal?)x.Puan)
            .AverageAsync(cancellationToken) ?? 0;

        ortalamaPuan = Math.Round(ortalamaPuan, 2);

        var zorlanilanBolum = await _context.OgrenciCevaplari
            .AsNoTracking()
            .Where(x =>
                x.SinavKatilimi.Sinav.KursId == kurs.KursId &&
                !x.DogruMu)
            .SelectMany(x => x.Soru.SoruDersleri.Select(sd => new
            {
                sd.Ders.BolumId,
                sd.Ders.Bolum.BolumAdi
            }))
            .GroupBy(x => new
            {
                x.BolumId,
                x.BolumAdi
            })
            .Select(g => new
            {
                g.Key.BolumAdi,
                YanlisSayisi = g.Count()
            })
            .OrderByDescending(x => x.YanlisSayisi)
            .Select(x => x.BolumAdi)
            .FirstOrDefaultAsync(cancellationToken);

        var dersToplamCevapSayilari = await _context.OgrenciCevaplari
            .AsNoTracking()
            .Where(x => x.SinavKatilimi.Sinav.KursId == kurs.KursId)
            .SelectMany(x => x.Soru.SoruDersleri.Select(sd => sd.DersId))
            .GroupBy(dersId => dersId)
            .Select(g => new
            {
                DersId = g.Key,
                ToplamCevapSayisi = g.Count()
            })
            .ToDictionaryAsync(
                x => x.DersId,
                x => x.ToplamCevapSayisi,
                cancellationToken);

        var zorlanilanDersler = await _context.OgrenciCevaplari
            .AsNoTracking()
            .Where(x =>
                x.SinavKatilimi.Sinav.KursId == kurs.KursId &&
                !x.DogruMu)
            .SelectMany(x => x.Soru.SoruDersleri.Select(sd => new
            {
                sd.DersId,
                sd.Ders.DersAdi,
                sd.Ders.Bolum.BolumAdi
            }))
            .GroupBy(x => new
            {
                x.DersId,
                x.DersAdi,
                x.BolumAdi
            })
            .Select(g => new AiZorlanilanDersVerisi
            {
                DersId = g.Key.DersId,
                DersAdi = g.Key.DersAdi,
                BolumAdi = g.Key.BolumAdi,
                YanlisSayisi = g.Count(),
                YanlisOrani = 0
            })
            .OrderByDescending(x => x.YanlisSayisi)
            .Take(5)
            .ToListAsync(cancellationToken);

        foreach (var ders in zorlanilanDersler)
        {
            if (dersToplamCevapSayilari.TryGetValue(ders.DersId, out var toplamCevap) &&
                toplamCevap > 0)
            {
                ders.YanlisOrani = Math.Round((decimal)ders.YanlisSayisi * 100 / toplamCevap, 2);
            }
        }

        if (string.IsNullOrWhiteSpace(zorlanilanBolum))
        {
            zorlanilanBolum = "Belirgin zorlanılan bölüm bulunamadı";
        }

        if (!zorlanilanDersler.Any())
        {
            zorlanilanDersler.Add(new AiZorlanilanDersVerisi
            {
                DersId = 0,
                DersAdi = "Belirgin zorlanılan ders bulunamadı",
                BolumAdi = zorlanilanBolum,
                YanlisSayisi = 0,
                YanlisOrani = 0
            });
        }

        return new AiEgitmenKursAnalizVerisi
        {
            KursId = kurs.KursId,
            KursAdi = kurs.KursAdi,
            ToplamOgrenciSayisi = toplamOgrenciSayisi,
            OrtalamaPuan = ortalamaPuan,
            GenelTamamlanmaOrani = genelTamamlanmaOrani,
            ZorlanilanBolum = zorlanilanBolum,
            ZorlanilanDersler = zorlanilanDersler
        };
    }

}
