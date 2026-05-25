using CoursVia.Data;
using CoursVia.Models;
using CoursVia.ViewModels.Egitmen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers;

[Authorize(Roles = "Eğitmen")]
public class EgitmenSinavController : EgitmenBaseController
{
    private const int SoruHavuzuSayfaBoyutu = 10;

    public EgitmenSinavController(AppDbContext context)
        : base(context)
    {
    }

    [HttpGet]
    public async Task<IActionResult> SinavHazirla(int kursId)
    {
        int kullaniciId = AktifKullaniciId;

        var kurs = await _context.Kurslar
            .Include(x => x.Sinav)
            .FirstOrDefaultAsync(x => x.KursId == kursId && x.EgitmenId == kullaniciId);

        if (kurs == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!KursDuzenlenebilirMi(kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
        }

        int havuzdakiSoruSayisi = kurs.Sinav == null
            ? 0
            : await _context.Sorular.CountAsync(x =>
                x.SinavId == kurs.Sinav.SinavId &&
                x.AktifMi);

        var model = new SinavHazirlaViewModel
        {
            KursId = kurs.KursId,
            KursAdi = kurs.KursAdi,

            SinavId = kurs.Sinav?.SinavId,
            SinavAdi = kurs.Sinav?.SinavAdi ?? $"{kurs.KursAdi} Final Sınavı",
            Aciklama = kurs.Sinav?.Aciklama,

            GecmeNotu = kurs.Sinav?.GecmeNotu ?? 70,
            SureDakika = kurs.Sinav?.SureDakika ?? 30,
            SoruSayisi = kurs.Sinav?.SoruSayisi ?? 10,

            HavuzdakiSoruSayisi = havuzdakiSoruSayisi
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SinavHazirla(SinavHazirlaViewModel model)
    {
        int kullaniciId = AktifKullaniciId;

        var kurs = await _context.Kurslar
            .Include(x => x.Sinav)
            .FirstOrDefaultAsync(x => x.KursId == model.KursId && x.EgitmenId == kullaniciId);

        if (kurs == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!KursDuzenlenebilirMi(kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = model.KursId });
        }

        model.SinavAdi = model.SinavAdi?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(model.SinavAdi))
        {
            ModelStateHatasiYoksaEkle(
                nameof(model.SinavAdi),
                "Sınav adı zorunludur."
            );
        }

        if (!ModelState.IsValid)
        {
            model.KursAdi = kurs.KursAdi;
            model.SinavId = kurs.Sinav?.SinavId;
            model.HavuzdakiSoruSayisi = kurs.Sinav == null
                ? 0
                : await _context.Sorular.CountAsync(x =>
                    x.SinavId == kurs.Sinav.SinavId &&
                    x.AktifMi);

            return View(model);
        }

        if (kurs.Sinav == null)
        {
            var yeniSinav = new Sinav
            {
                KursId = kurs.KursId,
                SinavAdi = model.SinavAdi,
                Aciklama = string.IsNullOrWhiteSpace(model.Aciklama) ? null : model.Aciklama.Trim(),
                GecmeNotu = model.GecmeNotu,
                SureDakika = model.SureDakika,
                SoruSayisi = model.SoruSayisi,
                OlusturmaTarihi = DateTime.Now
            };

            _context.Sinavlar.Add(yeniSinav);
        }
        else
        {
            kurs.Sinav.SinavAdi = model.SinavAdi;
            kurs.Sinav.Aciklama = string.IsNullOrWhiteSpace(model.Aciklama) ? null : model.Aciklama.Trim();
            kurs.Sinav.GecmeNotu = model.GecmeNotu;
            kurs.Sinav.SureDakika = model.SureDakika;
            kurs.Sinav.SoruSayisi = model.SoruSayisi;
        }

        OnayliKursuTaslakYap(kurs);
        kurs.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["KursBasari"] = "Sınav ayarları başarıyla kaydedildi.";

        return RedirectToAction(nameof(SoruHavuzu), new { kursId = kurs.KursId });
    }

    [HttpGet]
    public async Task<IActionResult> SoruHavuzu(int kursId, string? arama, int? dersId, int sayfa = 1)
    {
        int kullaniciId = AktifKullaniciId;
        const int sayfaBoyutu = SoruHavuzuSayfaBoyutu;

        arama = string.IsNullOrWhiteSpace(arama) ? null : arama.Trim();
        sayfa = Math.Max(1, sayfa);

        var sinav = await _context.Sinavlar
            .AsNoTracking()
            .Include(x => x.Kurs)
            .FirstOrDefaultAsync(x => x.KursId == kursId);

        if (sinav == null)
        {
            bool kursVarMi = await _context.Kurslar
                .AnyAsync(x => x.KursId == kursId && x.EgitmenId == kullaniciId);

            if (!kursVarMi)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            TempData["KursHata"] = "Soru havuzuna geçmeden önce sınav ayarlarını oluşturmalısınız.";
            return RedirectToAction(nameof(SinavHazirla), new { kursId });
        }

        if (sinav.Kurs.EgitmenId != kullaniciId)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (dersId.HasValue)
        {
            bool dersGecerliMi = await _context.Dersler
                .AsNoTracking()
                .AnyAsync(x =>
                    x.DersId == dersId.Value &&
                    x.KursId == sinav.KursId &&
                    x.AktifMi &&
                    !x.SistemDersiMi);

            if (!dersGecerliMi)
            {
                dersId = null;
            }
        }

        int havuzdakiSoruSayisi = await _context.Sorular
            .AsNoTracking()
            .CountAsync(x =>
                x.SinavId == sinav.SinavId &&
                x.AktifMi);

        var soruQuery = _context.Sorular
            .AsNoTracking()
            .Where(x =>
                x.SinavId == sinav.SinavId);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            string aramaMetni = $"%{arama}%";

            soruQuery = soruQuery.Where(x =>
                EF.Functions.Like(x.SoruMetni, aramaMetni));
        }

        if (dersId.HasValue)
        {
            soruQuery = soruQuery.Where(x =>
                x.SoruDersleri.Any(sd => sd.DersId == dersId.Value));
        }

        int toplamSoruSayisi = await soruQuery.CountAsync();
        int toplamSayfa = toplamSoruSayisi == 0
            ? 1
            : (int)Math.Ceiling(toplamSoruSayisi / (double)sayfaBoyutu);

        sayfa = Math.Min(sayfa, toplamSayfa);

        var soruIdleri = await soruQuery
            .OrderByDescending(x => x.SoruId)
            .Skip((sayfa - 1) * sayfaBoyutu)
            .Take(sayfaBoyutu)
            .Select(x => x.SoruId)
            .ToListAsync();

        var soruSiralari = soruIdleri
            .Select((id, index) => new { id, index })
            .ToDictionary(x => x.id, x => x.index);

        var sorular = soruIdleri.Any()
            ? await _context.Sorular
                .AsNoTracking()
                .Include(x => x.SoruSecenekleri)
                .Include(x => x.SoruDersleri)
                    .ThenInclude(x => x.Ders)
                        .ThenInclude(x => x.Bolum)
                .AsSplitQuery()
                .Where(x =>
                    soruIdleri.Contains(x.SoruId))
                .ToListAsync()
            : new List<Soru>();

        var model = new SoruHavuzuViewModel
        {
            KursId = sinav.KursId,
            SinavId = sinav.SinavId,
            KursAdi = sinav.Kurs.KursAdi,
            SinavAdi = sinav.SinavAdi,
            GecmeNotu = sinav.GecmeNotu,
            SureDakika = sinav.SureDakika,
            SoruSayisi = sinav.SoruSayisi,
            HavuzdakiSoruSayisi = havuzdakiSoruSayisi,
            KursDuzenlenebilirMi = KursDuzenlenebilirMi(sinav.Kurs.DurumId),
            Arama = arama,
            DersId = dersId,
            Sayfa = sayfa,
            SayfaBoyutu = sayfaBoyutu,
            ToplamSoruSayisi = toplamSoruSayisi,
            Dersler = await DersSecimListesiniGetirAsync(sinav.KursId),
            Sorular = sorular
                .OrderBy(x => soruSiralari[x.SoruId])
                .Select(x => new SoruHavuzuSoruViewModel
                {
                    SoruId = x.SoruId,
                    SoruMetni = x.SoruMetni,
                    AktifMi = x.AktifMi,
                    SecenekSayisi = x.SoruSecenekleri.Count(s => s.AktifMi),
                    DogruSecenekSayisi = x.SoruSecenekleri.Count(s => s.AktifMi && s.DogruMu),
                    DersBaglantilariGecerliMi =
                        x.SoruDersleri.Any(sd =>
                            sd.Ders.KursId == sinav.KursId &&
                            sd.Ders.AktifMi &&
                            !sd.Ders.SistemDersiMi) &&
                        !x.SoruDersleri.Any(sd =>
                            sd.Ders.KursId != sinav.KursId ||
                            !sd.Ders.AktifMi ||
                            sd.Ders.SistemDersiMi),
                    DersAdlari = x.SoruDersleri
                        .Where(sd =>
                            sd.Ders.KursId == sinav.KursId &&
                            sd.Ders.AktifMi &&
                            !sd.Ders.SistemDersiMi)
                        .OrderBy(sd => sd.Ders.Bolum.SiraNo)
                        .ThenBy(sd => sd.Ders.SiraNo)
                        .Select(sd => $"{sd.Ders.Bolum.BolumAdi} / {sd.Ders.DersAdi}")
                        .ToList()
                })
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SoruEkle(int kursId)
    {
        int kullaniciId = AktifKullaniciId;

        var sinav = await _context.Sinavlar
            .Include(x => x.Kurs)
            .FirstOrDefaultAsync(x => x.KursId == kursId);

        if (sinav == null)
        {
            bool kursVarMi = await _context.Kurslar
                .AnyAsync(x => x.KursId == kursId && x.EgitmenId == kullaniciId);

            if (!kursVarMi)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            TempData["KursHata"] = "Soru eklemeden önce sınav ayarlarını oluşturmalısınız.";
            return RedirectToAction(nameof(SinavHazirla), new { kursId });
        }

        if (sinav.Kurs.EgitmenId != kullaniciId)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!KursDuzenlenebilirMi(sinav.Kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction(nameof(SoruHavuzu), new { kursId });
        }

        var model = await SoruEkleModeliOlusturAsync(sinav);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoruEkle(SoruEkleViewModel model)
    {
        int kullaniciId = AktifKullaniciId;

        var sinav = await _context.Sinavlar
            .Include(x => x.Kurs)
            .FirstOrDefaultAsync(x =>
                x.SinavId == model.SinavId &&
                x.KursId == model.KursId);

        if (sinav == null || sinav.Kurs.EgitmenId != kullaniciId)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!KursDuzenlenebilirMi(sinav.Kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction(nameof(SoruHavuzu), new { kursId = sinav.KursId });
        }

        var secenekMetinleri = await SoruFormunuDogrulaAsync(model, sinav.KursId);

        if (!ModelState.IsValid)
        {
            await SoruEkleModeliniDoldurAsync(model, sinav);
            return View(model);
        }

        int dogruSecenekIndex = model.DogruSecenekIndex.GetValueOrDefault();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var soru = new Soru
            {
                SinavId = sinav.SinavId,
                SoruMetni = model.SoruMetni!.Trim(),
                AktifMi = true
            };

            _context.Sorular.Add(soru);
            await _context.SaveChangesAsync();

            for (int i = 0; i < secenekMetinleri.Count; i++)
            {
                _context.SoruSecenekleri.Add(new SoruSecenegi
                {
                    SoruId = soru.SoruId,
                    SecenekMetni = secenekMetinleri[i],
                    DogruMu = i == dogruSecenekIndex,
                    AktifMi = true
                });
            }

            foreach (var dersId in model.SeciliDersIdleri)
            {
                _context.SoruDersleri.Add(new SoruDersi
                {
                    SoruId = soru.SoruId,
                    DersId = dersId
                });
            }

            OnayliKursuTaslakYap(sinav.Kurs);
            sinav.Kurs.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["KursBasari"] = "Soru havuza başarıyla eklendi.";
            return RedirectToAction(nameof(SoruHavuzu), new { kursId = sinav.KursId });
        }
        catch
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError("", "Soru kaydedilirken bir hata oluştu. Lütfen tekrar deneyin.");
            await SoruEkleModeliniDoldurAsync(model, sinav);

            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> SoruDuzenle(int soruId)
    {
        int kullaniciId = AktifKullaniciId;

        var soru = await _context.Sorular
            .Include(x => x.Sinav)
                .ThenInclude(x => x.Kurs)
            .Include(x => x.SoruSecenekleri)
            .Include(x => x.SoruDersleri)
                .ThenInclude(x => x.Ders)
            .FirstOrDefaultAsync(x =>
                x.SoruId == soruId);

        if (soru == null || soru.Sinav.Kurs.EgitmenId != kullaniciId)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!KursDuzenlenebilirMi(soru.Sinav.Kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction(nameof(SoruHavuzu), new { kursId = soru.Sinav.KursId });
        }

        var secenekler = soru.SoruSecenekleri
            .OrderBy(x => x.SecenekId)
            .ToList();

        int? dogruSecenekIndex = null;

        for (int i = 0; i < secenekler.Count; i++)
        {
            if (secenekler[i].DogruMu)
            {
                dogruSecenekIndex = i;
                break;
            }
        }

        var model = new SoruDuzenleViewModel
        {
            SoruId = soru.SoruId,
            SoruMetni = soru.SoruMetni,
            SecenekMetinleri = secenekler
                .Select(x => x.SecenekMetni)
                .ToList(),
            SecenekIdleri = secenekler
                .Select(x => x.SecenekId)
                .ToList(),
            SecenekAktifDurumlari = secenekler
                .Select(x => x.AktifMi)
                .ToList(),
            DogruSecenekIndex = dogruSecenekIndex,
            SeciliDersIdleri = soru.SoruDersleri
                .Where(x =>
                    x.Ders.KursId == soru.Sinav.KursId &&
                    x.Ders.AktifMi &&
                    !x.Ders.SistemDersiMi)
                .Select(x => x.DersId)
                .Distinct()
                .ToList()
        };

        await SoruEkleModeliniDoldurAsync(model, soru.Sinav);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoruDuzenle(SoruDuzenleViewModel model)
    {
        int kullaniciId = AktifKullaniciId;

        var soru = await _context.Sorular
            .Include(x => x.Sinav)
                .ThenInclude(x => x.Kurs)
            .Include(x => x.SoruSecenekleri)
            .Include(x => x.SoruDersleri)
            .FirstOrDefaultAsync(x =>
                x.SoruId == model.SoruId &&
                x.SinavId == model.SinavId);

        if (soru == null || soru.Sinav.Kurs.EgitmenId != kullaniciId)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!KursDuzenlenebilirMi(soru.Sinav.Kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction(nameof(SoruHavuzu), new { kursId = soru.Sinav.KursId });
        }

        model.KursId = soru.Sinav.KursId;
        model.SinavId = soru.SinavId;

        var secenekMetinleri = await SoruFormunuDogrulaAsync(model, soru.Sinav.KursId);
        SecenekIdleriniDogrula(model, soru);

        if (!ModelState.IsValid)
        {
            await SoruEkleModeliniDoldurAsync(model, soru.Sinav);
            return View(model);
        }

        int dogruSecenekIndex = model.DogruSecenekIndex.GetValueOrDefault();
        bool soruKullanildiMi = await SoruKullanildiMiAsync(soru.SoruId);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            soru.SoruMetni = model.SoruMetni!.Trim();

            _context.SoruDersleri.RemoveRange(soru.SoruDersleri);

            var tumSecenekler = soru.SoruSecenekleri.ToDictionary(x => x.SecenekId);
            var formdanGelenSecenekIdleri = new HashSet<int>();

            foreach (var secenek in tumSecenekler.Values)
            {
                secenek.DogruMu = false;
            }
            await _context.SaveChangesAsync();

            for (int i = 0; i < secenekMetinleri.Count; i++)
            {
                int secenekId = model.SecenekIdleri[i];
                bool dogruMu = i == dogruSecenekIndex;
                bool aktifMi = model.SecenekAktifDurumlari[i];

                if (secenekId > 0 && tumSecenekler.TryGetValue(secenekId, out var mevcutSecenek))
                {
                    mevcutSecenek.SecenekMetni = secenekMetinleri[i];
                    mevcutSecenek.DogruMu = dogruMu;
                    mevcutSecenek.AktifMi = aktifMi;
                    formdanGelenSecenekIdleri.Add(secenekId);
                }
                else
                {
                    _context.SoruSecenekleri.Add(new SoruSecenegi
                    {
                        SoruId = soru.SoruId,
                        SecenekMetni = secenekMetinleri[i],
                        DogruMu = dogruMu,
                        AktifMi = aktifMi
                    });
                }
            }

            var silinecekSecenekler = tumSecenekler.Values
                .Where(x => !formdanGelenSecenekIdleri.Contains(x.SecenekId))
                .ToList();

            if (soruKullanildiMi)
            {
                foreach (var sec in silinecekSecenekler)
                {
                    sec.AktifMi = false;
                }
            }
            else
            {
                _context.SoruSecenekleri.RemoveRange(silinecekSecenekler);
            }

            foreach (var dersId in model.SeciliDersIdleri)
            {
                _context.SoruDersleri.Add(new SoruDersi
                {
                    SoruId = soru.SoruId,
                    DersId = dersId
                });
            }

            OnayliKursuTaslakYap(soru.Sinav.Kurs);
            soru.Sinav.Kurs.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["KursBasari"] = "Soru başarıyla güncellendi.";
            return RedirectToAction(nameof(SoruHavuzu), new { kursId = soru.Sinav.KursId });
        }
        catch
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError("", "Soru güncellenirken bir hata oluştu. Lütfen tekrar deneyin.");
            await SoruEkleModeliniDoldurAsync(model, soru.Sinav);

            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoruSil(int soruId)
    {
        int kullaniciId = AktifKullaniciId;

        var soru = await _context.Sorular
            .Include(x => x.Sinav)
                .ThenInclude(x => x.Kurs)
            .Include(x => x.SoruSecenekleri)
            .Include(x => x.SoruDersleri)
            .FirstOrDefaultAsync(x =>
                x.SoruId == soruId);

        if (soru == null || soru.Sinav.Kurs.EgitmenId != kullaniciId)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!KursDuzenlenebilirMi(soru.Sinav.Kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction(nameof(SoruHavuzu), new { kursId = soru.Sinav.KursId });
        }

        bool soruKullanildiMi = await SoruKullanildiMiAsync(soru.SoruId);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (soruKullanildiMi)
            {
                soru.AktifMi = false;
            }
            else
            {
                _context.SoruDersleri.RemoveRange(soru.SoruDersleri);
                _context.SoruSecenekleri.RemoveRange(soru.SoruSecenekleri);
                _context.Sorular.Remove(soru);
            }

            OnayliKursuTaslakYap(soru.Sinav.Kurs);
            soru.Sinav.Kurs.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["KursBasari"] = soruKullanildiMi
                ? "Soru havuzdan kaldırıldı."
                : "Soru başarıyla silindi.";
            return RedirectToAction(nameof(SoruHavuzu), new { kursId = soru.Sinav.KursId });
        }
        catch
        {
            await transaction.RollbackAsync();

            TempData["KursHata"] = "Soru silinirken bir hata oluştu. Lütfen tekrar deneyin.";
            return RedirectToAction(nameof(SoruHavuzu), new { kursId = soru.Sinav.KursId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoruDurumDegistir(int soruId)
    {
        int kullaniciId = AktifKullaniciId;

        var soru = await _context.Sorular
            .Include(x => x.Sinav)
                .ThenInclude(x => x.Kurs)
            .FirstOrDefaultAsync(x => x.SoruId == soruId);

        if (soru == null || soru.Sinav.Kurs.EgitmenId != kullaniciId)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!KursDuzenlenebilirMi(soru.Sinav.Kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction(nameof(SoruHavuzu), new { kursId = soru.Sinav.KursId });
        }

        soru.AktifMi = !soru.AktifMi;
        
        OnayliKursuTaslakYap(soru.Sinav.Kurs);
        soru.Sinav.Kurs.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["KursBasari"] = soru.AktifMi ? "Soru aktif duruma getirildi." : "Soru pasife alındı.";
        return RedirectToAction(nameof(SoruHavuzu), new { kursId = soru.Sinav.KursId });
    }

    private async Task<SoruEkleViewModel> SoruEkleModeliOlusturAsync(Sinav sinav)
    {
        var model = new SoruEkleViewModel();

        await SoruEkleModeliniDoldurAsync(model, sinav);

        return model;
    }

    private async Task SoruEkleModeliniDoldurAsync(SoruEkleViewModel model, Sinav sinav)
    {
        model.KursId = sinav.KursId;
        model.SinavId = sinav.SinavId;
        model.KursAdi = sinav.Kurs.KursAdi;
        model.SinavAdi = sinav.SinavAdi;
        model.Dersler = await DersSecimListesiniGetirAsync(sinav.KursId);

        model.SecenekMetinleri ??= new List<string>();
    }

    private async Task<List<string>> SoruFormunuDogrulaAsync(SoruEkleViewModel model, int kursId)
    {
        model.SoruMetni = model.SoruMetni?.Trim();
        model.SeciliDersIdleri = model.SeciliDersIdleri?
            .Where(x => x > 0)
            .Distinct()
            .ToList() ?? new List<int>();

        var secenekMetinleri = model.SecenekMetinleri?
            .Select(x => x?.Trim() ?? string.Empty)
            .ToList() ?? new List<string>();

        model.SecenekMetinleri = secenekMetinleri;
        model.SecenekIdleri = model.SecenekIdleri?
            .Take(secenekMetinleri.Count)
            .Select(x => Math.Max(0, x))
            .ToList() ?? new List<int>();

        while (model.SecenekIdleri.Count < secenekMetinleri.Count)
        {
            model.SecenekIdleri.Add(0);
        }

        model.SecenekAktifDurumlari ??= new List<bool>();
        while (model.SecenekAktifDurumlari.Count < secenekMetinleri.Count)
        {
            model.SecenekAktifDurumlari.Add(true);
        }

        if (string.IsNullOrWhiteSpace(model.SoruMetni))
        {
            ModelStateHatasiYoksaEkle(
                nameof(model.SoruMetni),
                "Soru metni zorunludur."
            );
        }

        int aktifSecenekSayisi = 0;
        bool aktifDogruSecenekVarMi = false;

        for (int i = 0; i < secenekMetinleri.Count; i++)
        {
            if (model.SecenekAktifDurumlari[i])
            {
                aktifSecenekSayisi++;
                if (model.DogruSecenekIndex == i)
                {
                    aktifDogruSecenekVarMi = true;
                }
            }
        }

        if (aktifSecenekSayisi < 2)
        {
            ModelState.AddModelError(
                nameof(model.SecenekMetinleri),
                "En az iki aktif seçenek olmalıdır."
            );
        }

        if (!aktifDogruSecenekVarMi)
        {
            ModelState.AddModelError(
                nameof(model.DogruSecenekIndex),
                "Aktif seçenekler arasından tam olarak bir doğru seçenek seçmelisiniz."
            );
        }

        if (secenekMetinleri.Any(string.IsNullOrWhiteSpace))
        {
            ModelState.AddModelError(
                nameof(model.SecenekMetinleri),
                "Seçenek metinleri boş olamaz."
            );
        }

        if (!model.SeciliDersIdleri.Any())
        {
            ModelState.AddModelError(
                nameof(model.SeciliDersIdleri),
                "Soru en az bir derse bağlı olmalıdır."
            );

            return secenekMetinleri;
        }

        int gecerliDersSayisi = await _context.Dersler
            .AsNoTracking()
            .CountAsync(x =>
                model.SeciliDersIdleri.Contains(x.DersId) &&
                x.KursId == kursId &&
                x.AktifMi &&
                !x.SistemDersiMi);

        if (gecerliDersSayisi != model.SeciliDersIdleri.Count)
        {
            ModelState.AddModelError(
                nameof(model.SeciliDersIdleri),
                "Seçilen derslerden biri geçersiz, pasif veya sistem dersidir."
            );
        }

        return secenekMetinleri;
    }

    private void SecenekIdleriniDogrula(SoruDuzenleViewModel model, Soru soru)
    {
        var tumSecenekIdleri = soru.SoruSecenekleri
            .Select(x => x.SecenekId)
            .ToHashSet();

        bool tekrarEdenSecenekIdVarMi = model.SecenekIdleri
            .Where(x => x > 0)
            .GroupBy(x => x)
            .Any(x => x.Count() > 1);

        if (tekrarEdenSecenekIdVarMi)
        {
            ModelState.AddModelError(
                nameof(model.SecenekMetinleri),
                "Seçenek bilgisi geçersiz. Lütfen sayfayı yenileyip tekrar deneyin."
            );
        }

        bool gecersizSecenekIdVarMi = model.SecenekIdleri
            .Any(x => x > 0 && !tumSecenekIdleri.Contains(x));

        if (gecersizSecenekIdVarMi)
        {
            ModelState.AddModelError(
                nameof(model.SecenekMetinleri),
                "Seçenek bilgisi geçersiz. Lütfen sayfayı yenileyip tekrar deneyin."
            );
        }
    }

    private void ModelStateHatasiYoksaEkle(string key, string errorMessage)
    {
        if (!ModelState.TryGetValue(key, out var modelStateEntry) ||
            modelStateEntry.Errors.Count == 0)
        {
            ModelState.AddModelError(key, errorMessage);
        }
    }

    private async Task<bool> SoruKullanildiMiAsync(int soruId)
    {
        return await _context.OgrenciCevaplari
            .AnyAsync(x => x.SoruId == soruId);
    }

    private async Task<List<SoruDersSecimViewModel>> DersSecimListesiniGetirAsync(int kursId)
    {
        return await _context.Dersler
            .Include(x => x.Bolum)
            .Where(x =>
                x.KursId == kursId &&
                x.AktifMi &&
                !x.SistemDersiMi)
            .OrderBy(x => x.Bolum.SiraNo)
            .ThenBy(x => x.SiraNo)
            .Select(x => new SoruDersSecimViewModel
            {
                DersId = x.DersId,
                DersAdi = x.DersAdi,
                BolumAdi = x.Bolum.BolumAdi
            })
            .ToListAsync();
    }
}
