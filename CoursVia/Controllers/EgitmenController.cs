using CoursVia.ViewModels.Egitmen.Ogrencilerim;
using CoursVia.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers;

[Authorize(Roles = "Eğitmen")]
public class EgitmenController : Controller
{
    private readonly AppDbContext _context;

    public EgitmenController(AppDbContext context)
    {
        _context = context;
    }

    // =========================
    // EĞİTMEN DASHBOARD
    // =========================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var egitmenProfili = await _context.EgitmenProfilleri
            .Include(x => x.Kullanici)
            .Include(x => x.EgitmenBranslari)
                .ThenInclude(x => x.Kategori)
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (egitmenProfili == null || egitmenProfili.DurumId != 8)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        ViewBag.YayindakiKurs = await _context.Kurslar
            .CountAsync(x => x.EgitmenId == kullaniciId && x.DurumId == 5);

        ViewBag.OnayBekleyenKurs = await _context.Kurslar
            .CountAsync(x => x.EgitmenId == kullaniciId && x.DurumId == 4);

        ViewBag.TaslakKurs = await _context.Kurslar
            .CountAsync(x => x.EgitmenId == kullaniciId && x.DurumId == 3);

        ViewBag.SonKurslar = await _context.Kurslar
            .Include(x => x.Durum)
            .Include(x => x.KursKategorileri)
                .ThenInclude(x => x.Kategori)
            .Where(x => x.EgitmenId == kullaniciId)
            .OrderByDescending(x => x.OlusturmaTarihi)
            .Take(5)
            .ToListAsync();

        ViewBag.SonKayitlar = await _context.KursKayitlari
            .Include(x => x.Kullanici)
            .Include(x => x.Kurs)
            .Where(x => x.Kurs.EgitmenId == kullaniciId && x.AktifMi)
            .OrderByDescending(x => x.KayitTarihi)
            .Take(5)
            .ToListAsync();

