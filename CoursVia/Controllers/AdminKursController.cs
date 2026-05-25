using CoursVia.Data;
using CoursVia.Models;
using CoursVia.Services;
using CoursVia.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Claims;

namespace CoursVia.Controllers;

[Authorize(Roles = "Admin")]
public class AdminKursController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly AdminLogService _adminLogService;
    private readonly BildirimService _bildirimService;

    public AdminKursController(
        AppDbContext context,
        IWebHostEnvironment webHostEnvironment,
        AdminLogService adminLogService,
        BildirimService bildirimService)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _adminLogService = adminLogService;
        _bildirimService = bildirimService;
    }

    
    [HttpGet]
    public async Task<IActionResult> Kurslar(
    string? arama,
    string durum = "tum",
    int? kategoriId = null,
    int sayfa = 1)
    {
        const int sayfaBasinaKayit = 10;

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        durum = string.IsNullOrWhiteSpace(durum)
            ? "tum"
            : durum.Trim().ToLower();

        if (durum != "tum" &&
            durum != "pasif" &&
            durum != "taslak" &&
            durum != "bekleyen" &&
            durum != "onayli" &&
            durum != "reddedilen" &&
            durum != "duzeltme")
        {
            durum = "tum";
        }

        if (kategoriId.HasValue && kategoriId.Value <= 0)
        {
            kategoriId = null;
        }

        if (sayfa < 1)
        {
            sayfa = 1;
        }

        var query = _context.Kurslar
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.KursAdi.Contains(arama) ||
                (x.Aciklama != null && x.Aciklama.Contains(arama)) ||
                x.Egitmen.Ad.Contains(arama) ||
                x.Egitmen.Soyad.Contains(arama) ||
                x.Egitmen.Eposta.Contains(arama));
        }

        query = durum switch
        {
            "pasif" => query.Where(x => x.DurumId == 2),
            "taslak" => query.Where(x => x.DurumId == 3),
            "bekleyen" => query.Where(x => x.DurumId == 4),
            "onayli" => query.Where(x => x.DurumId == 5),
            "reddedilen" => query.Where(x => x.DurumId == 6),
            "duzeltme" => query.Where(x => x.DurumId == 7),
            _ => query
        };

        if (kategoriId.HasValue)
        {
            query = query.Where(x =>
                x.KursKategorileri.Any(k => k.KategoriId == kategoriId.Value));
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

        var kurslar = await query
            .AsSplitQuery()
            .OrderByDescending(x => x.OlusturmaTarihi)
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new AdminKursListeItemViewModel
            {
                KursId = x.KursId,
                KursAdi = x.KursAdi,
                Aciklama = x.Aciklama,
                KapakGorselUrl = x.KapakGorselUrl,

                EgitmenAdSoyad = x.Egitmen.Ad + " " + x.Egitmen.Soyad,
                EgitmenEposta = x.Egitmen.Eposta,

                DurumId = x.DurumId,
                DurumAdi = x.Durum.DurumAdi,

                OlusturmaTarihi = x.OlusturmaTarihi,
                GuncellemeTarihi = x.GuncellemeTarihi,

                BolumSayisi = x.Bolumler.Count,
                DersSayisi = x.Dersler.Count,

                OgrenciSayisi = _context.KursKayitlari.Count(k => k.KursId == x.KursId),
                SertifikaSayisi = _context.Sertifikalar.Count(s => s.KursId == x.KursId),
                DegerlendirmeSayisi = _context.KursDegerlendirmeleri.Count(d => d.KursId == x.KursId),

                SinavVarMi = x.Sinav != null,
                SoruSayisi = x.Sinav == null ? 0 : x.Sinav.Sorular.Count,

                Kategoriler = x.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList(),

                KategoriIdleri = x.KursKategorileri
                    .Select(k => k.KategoriId)
                    .ToList()
            })
            .ToListAsync();

        var kategoriSecenekleri = await _context.Kategoriler
            .AsNoTracking()
            .OrderBy(x => x.KategoriAdi)
            .Select(x => new AdminKursKategoriFiltreViewModel
            {
                KategoriId = x.KategoriId,
                KategoriAdi = x.KategoriAdi,
                KursSayisi = x.KursKategorileri.Count
            })
            .ToListAsync();

        var model = new AdminKursYonetimiViewModel
        {
            Arama = arama,
            Durum = durum,
            KategoriId = kategoriId,
            KategoriSecenekleri = kategoriSecenekleri,

            Kurslar = kurslar,

            ToplamKursSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(),

            OnayliKursSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 5),

            OnayBekleyenKursSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 4),

            TaslakKursSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 3),

            DuzeltmeIstenenKursSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 7),

            ReddedilenKursSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 6),

            PasifKursSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 2),

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            ToplamSayfa = toplamSayfa,
            SayfaBasinaKayit = sayfaBasinaKayit
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KategoriDuzenle(int kursId, List<int> seciliKategoriler, string? returnUrl = null)
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kursKategori = await _context.Kurslar
            .Include(x => x.KursKategorileri)
            .FirstOrDefaultAsync(x => x.KursId == kursId);

        if (kursKategori == null)
        {
            TempData["AdminHata"] = "Kurs bulunamadı.";
            return RedirectToAction(nameof(Kurslar));
        }

        _context.KursKategorileri.RemoveRange(kursKategori.KursKategorileri);

        if (seciliKategoriler != null && seciliKategoriler.Any())
        {
            foreach (var katId in seciliKategoriler)
            {
                _context.KursKategorileri.Add(new KursKategorisi
                {
                    KursId = kursKategori.KursId,
                    KategoriId = katId
                });
            }
        }

        kursKategori.GuncellemeTarihi = DateTime.Now;

        await _adminLogService.KaydetAsync(
            adminId,
            AdminLogService.KursIslemleri,
            $"{kursKategori.KursAdi} adlı kursun kategorileri güncellendi.");

        await _context.SaveChangesAsync();

        TempData["AdminBasari"] = "Kurs kategorileri güncellendi.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Kurslar));
    }

    [HttpGet]
    public async Task<IActionResult> Detay(int id, int yorumSayfa = 1)
    {
        const int yorumSayfaBasinaKayit = 5;

        if (yorumSayfa < 1)
        {
            yorumSayfa = 1;
        }

        var kurs = await _context.Kurslar
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Egitmen)
            .Include(x => x.Durum)
            .Include(x => x.KursKategorileri)
                .ThenInclude(x => x.Kategori)
            .Include(x => x.Bolumler)
                .ThenInclude(x => x.Dersler)
                    .ThenInclude(x => x.DersMateryalleri)
                        .ThenInclude(x => x.MateryalTipi)
            .Include(x => x.Sinav)
                .ThenInclude(x => x!.Sorular)
                    .ThenInclude(x => x.SoruSecenekleri)
            .FirstOrDefaultAsync(x => x.KursId == id);

        if (kurs == null)
        {
            TempData["AdminHata"] = "Kurs bulunamadı.";
            return RedirectToAction(nameof(Kurslar));
        }

        var degerlendirmeQuery = _context.KursDegerlendirmeleri
            .AsNoTracking()
            .Where(x => x.KursId == kurs.KursId);

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
            .Select(x => new AdminKursDegerlendirmeViewModel
            {
                DegerlendirmeId = x.DegerlendirmeId,
                OgrenciAdSoyad = x.Kullanici.Ad + " " + x.Kullanici.Soyad,
                Puan = x.Puan,
                YorumMetni = x.YorumMetni,
                DegerlendirmeTarihi = x.DegerlendirmeTarihi
            })
            .ToListAsync();

        var kursKayitOzetleri = await _context.KursKayitlari
            .AsNoTracking()
            .Where(x => x.KursId == kurs.KursId)
            .Select(x => new
            {
                x.AktifMi,
                x.TamamlandiMi
            })
            .ToListAsync();

        int kayitliOgrenciSayisi = kursKayitOzetleri.Count;
        int devamEdenOgrenciSayisi = kursKayitOzetleri.Count(x => x.AktifMi && !x.TamamlandiMi);
        int tamamlayanOgrenciSayisi = kursKayitOzetleri.Count(x => x.TamamlandiMi);
        int pasifKayitSayisi = kursKayitOzetleri.Count(x => !x.AktifMi);

        var kategoriSecenekleri = await _context.Kategoriler
            .AsNoTracking()
            .OrderBy(x => x.KategoriAdi)
            .Select(x => new AdminKursKategoriFiltreViewModel
            {
                KategoriId = x.KategoriId,
                KategoriAdi = x.KategoriAdi,
                KursSayisi = x.KursKategorileri.Count
            })
            .ToListAsync();
        var model = new AdminKursDetayViewModel
        {
            KursId = kurs.KursId,
            KursAdi = kurs.KursAdi,
            Aciklama = kurs.Aciklama,
            KapakGorselUrl = kurs.KapakGorselUrl,

            EgitmenAdSoyad = $"{kurs.Egitmen.Ad} {kurs.Egitmen.Soyad}".Trim(),
            EgitmenEposta = kurs.Egitmen.Eposta,

            DurumId = kurs.DurumId,
            DurumAdi = kurs.Durum.DurumAdi,

            OlusturmaTarihi = kurs.OlusturmaTarihi,
            GuncellemeTarihi = kurs.GuncellemeTarihi,

            Kategoriler = kurs.KursKategorileri
                .Select(x => x.Kategori.KategoriAdi)
                .OrderBy(x => x)
                .ToList(),

            KategoriSecenekleri = kategoriSecenekleri,
            KategoriIdleri = kurs.KursKategorileri.Select(k => k.KategoriId).ToList(),

            Bolumler = kurs.Bolumler
                .OrderBy(x => x.SiraNo)
                .Select(x => new AdminKursBolumViewModel
                {
                    BolumId = x.BolumId,
                    BolumAdi = x.BolumAdi,
                    SiraNo = x.SiraNo,

                    Dersler = x.Dersler
                        .OrderBy(d => d.SiraNo)
                        .Select(d => new AdminKursDersViewModel
                        {
                            DersId = d.DersId,
                            DersAdi = d.DersAdi,
                            Aciklama = d.Aciklama,
                            VideoUrl = d.VideoUrl,
                            SiraNo = d.SiraNo,
                            AktifMi = d.AktifMi,
                            SistemDersiMi = d.SistemDersiMi,

                            Materyaller = d.DersMateryalleri
                                .OrderBy(m => m.Baslik)
                                .Select(m => new AdminKursDersMateryalViewModel
                                {
                                    MateryalId = m.MateryalId,
                                    Baslik = m.Baslik,
                                    MateryalUrl = m.MateryalUrl,
                                    MateryalTipAdi = m.MateryalTipi.MateryalTipAdi
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList(),


            Sinav = kurs.Sinav == null
                ? null
                : new AdminKursSinavViewModel
                {
                    SinavId = kurs.Sinav.SinavId,
                    SinavAdi = kurs.Sinav.SinavAdi,
                    Aciklama = kurs.Sinav.Aciklama,
                    GecmeNotu = kurs.Sinav.GecmeNotu,
                    SureDakika = kurs.Sinav.SureDakika,
                    SoruSayisi = kurs.Sinav.SoruSayisi,
                    AktifSoruSayisi = kurs.Sinav.Sorular.Count(x => x.AktifMi),

                    Sorular = kurs.Sinav.Sorular
                        .OrderBy(x => x.SoruId)
                        .Select(x => new AdminKursSoruViewModel
                        {
                            SoruId = x.SoruId,
                            SoruMetni = x.SoruMetni,
                            AktifMi = x.AktifMi,

                            Secenekler = x.SoruSecenekleri
                                .OrderBy(s => s.SecenekId)
                                .Select(s => new AdminKursSoruSecenegiViewModel
                                {
                                    SecenekId = s.SecenekId,
                                    SecenekMetni = s.SecenekMetni,
                                    DogruMu = s.DogruMu,
                                    AktifMi = s.AktifMi
                                })
                                .ToList()
                        })
                        .ToList()
                },

            OgrenciSayisi = kayitliOgrenciSayisi,
            KayitliOgrenciSayisi = kayitliOgrenciSayisi,
            DevamEdenOgrenciSayisi = devamEdenOgrenciSayisi,
            TamamlayanOgrenciSayisi = tamamlayanOgrenciSayisi,
            PasifKayitSayisi = pasifKayitSayisi,

            SertifikaSayisi = await _context.Sertifikalar
                .AsNoTracking()
                .CountAsync(x => x.KursId == kurs.KursId),

            DegerlendirmeSayisi = yorumToplamKayit,
            OrtalamaPuan = ortalamaPuan,
            Degerlendirmeler = degerlendirmeler,
            YorumSayfa = yorumSayfa,
            YorumToplamSayfa = yorumToplamSayfa,
            YorumToplamKayit = yorumToplamKayit,
            YorumSayfaBasinaKayit = yorumSayfaBasinaKayit,

            FavoriSayisi = await _context.Favoriler
                .AsNoTracking()
                .CountAsync(x => x.KursId == kurs.KursId)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Ogrenciler(
        int id,
        string? arama,
        string durum = "tum",
        string siralama = "yeni",
        int sayfa = 1)
    {
        const int sayfaBasinaKayit = 10;

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        durum = string.IsNullOrWhiteSpace(durum)
            ? "tum"
            : durum.Trim().ToLower();

        if (durum != "tum" &&
            durum != "devam" &&
            durum != "tamamlayan" &&
            durum != "pasif")
        {
            durum = "tum";
        }

        siralama = string.IsNullOrWhiteSpace(siralama)
            ? "yeni"
            : siralama.Trim().ToLower();

        if (siralama != "yeni" && siralama != "eski")
        {
            siralama = "yeni";
        }

        if (sayfa < 1)
        {
            sayfa = 1;
        }

        var kurs = await _context.Kurslar
            .AsNoTracking()
            .Where(x => x.KursId == id)
            .Select(x => new
            {
                x.KursId,
                x.KursAdi
            })
            .FirstOrDefaultAsync();

        if (kurs == null)
        {
            TempData["AdminHata"] = "Kurs bulunamadı.";
            return RedirectToAction(nameof(Kurslar));
        }

        int toplamDersSayisi = await _context.Dersler
            .AsNoTracking()
            .CountAsync(x =>
                x.KursId == id &&
                x.AktifMi &&
                !x.SistemDersiMi);

        bool sinavVarMi = await _context.Sinavlar
            .AsNoTracking()
            .AnyAsync(x => x.KursId == id);

        var query = _context.KursKayitlari
            .AsNoTracking()
            .Where(x => x.KursId == id);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.Kullanici.Ad.Contains(arama) ||
                x.Kullanici.Soyad.Contains(arama) ||
                x.Kullanici.Eposta.Contains(arama));
        }

        query = durum switch
        {
            "devam" => query.Where(x => x.AktifMi && !x.TamamlandiMi),
            "tamamlayan" => query.Where(x => x.TamamlandiMi),
            "pasif" => query.Where(x => !x.AktifMi),
            _ => query
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

        var siraliQuery = siralama == "eski"
            ? query.OrderBy(x => x.KayitTarihi)
            : query.OrderByDescending(x => x.KayitTarihi);

        var ogrenciler = await siraliQuery
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new AdminKursOgrenciListeItemViewModel
            {
                KursKayitId = x.KursKayitId,
                OgrenciId = x.KullaniciId,
                AdSoyad = x.Kullanici.Ad + " " + x.Kullanici.Soyad,
                Eposta = x.Kullanici.Eposta,
                KayitTarihi = x.KayitTarihi,
                AktifMi = x.AktifMi,
                TamamlandiMi = x.TamamlandiMi,
                TamamlananDersSayisi = x.DersIlerlemeleri
                    .Count(i =>
                        i.TamamlandiMi &&
                        i.Ders.AktifMi &&
                        !i.Ders.SistemDersiMi)
            })
            .ToListAsync();

        var kursKayitIdleri = ogrenciler
            .Select(x => x.KursKayitId)
            .ToList();

        var sinavKatilimlari = await _context.SinavKatilimlari
            .AsNoTracking()
            .Where(x => kursKayitIdleri.Contains(x.KursKayitId))
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

        var sonSinavlar = sinavKatilimlari
            .GroupBy(x => x.KursKayitId)
            .ToDictionary(
                x => x.Key,
                x => x.First());

        var sinavGirisSayilari = sinavKatilimlari
            .GroupBy(x => x.KursKayitId)
            .ToDictionary(
                x => x.Key,
                x => x.Count());

        foreach (var ogrenci in ogrenciler)
        {
            ogrenci.ToplamDersSayisi = toplamDersSayisi;
            ogrenci.IlerlemeYuzdesi = toplamDersSayisi == 0
                ? 0
                : (int)Math.Round(ogrenci.TamamlananDersSayisi * 100.0 / toplamDersSayisi);

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
                ogrenci.SinavDurumu = sinavVarMi
                    ? "Henüz girmedi"
                    : "Sınav yok";
            }
        }

        var model = new AdminKursOgrencilerViewModel
        {
            KursId = kurs.KursId,
            KursAdi = kurs.KursAdi,
            Arama = arama,
            Durum = durum,
            Siralama = siralama,
            Ogrenciler = ogrenciler,
            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            ToplamSayfa = toplamSayfa,
            SayfaBasinaKayit = sayfaBasinaKayit
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DuzenlemeyeGonder(int id, string? aciklama)
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        aciklama = string.IsNullOrWhiteSpace(aciklama)
            ? null
            : aciklama.Trim();

        if (string.IsNullOrWhiteSpace(aciklama))
        {
            TempData["AdminHata"] = "Düzenlemeye gönderme sebebi zorunludur.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var kurs = await _context.Kurslar
            .Include(x => x.Egitmen)
            .FirstOrDefaultAsync(x => x.KursId == id);

        if (kurs == null)
        {
            TempData["AdminHata"] = "Kurs bulunamadı.";
            return RedirectToAction(nameof(Kurslar));
        }

        if (kurs.DurumId == 2)
        {
            TempData["AdminHata"] = "Pasif kurs düzenlemeye gönderilemez.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        if (kurs.DurumId == 7)
        {
            TempData["AdminHata"] = "Kurs zaten düzenleme bekliyor.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        kurs.DurumId = 7;
        kurs.GuncellemeTarihi = DateTime.Now;

        _context.KursOnaylari.Add(new KursOnayi
        {
            KursId = kurs.KursId,
            AdminId = adminId,
            DurumId = 7,
            Aciklama = aciklama,
            IslemTarihi = DateTime.Now
        });


        await _bildirimService.BildirimOlusturAsync(
        kurs.EgitmenId,
        "Uyarı",
        "Kursunuz için düzeltme istendi",
        $"\"{kurs.KursAdi}\" adlı kursunuz için düzeltme istendi. Lütfen kurs içeriğini kontrol edin. Sebep: {aciklama}");

        await DevamEdenOgrencilereKursBildirimGonderAsync(
            kurs.KursId,
            "Kurs güncelleniyor",
            $"\"{kurs.KursAdi}\" adlı kurs şu anda güncellenmektedir. Güncelleme tamamlandığında kursa tekrar devam edebilirsiniz.");


        await _adminLogService.KaydetAsync(
            adminId,
            AdminLogService.KursIslemleri,
            $"{kurs.KursAdi} adlı kurs düzenleme için eğitmene gönderildi. Sebep: {aciklama}");

        

        await _context.SaveChangesAsync();

        TempData["AdminBasari"] = "Kurs düzenleme için eğitmene gönderildi.";

        return RedirectToAction(nameof(Detay), new { id = kurs.KursId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PasifeAl(int id)
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kurs = await _context.Kurslar
            .FirstOrDefaultAsync(x => x.KursId == id);

        if (kurs == null)
        {
            TempData["AdminHata"] = "Kurs bulunamadı.";
            return RedirectToAction(nameof(Kurslar));
        }

        if (kurs.DurumId == 2)
        {
            TempData["AdminHata"] = "Kurs zaten pasif durumda.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        kurs.DurumId = 2;
        kurs.GuncellemeTarihi = DateTime.Now;

        await _adminLogService.KaydetAsync(
            adminId,
            AdminLogService.KursIslemleri,
            $"{kurs.KursAdi} adlı kurs pasife alındı.");

        await _context.SaveChangesAsync();

        TempData["AdminBasari"] = "Kurs pasife alındı.";

        return RedirectToAction(nameof(Detay), new { id = kurs.KursId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YayinaAl(int id)
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kurs = await _context.Kurslar
            .FirstOrDefaultAsync(x => x.KursId == id);

        if (kurs == null)
        {
            TempData["AdminHata"] = "Kurs bulunamadı.";
            return RedirectToAction(nameof(Kurslar));
        }

        if (kurs.DurumId == 5)
        {
            TempData["AdminHata"] = "Kurs zaten yayında.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        kurs.DurumId = 5;
        kurs.GuncellemeTarihi = DateTime.Now;

        _context.KursOnaylari.Add(new KursOnayi
        {
            KursId = kurs.KursId,
            AdminId = adminId,
            DurumId = 5,
            Aciklama = "Admin tarafından yayına alındı.",
            IslemTarihi = DateTime.Now
        });

        await _adminLogService.KaydetAsync(
            adminId,
            AdminLogService.KursIslemleri,
            $"{kurs.KursAdi} adlı kurs admin tarafından yayına alındı.");

        await DevamEdenOgrencilereKursBildirimGonderAsync(
            kurs.KursId,
            "Kurs tekrar yayında",
            $"\"{kurs.KursAdi}\" adlı kurs güncellendi ve tekrar erişime açıldı.");

        await _context.SaveChangesAsync();

        TempData["AdminBasari"] = "Kurs yayına alındı.";

        return RedirectToAction(nameof(Detay), new { id = kurs.KursId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kurs = await _context.Kurslar
            .FirstOrDefaultAsync(x => x.KursId == id);

        if (kurs == null)
        {
            TempData["AdminHata"] = "Kurs bulunamadı.";
            return RedirectToAction(nameof(Kurslar));
        }

        string kursAdi = kurs.KursAdi;
        string? kapakGorselUrl = kurs.KapakGorselUrl;

        var silinecekDosyaUrlListesi = await _context.DersMateryalleri
            .AsNoTracking()
            .Where(x => x.Ders.KursId == id)
            .Select(x => x.MateryalUrl)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(kapakGorselUrl))
        {
            silinecekDosyaUrlListesi.Add(kapakGorselUrl);
        }

        silinecekDosyaUrlListesi = silinecekDosyaUrlListesi
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await KursaBagliVerileriSilAsync(id);

            _context.Kurslar.Remove(kurs);

            await _adminLogService.KaydetAsync(
                adminId,
                AdminLogService.KursIslemleri,
                $"{kursAdi} adlı kurs ve bağlı tüm kayıtları kalıcı olarak silindi.");

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            foreach (var dosyaUrl in silinecekDosyaUrlListesi)
            {
                bool baskaKayittaKullaniliyorMu = await DosyaUrlBaskaKayittaKullaniliyorMuAsync(dosyaUrl);

                if (!baskaKayittaKullaniliyorMu)
                {
                    LocalDosyayiSil(dosyaUrl);
                }
            }

            TempData["AdminBasari"] = "Kurs ve bağlı tüm kayıtları kalıcı olarak silindi.";

            return RedirectToAction(nameof(Kurslar));
        }
        catch
        {
            await transaction.RollbackAsync();

            TempData["AdminHata"] = "Kurs silinirken beklenmeyen bir hata oluştu.";
            return RedirectToAction(nameof(Detay), new { id });
        }
    }

    private async Task KursaBagliVerileriSilAsync(int kursId)
    {
        var kursKayitIdleri = await _context.KursKayitlari
            .Where(x => x.KursId == kursId)
            .Select(x => x.KursKayitId)
            .ToListAsync();

        var dersIdleri = await _context.Dersler
            .Where(x => x.KursId == kursId)
            .Select(x => x.DersId)
            .ToListAsync();

        var sinavIdleri = await _context.Sinavlar
            .Where(x => x.KursId == kursId)
            .Select(x => x.SinavId)
            .ToListAsync();

        var sinavKatilimIdleri = await _context.SinavKatilimlari
            .Where(x =>
                kursKayitIdleri.Contains(x.KursKayitId) ||
                sinavIdleri.Contains(x.SinavId))
            .Select(x => x.SinavKatilimId)
            .ToListAsync();

        var ogrenciCevaplari = await _context.OgrenciCevaplari
            .Where(x => sinavKatilimIdleri.Contains(x.SinavKatilimId))
            .ToListAsync();

        var sinavKatilimlari = await _context.SinavKatilimlari
            .Where(x => sinavKatilimIdleri.Contains(x.SinavKatilimId))
            .ToListAsync();

        var dersIlerlemeleri = await _context.DersIlerlemeleri
            .Where(x =>
                kursKayitIdleri.Contains(x.KursKayitId) ||
                dersIdleri.Contains(x.DersId))
            .ToListAsync();

        var soruDersleri = await _context.SoruDersleri
            .Where(x =>
                sinavIdleri.Contains(x.Soru.SinavId) ||
                dersIdleri.Contains(x.DersId))
            .ToListAsync();

        var soruSecenekleri = await _context.SoruSecenekleri
            .Where(x => sinavIdleri.Contains(x.Soru.SinavId))
            .ToListAsync();

        var sorular = await _context.Sorular
            .Where(x => sinavIdleri.Contains(x.SinavId))
            .ToListAsync();

        var sinavlar = await _context.Sinavlar
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        var dersMateryalleri = await _context.DersMateryalleri
            .Where(x => dersIdleri.Contains(x.DersId))
            .ToListAsync();

        var dersler = await _context.Dersler
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        var bolumler = await _context.Bolumler
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        var kursKayitlari = await _context.KursKayitlari
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        var sertifikalar = await _context.Sertifikalar
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        var favoriler = await _context.Favoriler
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        var degerlendirmeler = await _context.KursDegerlendirmeleri
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        var oneriler = await _context.Oneriler
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        var kursOnaylari = await _context.KursOnaylari
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        var kursKategorileri = await _context.KursKategorileri
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        _context.OgrenciCevaplari.RemoveRange(ogrenciCevaplari);
        _context.SinavKatilimlari.RemoveRange(sinavKatilimlari);
        _context.DersIlerlemeleri.RemoveRange(dersIlerlemeleri);

        _context.SoruDersleri.RemoveRange(soruDersleri);
        _context.SoruSecenekleri.RemoveRange(soruSecenekleri);
        _context.Sorular.RemoveRange(sorular);
        _context.Sinavlar.RemoveRange(sinavlar);

        _context.DersMateryalleri.RemoveRange(dersMateryalleri);
        _context.Dersler.RemoveRange(dersler);
        _context.Bolumler.RemoveRange(bolumler);

        _context.KursKayitlari.RemoveRange(kursKayitlari);
        _context.Sertifikalar.RemoveRange(sertifikalar);
        _context.Favoriler.RemoveRange(favoriler);
        _context.KursDegerlendirmeleri.RemoveRange(degerlendirmeler);
        _context.Oneriler.RemoveRange(oneriler);
        _context.KursOnaylari.RemoveRange(kursOnaylari);
        _context.KursKategorileri.RemoveRange(kursKategorileri);
    }

    private async Task DevamEdenOgrencilereKursBildirimGonderAsync(
        int kursId,
        string baslik,
        string mesaj)
    {
        var devamEdenOgrenciIdleri = await _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.KursId == kursId &&
                x.AktifMi &&
                !x.TamamlandiMi)
            .Select(x => x.KullaniciId)
            .Distinct()
            .ToListAsync();

        foreach (int ogrenciId in devamEdenOgrenciIdleri)
        {
            await _bildirimService.BildirimOlusturAsync(
                ogrenciId,
                "Bilgilendirme",
                baslik,
                mesaj);
        }
    }

    private async Task<bool> DosyaUrlBaskaKayittaKullaniliyorMuAsync(string dosyaUrl)
    {
        if (string.IsNullOrWhiteSpace(dosyaUrl))
        {
            return false;
        }

        dosyaUrl = dosyaUrl.Trim();

        bool materyaldeKullaniliyorMu = await _context.DersMateryalleri
            .AsNoTracking()
            .AnyAsync(x => x.MateryalUrl == dosyaUrl);

        if (materyaldeKullaniliyorMu)
        {
            return true;
        }

        bool kapaktaKullaniliyorMu = await _context.Kurslar
            .AsNoTracking()
            .AnyAsync(x => x.KapakGorselUrl == dosyaUrl);

        return kapaktaKullaniliyorMu;
    }

    private void LocalDosyayiSil(string? dosyaUrl)
    {
        if (string.IsNullOrWhiteSpace(dosyaUrl))
        {
            return;
        }

        dosyaUrl = dosyaUrl.Trim();

        if (dosyaUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            dosyaUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string temizYol = dosyaUrl
            .Split('?')[0]
            .TrimStart('~')
            .TrimStart('/')
            .Replace("/", Path.DirectorySeparatorChar.ToString());

        if (string.IsNullOrWhiteSpace(temizYol))
        {
            return;
        }

        string webRootPath = _webHostEnvironment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            return;
        }

        string tamYol = Path.GetFullPath(Path.Combine(webRootPath, temizYol));
        string guvenliRoot = Path.GetFullPath(webRootPath);

        bool wwwrootIcindeMi =
            tamYol.StartsWith(guvenliRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tamYol, guvenliRoot, StringComparison.OrdinalIgnoreCase);

        if (!wwwrootIcindeMi)
        {
            return;
        }

        try
        {
            if (System.IO.File.Exists(tamYol))
            {
                System.IO.File.Delete(tamYol);
            }
        }
        catch
        {
            // Dosya silinemezse DB işlemi geri alınmaz.
        }
    }

}
