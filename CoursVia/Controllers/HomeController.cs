using CoursVia.Data;
using CoursVia.ViewModels.Ogrenci;
using CoursVia.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers;

public class HomeController : Controller
{
    private const int DurumAktif = 1;
    private const int DurumYayinda = 5;
    private const int RolOgrenci = 3;

    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var onayliKurslarQuery = _context.Kurslar
            .AsNoTracking()
            .Where(x => x.DurumId == DurumYayinda);

        var oneCikanKurslar = await onayliKurslarQuery
            .OrderByDescending(x => x.KursDegerlendirmeleri.Any()
                ? x.KursDegerlendirmeleri.Average(d => (double)d.Puan)
                : 0)
            .ThenByDescending(x => _context.KursKayitlari
                .Count(k => k.KursId == x.KursId && k.AktifMi))
            .ThenByDescending(x => x.OlusturmaTarihi)
            .Take(10)
            .Select(x => new HomeKursKartiViewModel
            {
                KursId = x.KursId,
                KursAdi = x.KursAdi,
                Aciklama = x.Aciklama,
                KapakGorselUrl = x.KapakGorselUrl,

                EgitmenAdSoyad = x.Egitmen.Ad + " " + x.Egitmen.Soyad,
                OlusturmaTarihi = x.OlusturmaTarihi,

                OrtalamaPuan = x.KursDegerlendirmeleri.Any()
                    ? Math.Round(x.KursDegerlendirmeleri.Average(d => (double)d.Puan), 1)
                    : 0,

                DegerlendirmeSayisi = x.KursDegerlendirmeleri.Count,
                YorumSayisi = x.KursDegerlendirmeleri.Count,

                OgrenciSayisi = _context.KursKayitlari
                    .Count(k => k.KursId == x.KursId && k.AktifMi),

                Kategoriler = x.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList()
            })
            .ToListAsync();

        var yeniKurslar = await onayliKurslarQuery
            .OrderByDescending(x => x.OlusturmaTarihi)
            .Take(6)
            .Select(x => new HomeKursKartiViewModel
            {
                KursId = x.KursId,
                KursAdi = x.KursAdi,
                Aciklama = x.Aciklama,
                KapakGorselUrl = x.KapakGorselUrl,

                EgitmenAdSoyad = x.Egitmen.Ad + " " + x.Egitmen.Soyad,
                OlusturmaTarihi = x.OlusturmaTarihi,

                OrtalamaPuan = x.KursDegerlendirmeleri.Any()
                    ? Math.Round(x.KursDegerlendirmeleri.Average(d => (double)d.Puan), 1)
                    : 0,

                DegerlendirmeSayisi = x.KursDegerlendirmeleri.Count,
                YorumSayisi = x.KursDegerlendirmeleri.Count,

                OgrenciSayisi = _context.KursKayitlari
                    .Count(k => k.KursId == x.KursId && k.AktifMi),

                Kategoriler = x.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList()
            })
            .ToListAsync();

        var kategoriler = await _context.Kategoriler
            .AsNoTracking()
            .Where(x => x.KursKategorileri.Any(k => k.Kurs.DurumId == DurumYayinda))
            .OrderByDescending(x => x.KursKategorileri.Count(k => k.Kurs.DurumId == DurumYayinda))
            .ThenBy(x => x.KategoriAdi)
            .Take(8)
            .Select(x => new HomeKategoriViewModel
            {
                KategoriId = x.KategoriId,
                KategoriAdi = x.KategoriAdi,
                KursSayisi = x.KursKategorileri.Count(k => k.Kurs.DurumId == DurumYayinda)
            })
            .ToListAsync();

        var model = new HomeIndexViewModel
        {
            ToplamOnayliKursSayisi = await onayliKurslarQuery.CountAsync(),

            ToplamEgitmenSayisi = await _context.EgitmenProfilleri
                .AsNoTracking()
                .CountAsync(x =>
                    x.DurumId == DurumYayinda &&
                    x.Kullanici.DurumId == DurumAktif),

            ToplamOgrenciSayisi = await _context.KullaniciRolleri
                .AsNoTracking()
                .Where(x =>
                    x.RolId == RolOgrenci &&
                    x.Kullanici.DurumId == DurumAktif)
                .Select(x => x.KullaniciId)
                .Distinct()
                .CountAsync(),

            ToplamKategoriSayisi = await _context.Kategoriler
                .AsNoTracking()
                .CountAsync(x => x.KursKategorileri.Any(k => k.Kurs.DurumId == DurumYayinda)),

            OneCikanKurslar = oneCikanKurslar,
            YeniKurslar = yeniKurslar,
            Kategoriler = kategoriler
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Kurslar(string? arama, int? kategoriId, string siralama = "oneCikan", int sayfa = 1)
    {
        const int sayfaBasinaKayit = 12;

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        if (sayfa < 1)
        {
            sayfa = 1;
        }

        string[] siralamaSecenekleri = ["oneCikan", "puan", "populer", "enYeni", "ad"];

        if (!siralamaSecenekleri.Contains(siralama))
        {
            siralama = "oneCikan";
        }

        var kategoriler = await _context.Kategoriler
            .AsNoTracking()
            .OrderBy(x => x.KategoriAdi)
            .Select(x => new HomeKategoriViewModel
            {
                KategoriId = x.KategoriId,
                KategoriAdi = x.KategoriAdi,
                KursSayisi = x.KursKategorileri.Count(k => k.Kurs.DurumId == DurumYayinda)
            })
            .ToListAsync();

        var query = _context.Kurslar
            .AsNoTracking()
            .Where(x => x.DurumId == DurumYayinda);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.KursAdi.Contains(arama) ||
                (x.Aciklama != null && x.Aciklama.Contains(arama)) ||
                x.Egitmen.Ad.Contains(arama) ||
                x.Egitmen.Soyad.Contains(arama) ||
                x.KursKategorileri.Any(k => k.Kategori.KategoriAdi.Contains(arama)));
        }