        return View(egitmenProfili);
    }

    // =========================
    // DİĞER EĞİTMEN SAYFALARI
    // =========================

    [HttpGet]
    public async Task<IActionResult> Ogrencilerim(string? arama, int? kursId, int sayfa = 1)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        const int sayfaBasinaKayit = 10;

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        if (sayfa < 1)
        {
            sayfa = 1;
        }

        var kurslar = await _context.Kurslar
            .AsNoTracking()
            .Where(x => x.EgitmenId == kullaniciId)
            .OrderBy(x => x.KursAdi)
            .Select(x => new KursSecimViewModel
            {
                KursId = x.KursId,
                KursAdi = x.KursAdi
            })
            .ToListAsync();

        var query = _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.Kurs.EgitmenId == kullaniciId &&
                x.AktifMi);

        if (kursId.HasValue)
        {
            query = query.Where(x => x.KursId == kursId.Value);
        }

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.Kullanici.Ad.Contains(arama) ||
                x.Kullanici.Soyad.Contains(arama) ||
                (x.Kullanici.Ad + " " + x.Kullanici.Soyad).Contains(arama));
        }

        int toplamKayit = await query.CountAsync();

        int toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);

        if (toplamSayfa < 1)
        {
            toplamSayfa = 1;
        }

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        var tumOgrenciOzetleri = await query
            .Select(x => new OgrencilerimListeItemViewModel
            {
                KursKayitId = x.KursKayitId,

                ToplamDersSayisi = _context.Dersler
                    .Count(d =>
                        d.KursId == x.KursId &&
                        d.AktifMi &&
                        !d.SistemDersiMi),

                TamamlananDersSayisi = _context.DersIlerlemeleri
                    .Count(i =>
                        i.KursKayitId == x.KursKayitId &&
                        i.TamamlandiMi &&
                        i.Ders.AktifMi &&
                        !i.Ders.SistemDersiMi)
            })
            .ToListAsync();

        var tumKursKayitIdleri = tumOgrenciOzetleri
            .Select(x => x.KursKayitId)
            .ToList();

        var tumSinavKatilimlari = await _context.SinavKatilimlari
            .AsNoTracking()
            .Where(x => tumKursKayitIdleri.Contains(x.KursKayitId))
            .OrderByDescending(x => x.BaslamaTarihi)
            .Select(x => new
            {
                x.KursKayitId,
                x.BaslamaTarihi,
                x.BitisTarihi,
                x.AlinanPuan,
                x.GectiMi
            })
            .ToListAsync();

        var tumSonSinavlar = tumSinavKatilimlari
            .GroupBy(x => x.KursKayitId)
            .ToDictionary(
                x => x.Key,
                x => x.First()
            );

        var tumSinavGirisSayilari = tumSinavKatilimlari
            .GroupBy(x => x.KursKayitId)
            .ToDictionary(
                x => x.Key,
                x => x.Count()
            );

        foreach (var ogrenci in tumOgrenciOzetleri)
        {
            ogrenci.IlerlemeYuzdesi = ogrenci.ToplamDersSayisi == 0
                ? 0
                : (int)Math.Round((ogrenci.TamamlananDersSayisi * 100.0) / ogrenci.ToplamDersSayisi);

            ogrenci.SinavGirisSayisi = tumSinavGirisSayilari.TryGetValue(ogrenci.KursKayitId, out int girisSayisi)
                ? girisSayisi
                : 0;

            if (tumSonSinavlar.TryGetValue(ogrenci.KursKayitId, out var sonSinav))
            {
                ogrenci.SinavdanGectiMi = sonSinav.GectiMi;
            }
        }

        var ogrenciler = await query
            .OrderByDescending(x => x.KayitTarihi)
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new OgrencilerimListeItemViewModel
            {
                KursKayitId = x.KursKayitId,
                OgrenciId = x.KullaniciId,

                OgrenciAdSoyad = x.Kullanici.Ad + " " + x.Kullanici.Soyad,

                KursId = x.KursId,
                KursAdi = x.Kurs.KursAdi,

                KayitTarihi = x.KayitTarihi,

                ToplamDersSayisi = _context.Dersler
                    .Count(d =>
                        d.KursId == x.KursId &&
                        d.AktifMi &&
                        !d.SistemDersiMi),

                TamamlananDersSayisi = _context.DersIlerlemeleri
                    .Count(i =>
                        i.KursKayitId == x.KursKayitId &&
                        i.TamamlandiMi &&
                        i.Ders.AktifMi &&
                        !i.Ders.SistemDersiMi)
            })
            .ToListAsync();

        var sayfadakiKursKayitIdleri = ogrenciler
            .Select(x => x.KursKayitId)
            .ToList();

        var sayfadakiSinavKatilimlari = tumSinavKatilimlari
            .Where(x => sayfadakiKursKayitIdleri.Contains(x.KursKayitId))
            .ToList();

        var sonSinavlar = sayfadakiSinavKatilimlari
            .GroupBy(x => x.KursKayitId)
            .ToDictionary(
                x => x.Key,
                x => x.First()
            );

        var sinavGirisSayilari = sayfadakiSinavKatilimlari
            .GroupBy(x => x.KursKayitId)
            .ToDictionary(
                x => x.Key,
                x => x.Count()
            );

        foreach (var ogrenci in ogrenciler)
        {
            ogrenci.IlerlemeYuzdesi = ogrenci.ToplamDersSayisi == 0
                ? 0
                : (int)Math.Round((ogrenci.TamamlananDersSayisi * 100.0) / ogrenci.ToplamDersSayisi);

            ogrenci.SinavGirisSayisi = sinavGirisSayilari.TryGetValue(ogrenci.KursKayitId, out int girisSayisi)
                ? girisSayisi
                : 0;

            if (sonSinavlar.TryGetValue(ogrenci.KursKayitId, out var sonSinav))
            {
                ogrenci.SonSinavPuani = sonSinav.AlinanPuan;
                ogrenci.SinavdanGectiMi = sonSinav.GectiMi;
                ogrenci.SonSinavTarihi = sonSinav.BitisTarihi;

                ogrenci.SinavDurumu = sonSinav.BitisTarihi == null
                    ? "Devam ediyor"
                    : sonSinav.GectiMi == true
                        ? "Geçti"
                        : "Kaldı";
            }
            else
            {
                ogrenci.SinavDurumu = "Henüz girmedi";
            }
        }

        var model = new OgrencilerimViewModel
        {
            Arama = arama,
            KursId = kursId,

            Kurslar = kurslar,
            Ogrenciler = ogrenciler,

            ToplamOgrenciSayisi = toplamKayit,

            OrtalamaIlerlemeYuzdesi = tumOgrenciOzetleri.Any()
                ? (int)Math.Round(tumOgrenciOzetleri.Average(x => x.IlerlemeYuzdesi))
                : 0,

            SinavaGirenOgrenciSayisi = tumOgrenciOzetleri.Count(x => x.SinavGirisSayisi > 0),

            SinavdanGecenOgrenciSayisi = tumOgrenciOzetleri.Count(x => x.SinavdanGectiMi == true),

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            ToplamSayfa = toplamSayfa,
            SayfaBasinaKayit = sayfaBasinaKayit
        };

        return View(model);
    }


}