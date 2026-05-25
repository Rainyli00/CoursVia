using CoursVia.Data;
using CoursVia.Models;
using CoursVia.Services;
using CoursVia.ViewModels.Ogrenci;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers;

[Authorize(Roles = "Öğrenci")]
public class OgrenciKursController : Controller
{
    private readonly AppDbContext _context;
    private readonly BildirimService _bildirimService;
    public OgrenciKursController(AppDbContext context, BildirimService bildirimService)
    {
        _context = context;
        _bildirimService = bildirimService;
    }

    [HttpGet]
    public async Task<IActionResult> Kesfet(string? arama, int? kategoriId, string? siralama, int sayfa = 1)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        const int sayfaBasinaKayit = 12;

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        if (sayfa < 1)
        {
            sayfa = 1;
        }

        var kategoriler = await _context.Kategoriler
            .AsNoTracking()
            .OrderBy(x => x.KategoriAdi)
            .Select(x => new KursKesfetKategoriViewModel
            {
                KategoriId = x.KategoriId,
                KategoriAdi = x.KategoriAdi
            })
            .ToListAsync();

        var query = _context.Kurslar
            .AsNoTracking()
            .Where(x => x.DurumId == 5);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.KursAdi.Contains(arama) ||
                (x.Aciklama != null && x.Aciklama.Contains(arama)) ||
                x.Egitmen.Ad.Contains(arama) ||
                x.Egitmen.Soyad.Contains(arama));
        }

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

        string[] siralamaSecenekleri = ["oneCikan", "puan", "populer", "enYeni", "ad"];

        if (string.IsNullOrWhiteSpace(siralama) || !siralamaSecenekleri.Contains(siralama))
        {
            siralama = "oneCikan";
        }

        var queryOrdered = siralama switch
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

        var kurslar = await queryOrdered
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new KursKesfetListeItemViewModel
            {
                KursId = x.KursId,
                KursAdi = x.KursAdi,
                Aciklama = x.Aciklama,
                KapakGorselUrl = x.KapakGorselUrl,

                EgitmenAdSoyad = x.Egitmen.Ad + " " + x.Egitmen.Soyad,

                OrtalamaPuan = x.KursDegerlendirmeleri.Any()
                    ? Math.Round(x.KursDegerlendirmeleri.Average(d => (double)d.Puan), 1)
                    : 0,

                DegerlendirmeSayisi = x.KursDegerlendirmeleri.Count,

                OgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi),

                Kategoriler = x.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList(),

                BolumSayisi = x.Bolumler.Count,

                DersSayisi = x.Dersler.Count(d =>
                    d.AktifMi &&
                    !d.SistemDersiMi),

                KayitliMi = _context.KursKayitlari.Any(k =>
                    k.KullaniciId == kullaniciId &&
                    k.KursId == x.KursId &&
                    k.AktifMi),

                FavorideMi = _context.Favoriler.Any(f =>
                    f.KullaniciId == kullaniciId &&
                    f.KursId == x.KursId),

                KendiKursuMu = x.EgitmenId == kullaniciId
            })
            .ToListAsync();

        var model = new KursKesfetViewModel
        {
            Arama = arama,
            KategoriId = kategoriId,
            Siralama = siralama,
            Kategoriler = kategoriler,
            Kurslar = kurslar,

            Sayfa = sayfa,
            ToplamSayfa = toplamSayfa,
            ToplamKayit = toplamKayit,
            SayfaBasinaKayit = sayfaBasinaKayit
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KayitOl(int id)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kurs = await _context.Kurslar
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.KursId == id);

        if (kurs == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (kurs.DurumId != 5)
        {
            TempData["OgrenciHata"] = "Sadece yayındaki kurslara kayıt olabilirsiniz.";
            return RedirectToAction(nameof(Kesfet));
        }

        if (kurs.EgitmenId == kullaniciId)
        {
            TempData["OgrenciHata"] = "Kendi kursunuza öğrenci olarak kayıt olamazsınız.";
            return RedirectToAction(nameof(Kesfet));
        }

        bool aktifKayitVar = await _context.KursKayitlari
            .AnyAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == id &&
                x.AktifMi);

        if (aktifKayitVar)
        {
            TempData["OgrenciHata"] = "Bu kursa zaten kayıtlısınız.";
            return RedirectToAction(nameof(Kurslarim));
        }

        var kursKaydi = new KursKaydi
        {
            KullaniciId = kullaniciId,
            KursId = id,
            KayitTarihi = DateTime.Now,
            TamamlandiMi = false,
            TamamlanmaTarihi = null,
            AktifMi = true
        };

        _context.KursKayitlari.Add(kursKaydi);
        await _context.SaveChangesAsync();

        TempData["OgrenciBasari"] = "Kursa başarıyla kayıt oldunuz.";
        return RedirectToAction("Izle", "OgrenciDers", new { kursId = id });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KayitIptal(int id, string? returnUrl = null)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kursKaydi = await _context.KursKayitlari
            .FirstOrDefaultAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == id);

        if (kursKaydi == null)
        {
            TempData["OgrenciHata"] = "Kurs kaydı bulunamadı.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Kurslarim));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Bu kayıt üzerinden girilen sınavlar
            var sinavKatilimIdleri = await _context.SinavKatilimlari
                .Where(x => x.KursKayitId == kursKaydi.KursKayitId)
                .Select(x => x.SinavKatilimId)
                .ToListAsync();

            // Önce öğrenci cevapları silinir
            var ogrenciCevaplari = await _context.OgrenciCevaplari
                .Where(x => sinavKatilimIdleri.Contains(x.SinavKatilimId))
                .ToListAsync();

            _context.OgrenciCevaplari.RemoveRange(ogrenciCevaplari);

            // Sonra sınav katılımları silinir
            var sinavKatilimlari = await _context.SinavKatilimlari
                .Where(x => x.KursKayitId == kursKaydi.KursKayitId)
                .ToListAsync();

            _context.SinavKatilimlari.RemoveRange(sinavKatilimlari);

            // Ders ilerlemeleri silinir
            var dersIlerlemeleri = await _context.DersIlerlemeleri
                .Where(x => x.KursKayitId == kursKaydi.KursKayitId)
                .ToListAsync();

            _context.DersIlerlemeleri.RemoveRange(dersIlerlemeleri);

            // Sertifika varsa silinir
            var sertifikalar = await _context.Sertifikalar
                .Where(x =>
                    x.KullaniciId == kullaniciId &&
                    x.KursId == id)
                .ToListAsync();

            _context.Sertifikalar.RemoveRange(sertifikalar);

            // Favori varsa silinir
            var favoriler = await _context.Favoriler
                .Where(x =>
                    x.KullaniciId == kullaniciId &&
                    x.KursId == id)
                .ToListAsync();

            _context.Favoriler.RemoveRange(favoriler);

            // Değerlendirme varsa silinir
            var degerlendirmeler = await _context.KursDegerlendirmeleri
                .Where(x =>
                    x.KullaniciId == kullaniciId &&
                    x.KursId == id)
                .ToListAsync();

            _context.KursDegerlendirmeleri.RemoveRange(degerlendirmeler);

            // En son kurs kaydı silinir
            _context.KursKayitlari.Remove(kursKaydi);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["OgrenciBasari"] = "Kurs kaydınız ve bu kursa ait tüm ilerleme bilgileriniz silindi.";
        }
        catch
        {
            await transaction.RollbackAsync();

            TempData["OgrenciHata"] = "Kurs kaydı silinirken bir hata oluştu.";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Kurslarim));
    }

    [HttpGet]
    public async Task<IActionResult> Kurslarim(
        string? arama,
        string durum = "tum",
        int? kategoriId = null,
        string? siralama = null,
        int sayfa = 1)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        const int sayfaBasinaKayit = 5;

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        durum = string.IsNullOrWhiteSpace(durum)
            ? "tum"
            : durum.Trim().ToLower();

        if (durum != "tum" &&
            durum != "devam" &&
            durum != "tamamlanan" &&
            durum != "favoriler")
        {
            durum = "tum";
        }

        kategoriId = kategoriId.GetValueOrDefault() > 0
            ? kategoriId
            : null;

        siralama = siralama switch
        {
            "kayitYeni" => "guncel",
            "kayitEski" => "eski",
            "ad" => "az",
            _ => siralama
        };

        string[] siralamaSecenekleri =
        [
            "guncel",
            "eski",
            "az",
            "za",
            "puanYuksek",
            "puanDusuk"
        ];

        if (string.IsNullOrWhiteSpace(siralama) || !siralamaSecenekleri.Contains(siralama))
        {
            siralama = "guncel";
        }

        if (sayfa < 1)
        {
            sayfa = 1;
        }

        var kategoriler = await _context.KursKategorileri
            .AsNoTracking()
            .Where(x =>
                _context.KursKayitlari.Any(k =>
                    k.KullaniciId == kullaniciId &&
                    k.AktifMi &&
                    k.KursId == x.KursId))
            .GroupBy(x => new
            {
                x.KategoriId,
                x.Kategori.KategoriAdi
            })
            .Select(x => new OgrenciKursKategoriViewModel
            {
                KategoriId = x.Key.KategoriId,
                KategoriAdi = x.Key.KategoriAdi
            })
            .OrderBy(x => x.KategoriAdi)
            .ToListAsync();

        // Üst kartlar için tüm aktif kayıtlı kurslar
        var tumKursOzetleri = await _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                x.AktifMi)
            .Select(x => new
            {
                x.TamamlandiMi,

                ToplamDersSayisi = x.Kurs.Dersler
                    .Count(d =>
                        d.AktifMi &&
                        !d.SistemDersiMi),

                TamamlananDersSayisi = x.DersIlerlemeleri
                    .Count(i =>
                        i.TamamlandiMi &&
                        i.Ders.AktifMi &&
                        !i.Ders.SistemDersiMi)
            })
            .ToListAsync();

        int toplamKursSayisi = tumKursOzetleri.Count;
        int tamamlananKursSayisi = tumKursOzetleri.Count(x => x.TamamlandiMi);
        int devamEdenKursSayisi = tumKursOzetleri.Count(x => !x.TamamlandiMi);

        int ortalamaIlerlemeYuzdesi = 0;

        if (tumKursOzetleri.Any())
        {
            var ilerlemeler = tumKursOzetleri.Select(x =>
            {
                if (x.ToplamDersSayisi == 0)
                {
                    return 0;
                }

                return (int)Math.Round((x.TamamlananDersSayisi * 100.0) / x.ToplamDersSayisi);
            });

            ortalamaIlerlemeYuzdesi = (int)Math.Round(ilerlemeler.Average());
        }

        var query = _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                x.AktifMi);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.Kurs.KursAdi.Contains(arama) ||
                x.Kurs.Egitmen.Ad.Contains(arama) ||
                x.Kurs.Egitmen.Soyad.Contains(arama) ||
                x.Kurs.KursKategorileri.Any(k => k.Kategori.KategoriAdi.Contains(arama)));
        }

        if (kategoriId.HasValue)
        {
            query = query.Where(x =>
                x.Kurs.KursKategorileri.Any(k => k.KategoriId == kategoriId.Value));
        }

        if (durum == "devam")
        {
            query = query.Where(x => !x.TamamlandiMi);
        }
        else if (durum == "tamamlanan")
        {
            query = query.Where(x => x.TamamlandiMi);
        }
        else if (durum == "favoriler")
        {
            query = query.Where(x =>
                _context.Favoriler.Any(f =>
                    f.KullaniciId == kullaniciId &&
                    f.KursId == x.KursId));
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

        var queryOrdered = siralama switch
        {
            "eski" => query
                .OrderBy(x => x.Kurs.GuncellemeTarihi ?? x.Kurs.OlusturmaTarihi)
                .ThenBy(x => x.Kurs.KursAdi),

            "az" => query
                .OrderBy(x => x.Kurs.KursAdi)
                .ThenByDescending(x => x.Kurs.GuncellemeTarihi ?? x.Kurs.OlusturmaTarihi),

            "za" => query
                .OrderByDescending(x => x.Kurs.KursAdi)
                .ThenByDescending(x => x.Kurs.GuncellemeTarihi ?? x.Kurs.OlusturmaTarihi),

            "puanYuksek" => query
                .OrderByDescending(x => x.Kurs.KursDegerlendirmeleri.Any()
                    ? x.Kurs.KursDegerlendirmeleri.Average(d => (double)d.Puan)
                    : 0)
                .ThenByDescending(x => x.Kurs.KursDegerlendirmeleri.Count)
                .ThenBy(x => x.Kurs.KursAdi),

            "puanDusuk" => query
                .OrderBy(x => x.Kurs.KursDegerlendirmeleri.Any()
                    ? x.Kurs.KursDegerlendirmeleri.Average(d => (double)d.Puan)
                    : 0)
                .ThenByDescending(x => x.Kurs.KursDegerlendirmeleri.Count)
                .ThenBy(x => x.Kurs.KursAdi),

            _ => query
                .OrderByDescending(x => x.Kurs.GuncellemeTarihi ?? x.Kurs.OlusturmaTarihi)
                .ThenBy(x => x.Kurs.KursAdi)
        };

        var kurslar = await queryOrdered
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new OgrenciKursOzetViewModel
            {
                KursKayitId = x.KursKayitId,
                KursId = x.KursId,

                KursAdi = x.Kurs.KursAdi,
                KapakGorselUrl = x.Kurs.KapakGorselUrl,

                EgitmenAdSoyad = x.Kurs.Egitmen.Ad + " " + x.Kurs.Egitmen.Soyad,

                DurumId = x.Kurs.DurumId,
                DurumAdi = x.Kurs.Durum.DurumAdi,

                KayitTarihi = x.KayitTarihi,

                KursTamamlandiMi = x.TamamlandiMi,

                ToplamDersSayisi = x.Kurs.Dersler
                    .Count(d =>
                        d.AktifMi &&
                        !d.SistemDersiMi),

                TamamlananDersSayisi = x.DersIlerlemeleri
                    .Count(i =>
                        i.TamamlandiMi &&
                        i.Ders.AktifMi &&
                        !i.Ders.SistemDersiMi),

                FavorideMi = _context.Favoriler.Any(f =>
                    f.KullaniciId == kullaniciId &&
                    f.KursId == x.KursId),

                DegerlendirmeVarMi = _context.KursDegerlendirmeleri.Any(d =>
                    d.KullaniciId == kullaniciId &&
                    d.KursId == x.KursId),

                KendiPuan = _context.KursDegerlendirmeleri
                    .Where(d =>
                        d.KullaniciId == kullaniciId &&
                        d.KursId == x.KursId)
                    .Select(d => (int?)d.Puan)
                    .FirstOrDefault(),

                KendiYorumMetni = _context.KursDegerlendirmeleri
                    .Where(d =>
                        d.KullaniciId == kullaniciId &&
                        d.KursId == x.KursId)
                    .Select(d => d.YorumMetni)
                    .FirstOrDefault(),

                Kategoriler = x.Kurs.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList()
            })
            .ToListAsync();

        var kursKayitIdleri = kurslar
            .Select(x => x.KursKayitId)
            .ToList();

        var sinavKatilimlari = await _context.SinavKatilimlari
            .AsNoTracking()
            .Where(x => kursKayitIdleri.Contains(x.KursKayitId))
            .OrderByDescending(x => x.BaslamaTarihi)
            .Select(x => new
            {
                x.KursKayitId,
                x.BitisTarihi,
                x.AlinanPuan,
                x.GectiMi
            })
            .ToListAsync();

        var sonSinavlar = sinavKatilimlari
            .GroupBy(x => x.KursKayitId)
            .ToDictionary(
                x => x.Key,
                x => x.First()
            );

        foreach (var kurs in kurslar)
        {
            kurs.IlerlemeYuzdesi = kurs.ToplamDersSayisi == 0
                ? 0
                : (int)Math.Round((kurs.TamamlananDersSayisi * 100.0) / kurs.ToplamDersSayisi);

            if (sonSinavlar.TryGetValue(kurs.KursKayitId, out var sonSinav))
            {
                kurs.SonSinavPuani = sonSinav.AlinanPuan;
                kurs.SinavdanGectiMi = sonSinav.GectiMi;

                kurs.SinavDurumu = sonSinav.BitisTarihi == null
                    ? "Devam ediyor"
                    : sonSinav.GectiMi == true
                        ? "Geçti"
                        : "Kaldı";
            }
            else
            {
                kurs.SinavDurumu = "Henüz girmedi";
            }
        }

        var model = new OgrenciKurslarimViewModel
        {
            Kurslar = kurslar,

            ToplamKursSayisi = toplamKursSayisi,
            TamamlananKursSayisi = tamamlananKursSayisi,
            DevamEdenKursSayisi = devamEdenKursSayisi,
            OrtalamaIlerlemeYuzdesi = ortalamaIlerlemeYuzdesi,

            Arama = arama,
            Durum = durum,
            KategoriId = kategoriId,
            Siralama = siralama,
            Kategoriler = kategoriler,

            Sayfa = sayfa,
            ToplamSayfa = toplamSayfa,
            ToplamKayit = toplamKayit,
            SayfaBasinaKayit = sayfaBasinaKayit
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DegerlendirmeSil(int kursId, string? returnUrl = null)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        bool aktifKayitVar = await _context.KursKayitlari
            .AsNoTracking()
            .AnyAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == kursId &&
                x.AktifMi);

        if (!aktifKayitVar)
        {
            TempData["OgrenciHata"] = "Sadece kayıtlı olduğunuz kurslardaki değerlendirmenizi silebilirsiniz.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Kurslarim));
        }

        var degerlendirme = await _context.KursDegerlendirmeleri
            .FirstOrDefaultAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == kursId);

        if (degerlendirme == null)
        {
            TempData["OgrenciHata"] = "Silinecek değerlendirme bulunamadı.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Kurslarim));
        }

        _context.KursDegerlendirmeleri.Remove(degerlendirme);
        await _context.SaveChangesAsync();

        TempData["OgrenciBasari"] = "Değerlendirmeniz silindi.";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Kurslarim));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Degerlendir(int kursId, int puan, string? yorumMetni, string? returnUrl = null)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (puan < 1 || puan > 5)
        {
            TempData["OgrenciHata"] = "Puan 1 ile 5 arasında olmalıdır.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Kurslarim));
        }

        byte puanByte = (byte)puan;

        bool aktifKayitVar = await _context.KursKayitlari
            .AsNoTracking()
            .AnyAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == kursId &&
                x.AktifMi);

        if (!aktifKayitVar)
        {
            TempData["OgrenciHata"] = "Sadece kayıtlı olduğunuz kursları değerlendirebilirsiniz.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Kurslarim));
        }

        var kursBilgisi = await _context.Kurslar
            .AsNoTracking()
            .Where(x => x.KursId == kursId)
            .Select(x => new
            {
                x.KursAdi,
                x.EgitmenId
            })
            .FirstOrDefaultAsync();

        if (kursBilgisi == null)
        {
            TempData["OgrenciHata"] = "Kurs bulunamadı.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Kurslarim));
        }

        yorumMetni = string.IsNullOrWhiteSpace(yorumMetni)
            ? null
            : yorumMetni.Trim();

        var mevcutDegerlendirme = await _context.KursDegerlendirmeleri
            .FirstOrDefaultAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == kursId);

        if (mevcutDegerlendirme == null)
        {
            var yeniDegerlendirme = new KursDegerlendirmesi
            {
                KullaniciId = kullaniciId,
                KursId = kursId,
                Puan = puanByte,
                YorumMetni = yorumMetni,
                DegerlendirmeTarihi = DateTime.Now
            };

            _context.KursDegerlendirmeleri.Add(yeniDegerlendirme);

            await _bildirimService.BildirimOlusturAsync(
                kursBilgisi.EgitmenId,
                "Bilgilendirme",
                "Kursunuza yeni değerlendirme yapıldı",
                $"\"{kursBilgisi.KursAdi}\" kursunuza yeni bir öğrenci değerlendirmesi yapıldı."
            );

            TempData["OgrenciBasari"] = "Kurs değerlendirmesi kaydedildi.";
        }
        else
        {
            mevcutDegerlendirme.Puan = puanByte;
            mevcutDegerlendirme.YorumMetni = yorumMetni;
            mevcutDegerlendirme.DegerlendirmeTarihi = DateTime.Now;

            TempData["OgrenciBasari"] = "Kurs değerlendirmesi güncellendi.";
        }

        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Kurslarim));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FavoriDegistir(int id, string? returnUrl = null)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        bool aktifKayitVar = await _context.KursKayitlari
            .AsNoTracking()
            .AnyAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == id &&
                x.AktifMi);

        if (!aktifKayitVar)
        {
            TempData["OgrenciHata"] = "Sadece kayıtlı olduğunuz kursları favorilere ekleyebilirsiniz.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Kurslarim));
        }

        var favori = await _context.Favoriler
            .FirstOrDefaultAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == id);

        if (favori == null)
        {
            var yeniFavori = new Favori
            {
                KullaniciId = kullaniciId,
                KursId = id,
                EklenmeTarihi = DateTime.Now
            };

            _context.Favoriler.Add(yeniFavori);

            TempData["OgrenciBasari"] = "Kurs favorilere eklendi.";
        }
        else
        {
            _context.Favoriler.Remove(favori);

            TempData["OgrenciBasari"] = "Kurs favorilerden çıkarıldı.";
        }

        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Kurslarim));
    }

    [HttpGet]
    public async Task<IActionResult> Detay(int id, int yorumSayfa = 1)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        const int yorumSayfaBasinaKayit = 5;

        if (yorumSayfa < 1)
        {
            yorumSayfa = 1;
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
            .FirstOrDefaultAsync(x => x.KursId == id);

        if (kurs == null)
        {
            TempData["OgrenciHata"] = "Kurs bulunamadı veya yayında değil.";
            return RedirectToAction(nameof(Kesfet));
        }

        if (kurs.DurumId == 7)
        {
            TempData["OgrenciHata"] = "Bu kurs şu anda güncelleniyor.";
            return RedirectToAction(nameof(Kurslarim));
        }

        if (kurs.DurumId != 5)
        {
            TempData["OgrenciHata"] = "Kurs bulunamadı veya yayında değil.";
            return RedirectToAction(nameof(Kesfet));
        }

        bool kayitliMi = await _context.KursKayitlari
            .AsNoTracking()
            .AnyAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == id &&
                x.AktifMi);

        bool kendiKursuMu = kurs.EgitmenId == kullaniciId;

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
}