        if (kategoriId.HasValue)
        {
            query = query.Where(x =>
                x.KursKategorileri.Any(k => k.KategoriId == kategoriId.Value));
        }

        query = siralama switch
        {
            "puan" => query
                .OrderByDescending(x => x.KursDegerlendirmeleri.Any()
                    ? x.KursDegerlendirmeleri.Average(d => (double)d.Puan)
                    : 0)
                .ThenByDescending(x => x.KursDegerlendirmeleri.Count)
                .ThenBy(x => x.KursAdi),

            "populer" => query
                .OrderByDescending(x => _context.KursKayitlari.Count(k => k.KursId == x.KursId && k.AktifMi))
                .ThenByDescending(x => x.KursDegerlendirmeleri.Any()
                    ? x.KursDegerlendirmeleri.Average(d => (double)d.Puan)
                    : 0),

            "enYeni" => query
                .OrderByDescending(x => x.OlusturmaTarihi)
                .ThenBy(x => x.KursAdi),

            "ad" => query
                .OrderBy(x => x.KursAdi),

            _ => query
                .OrderByDescending(x => x.KursDegerlendirmeleri.Any()
                    ? x.KursDegerlendirmeleri.Average(d => (double)d.Puan)
                    : 0)
                .ThenByDescending(x => _context.KursKayitlari.Count(k => k.KursId == x.KursId && k.AktifMi))
                .ThenByDescending(x => x.OlusturmaTarihi)
        };

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

        var kurslar = await query
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new HomeKursListeItemViewModel
            {
                KursId = x.KursId,
                KursAdi = x.KursAdi,
                Aciklama = x.Aciklama,
                KapakGorselUrl = x.KapakGorselUrl,

                EgitmenAdSoyad = x.Egitmen.Ad + " " + x.Egitmen.Soyad,
                OlusturmaTarihi = x.OlusturmaTarihi,

                OrtalamaPuan = x.KursDegerlendirmeleri.Any()
                    ? Math.Round(x.KursDegerlendirmeleri.Average(d => (double)d.Puan), 1)
                    : 0,

                DegerlendirmeSayisi = x.KursDegerlendirmeleri.Count,
                YorumSayisi = x.KursDegerlendirmeleri.Count,

                OgrenciSayisi = _context.KursKayitlari
                    .Count(k => k.KursId == x.KursId && k.AktifMi),

                BolumSayisi = x.Bolumler.Count,

                DersSayisi = x.Dersler.Count(d =>
                    d.AktifMi &&
                    !d.SistemDersiMi),

                Kategoriler = x.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList()
            })
            .ToListAsync();

        var model = new HomeKurslarViewModel
        {
            Arama = arama,
            KategoriId = kategoriId,
            Siralama = siralama,
            Sayfa = sayfa,
            ToplamSayfa = toplamSayfa,
            ToplamKayit = toplamKayit,
            SayfaBasinaKayit = sayfaBasinaKayit,
            Kurslar = kurslar,
            Kategoriler = kategoriler
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> KursDetay(int id, int yorumSayfa = 1, string? returnUrl = null)
    {
        const int yorumSayfaBasinaKayit = 5;

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            ViewData["KurslarReturnUrl"] = returnUrl;
        }

        if (yorumSayfa < 1)
        {
            yorumSayfa = 1;
        }

        int? kullaniciId = null;
        string? kullaniciIdDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(kullaniciIdDegeri, out int bulunanKullaniciId))
        {
            kullaniciId = bulunanKullaniciId;
        }

        var kurs = await _context.Kurslar
            .AsNoTracking()
            .Include(x => x.Egitmen)
            .Include(x => x.KursKategorileri)
                .ThenInclude(x => x.Kategori)
            .Include(x => x.Bolumler.OrderBy(b => b.SiraNo))
                .ThenInclude(b => b.Dersler
                    .Where(d => d.AktifMi && !d.SistemDersiMi)
                    .OrderBy(d => d.SiraNo))
                    .ThenInclude(d => d.DersMateryalleri)
            .Include(x => x.Sinav)
            .FirstOrDefaultAsync(x =>
                x.KursId == id &&
                x.DurumId == DurumYayinda);

        if (kurs == null)
        {
            return RedirectToAction(nameof(Kurslar));
        }

        bool kayitliMi = false;
        bool kendiKursuMu = false;

        if (kullaniciId.HasValue)
        {
            kayitliMi = await _context.KursKayitlari
                .AsNoTracking()
                .AnyAsync(x =>
                    x.KullaniciId == kullaniciId.Value &&
                    x.KursId == id &&
                    x.AktifMi);

            kendiKursuMu = kurs.EgitmenId == kullaniciId.Value;
        }

        var degerlendirmeQuery = _context.KursDegerlendirmeleri
            .AsNoTracking()
            .Where(x => x.KursId == id);

        int yorumToplamKayit = await degerlendirmeQuery.CountAsync();
        int yorumToplamSayfa = (int)Math.Ceiling(yorumToplamKayit / (double)yorumSayfaBasinaKayit);

        if (yorumToplamSayfa < 1)
        {
            yorumToplamSayfa = 1;
        }

        if (yorumSayfa > yorumToplamSayfa)
        {
            yorumSayfa = yorumToplamSayfa;
        }

        double ortalamaPuan = yorumToplamKayit > 0
            ? Math.Round(await degerlendirmeQuery.AverageAsync(x => x.Puan), 1)
            : 0;

        var degerlendirmeler = await degerlendirmeQuery
            .OrderByDescending(x => x.DegerlendirmeTarihi)
            .Skip((yorumSayfa - 1) * yorumSayfaBasinaKayit)
            .Take(yorumSayfaBasinaKayit)
            .Select(x => new KursDetayDegerlendirmeViewModel
            {
                DegerlendirmeId = x.DegerlendirmeId,
                OgrenciAdSoyad = x.Kullanici.Ad + " " + x.Kullanici.Soyad,
                Puan = x.Puan,
                YorumMetni = x.YorumMetni,
                DegerlendirmeTarihi = x.DegerlendirmeTarihi
            })
            .ToListAsync();

        var model = new KursDetayViewModel
        {
            KursId = kurs.KursId,
            KursAdi = kurs.KursAdi,
            Aciklama = kurs.Aciklama,
            KapakGorselUrl = kurs.KapakGorselUrl,
            EgitmenAdSoyad = $"{kurs.Egitmen.Ad} {kurs.Egitmen.Soyad}".Trim(),

            Kategoriler = kurs.KursKategorileri
                .Select(x => x.Kategori.KategoriAdi)
                .OrderBy(x => x)
                .ToList(),

            BolumSayisi = kurs.Bolumler.Count,

            DersSayisi = kurs.Bolumler
                .SelectMany(x => x.Dersler)
                .Count(),

            KayitliMi = kayitliMi,
            KendiKursuMu = kendiKursuMu,
            OrtalamaPuan = ortalamaPuan,
            DegerlendirmeSayisi = yorumToplamKayit,
            Degerlendirmeler = degerlendirmeler,
            YorumSayfa = yorumSayfa,
            YorumToplamSayfa = yorumToplamSayfa,
            YorumToplamKayit = yorumToplamKayit,

            Bolumler = kurs.Bolumler
                .OrderBy(x => x.SiraNo)
                .Select(x => new KursDetayBolumViewModel
                {
                    BolumId = x.BolumId,
                    BolumAdi = x.BolumAdi,
                    SiraNo = x.SiraNo,

                    Dersler = x.Dersler
                        .OrderBy(d => d.SiraNo)
                        .Select(d => new KursDetayDersViewModel
                        {
                            DersId = d.DersId,
                            DersAdi = d.DersAdi,
                            Aciklama = d.Aciklama,
                            SiraNo = d.SiraNo,
                            MateryalSayisi = d.DersMateryalleri.Count
                        })
                        .ToList()
                })
                .ToList(),

            Sinav = kurs.Sinav == null
                ? null
                : new KursDetaySinavViewModel
                {
                    SinavAdi = kurs.Sinav.SinavAdi,
                    Aciklama = kurs.Sinav.Aciklama,
                    GecmeNotu = kurs.Sinav.GecmeNotu,
                    SureDakika = kurs.Sinav.SureDakika,
                    SoruSayisi = kurs.Sinav.SoruSayisi
                }
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Hakkimizda()
    {
        return View();
    }
}
