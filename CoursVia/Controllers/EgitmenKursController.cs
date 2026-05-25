using CoursVia.Data;
using CoursVia.Models;
using CoursVia.ViewModels.Egitmen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers;

[Authorize(Roles = "Eğitmen")]
public class EgitmenKursController : EgitmenBaseController
{
    private const long MaksKapakGorselBoyutu = 5 * 1024 * 1024;
    private static readonly string[] IzinliKapakGorselUzantilari = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly IWebHostEnvironment _webHostEnvironment;

    public EgitmenKursController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        : base(context)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    // =========================
    // KURSLARIM
    // =========================

    [HttpGet]
    public async Task<IActionResult> Kurslarim(
        string? arama,
        int? durumId,
        int? kategoriId,
        string? siralama,
        int sayfa = 1)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        const int sayfaBasinaKayit = 6;

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        if (sayfa < 1)
        {
            sayfa = 1;
        }

        kategoriId = kategoriId.GetValueOrDefault() > 0
            ? kategoriId
            : null;

        siralama = siralama switch
        {
            "yeni" => "guncel",
            "ad" => "az",
            "ogrenci" => "ogrenciCok",
            "puan" => "puanYuksek",
            _ => siralama
        };

        string[] siralamaSecenekleri =
        [
            "guncel",
            "eski",
            "az",
            "za",
            "puanYuksek",
            "puanDusuk",
            "ogrenciCok",
            "ogrenciAz"
        ];

        if (string.IsNullOrWhiteSpace(siralama) || !siralamaSecenekleri.Contains(siralama))
        {
            siralama = "guncel";
        }

        var egitmenProfili = await _context.EgitmenProfilleri
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (egitmenProfili == null || egitmenProfili.DurumId != 8)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var query = _context.Kurslar
            .AsNoTracking()
            .Include(x => x.Durum)
            .Include(x => x.KursKategorileri)
                .ThenInclude(x => x.Kategori)
            .Include(x => x.Bolumler)
            .Include(x => x.Dersler)
            .Where(x => x.EgitmenId == kullaniciId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            string aramaKucuk = arama.ToLower();

            query = query.Where(x =>
                x.KursAdi.ToLower().Contains(aramaKucuk) ||
                (x.Aciklama != null && x.Aciklama.ToLower().Contains(aramaKucuk)) ||
                x.KursKategorileri.Any(k => k.Kategori.KategoriAdi.ToLower().Contains(aramaKucuk)));
        }

        if (durumId.HasValue)
        {
            query = query.Where(x => x.DurumId == durumId.Value);
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

        var queryOrdered = siralama switch
        {
            "eski" => query
                .OrderBy(x => x.GuncellemeTarihi ?? x.OlusturmaTarihi)
                .ThenBy(x => x.KursAdi),

            "az" => query
                .OrderBy(x => x.KursAdi)
                .ThenByDescending(x => x.GuncellemeTarihi ?? x.OlusturmaTarihi),

            "za" => query
                .OrderByDescending(x => x.KursAdi)
                .ThenByDescending(x => x.GuncellemeTarihi ?? x.OlusturmaTarihi),

            "ogrenciCok" => query
                .OrderByDescending(x => _context.KursKayitlari.Count(k =>
                    k.KursId == x.KursId &&
                    k.AktifMi))
                .ThenByDescending(x => x.GuncellemeTarihi ?? x.OlusturmaTarihi),

            "ogrenciAz" => query
                .OrderBy(x => _context.KursKayitlari.Count(k =>
                    k.KursId == x.KursId &&
                    k.AktifMi))
                .ThenByDescending(x => x.GuncellemeTarihi ?? x.OlusturmaTarihi),

            "puanYuksek" => query
                .OrderByDescending(x => x.KursDegerlendirmeleri.Any()
                    ? x.KursDegerlendirmeleri.Average(d => (double)d.Puan)
                    : 0)
                .ThenByDescending(x => x.KursDegerlendirmeleri.Count)
                .ThenBy(x => x.KursAdi),

            "puanDusuk" => query
                .OrderBy(x => x.KursDegerlendirmeleri.Any()
                    ? x.KursDegerlendirmeleri.Average(d => (double)d.Puan)
                    : 0)
                .ThenByDescending(x => x.KursDegerlendirmeleri.Count)
                .ThenBy(x => x.KursAdi),

            _ => query
                .OrderByDescending(x => x.GuncellemeTarihi ?? x.OlusturmaTarihi)
                .ThenBy(x => x.KursAdi)
        };

        var kurslar = await queryOrdered
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .ToListAsync();

        var kursIdleri = kurslar
        .Select(x => x.KursId)
        .ToList();

        var puanOzetleri = await _context.KursDegerlendirmeleri
            .AsNoTracking()
            .Where(x => kursIdleri.Contains(x.KursId))
            .GroupBy(x => x.KursId)
            .Select(x => new
            {
                KursId = x.Key,
                OrtalamaPuan = x.Average(y => (double)y.Puan),
                YorumSayisi = x.Count()
            })
            .ToListAsync();

        ViewBag.KursOrtalamaPuanlari = puanOzetleri
            .ToDictionary(
                x => x.KursId,
                x => Math.Round(x.OrtalamaPuan, 1)
            );

        ViewBag.KursYorumSayilari = puanOzetleri
            .ToDictionary(
                x => x.KursId,
                x => x.YorumSayisi
            );

        var ogrenciSayilari = await _context.KursKayitlari
            .AsNoTracking()
            .Where(x => kursIdleri.Contains(x.KursId) && x.AktifMi)
            .GroupBy(x => x.KursId)
            .Select(x => new
            {
                KursId = x.Key,
                OgrenciSayisi = x.Count()
            })
            .ToListAsync();

        ViewBag.KursOgrenciSayilari = ogrenciSayilari
            .ToDictionary(
                x => x.KursId,
                x => x.OgrenciSayisi
            );

        ViewBag.Arama = arama;
        ViewBag.DurumId = durumId;
        ViewBag.KategoriId = kategoriId;
        ViewBag.Siralama = siralama;

        ViewBag.Sayfa = sayfa;
        ViewBag.ToplamSayfa = toplamSayfa;
        ViewBag.ToplamKayit = toplamKayit;
        ViewBag.SayfaBasinaKayit = sayfaBasinaKayit;
        ViewBag.OncekiSayfaVar = sayfa > 1;
        ViewBag.SonrakiSayfaVar = sayfa < toplamSayfa;

        ViewBag.Durumlar = await _context.Durumlar
            .AsNoTracking()
            .Where(x =>
                x.DurumId == 2 ||
                x.DurumId == 3 ||
                x.DurumId == 4 ||
                x.DurumId == 5 ||
                x.DurumId == 6 ||
                x.DurumId == 7)
            .OrderBy(x => x.DurumId)
            .ToListAsync();

        ViewBag.Kategoriler = await _context.KursKategorileri
            .AsNoTracking()
            .Where(x => x.Kurs.EgitmenId == kullaniciId)
            .GroupBy(x => new
            {
                x.KategoriId,
                x.Kategori.KategoriAdi
            })
            .Select(x => new Kategori
            {
                KategoriId = x.Key.KategoriId,
                KategoriAdi = x.Key.KategoriAdi
            })
            .OrderBy(x => x.KategoriAdi)
            .ToListAsync();



        ViewBag.ToplamKurs = await _context.Kurslar
            .AsNoTracking()
            .CountAsync(x => x.EgitmenId == kullaniciId);

        ViewBag.YayindakiKurs = await _context.Kurslar
            .AsNoTracking()
            .CountAsync(x =>
                x.EgitmenId == kullaniciId &&
                x.DurumId == 5);

        ViewBag.TaslakKurs = await _context.Kurslar
            .AsNoTracking()
            .CountAsync(x =>
                x.EgitmenId == kullaniciId &&
                x.DurumId == 3);

        return View(kurslar);
    }

    // =========================
    // KURS OLUŞTUR
    // =========================

    [HttpGet]
    public async Task<IActionResult> KursOlustur()
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var egitmenProfili = await _context.EgitmenProfilleri
            .Include(x => x.EgitmenBranslari)
                .ThenInclude(x => x.Kategori)
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (egitmenProfili == null || egitmenProfili.DurumId != 8)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var model = new KursOlusturViewModel
        {
            Kategoriler = egitmenProfili.EgitmenBranslari
                .Select(x => x.Kategori)
                .OrderBy(x => x.KategoriAdi)
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KursOlustur(KursOlusturViewModel model)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var egitmenProfili = await _context.EgitmenProfilleri
            .Include(x => x.EgitmenBranslari)
                .ThenInclude(x => x.Kategori)
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (egitmenProfili == null || egitmenProfili.DurumId != 8)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        model.SeciliKategoriIdleri ??= new List<int>();

        model.SeciliKategoriIdleri = model.SeciliKategoriIdleri
            .Distinct()
            .ToList();

        var egitmenKategoriIdleri = egitmenProfili.EgitmenBranslari
            .Select(x => x.KategoriId)
            .ToList();

        if (!egitmenKategoriIdleri.Any())
        {
            ModelState.AddModelError(
                nameof(model.SeciliKategoriIdleri),
                "Kurs oluşturabilmek için en az bir eğitmen branşınız bulunmalıdır."
            );
        }

        if (!model.SeciliKategoriIdleri.Any())
        {
            ModelState.AddModelError(
                nameof(model.SeciliKategoriIdleri),
                "En az bir kategori seçmelisiniz."
            );
        }

        bool yetkisizKategoriVarMi = model.SeciliKategoriIdleri
            .Any(kategoriId => !egitmenKategoriIdleri.Contains(kategoriId));

        if (yetkisizKategoriVarMi)
        {
            ModelState.AddModelError(
                nameof(model.SeciliKategoriIdleri),
                "Sadece kendi eğitmen branşlarınıza ait kategorilerden seçim yapabilirsiniz."
            );
        }

        bool urlGirildiMi = !string.IsNullOrWhiteSpace(model.KapakGorselUrl);
        bool dosyaYuklendiMi = model.KapakGorselDosya != null && model.KapakGorselDosya.Length > 0;

        if (!urlGirildiMi && !dosyaYuklendiMi)
        {
            ModelState.AddModelError(
                nameof(model.KapakGorselUrl),
                "Kapak görseli için URL girmeli veya dosya yüklemelisiniz."
            );
        }

        if (dosyaYuklendiMi)
        {
            KapakGorselDosyasiniDogrula(model.KapakGorselDosya!);
        }

        if (!ModelState.IsValid)
        {
            model.Kategoriler = egitmenProfili.EgitmenBranslari
                .Select(x => x.Kategori)
                .OrderBy(x => x.KategoriAdi)
                .ToList();

            return View(model);
        }

        string? yuklenenDosyaYolu = null;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            string kapakGorselYolu;

            if (dosyaYuklendiMi)
            {
                string uzanti = Path.GetExtension(model.KapakGorselDosya!.FileName).ToLower();

                string klasorYolu = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "uploads",
                    "kurs-kapaklari"
                );

                if (!Directory.Exists(klasorYolu))
                {
                    Directory.CreateDirectory(klasorYolu);
                }

                string dosyaAdi = $"{Guid.NewGuid()}{uzanti}";
                string dosyaYolu = Path.Combine(klasorYolu, dosyaAdi);

                using (var stream = new FileStream(dosyaYolu, FileMode.Create))
                {
                    await model.KapakGorselDosya.CopyToAsync(stream);
                }

                yuklenenDosyaYolu = dosyaYolu;
                kapakGorselYolu = $"/uploads/kurs-kapaklari/{dosyaAdi}";
            }
            else
            {
                kapakGorselYolu = model.KapakGorselUrl!.Trim();
            }

            var kurs = new Kurs
            {
                EgitmenId = kullaniciId,
                DurumId = 3, // Taslak

                KursAdi = model.KursAdi.Trim(),

                Aciklama = string.IsNullOrWhiteSpace(model.Aciklama)
                    ? null
                    : model.Aciklama.Trim(),

                KapakGorselUrl = kapakGorselYolu,

                OlusturmaTarihi = DateTime.Now
            };

            _context.Kurslar.Add(kurs);
            await _context.SaveChangesAsync();

            foreach (var kategoriId in model.SeciliKategoriIdleri)
            {
                _context.KursKategorileri.Add(new KursKategorisi
                {
                    KursId = kurs.KursId,
                    KategoriId = kategoriId
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["KursBasari"] = "Kurs taslak olarak oluşturuldu. Şimdi bölüm, ders, sınav ve soru içeriklerini ekleyebilirsiniz.";

            return RedirectToAction(nameof(KursIcerik), new { id = kurs.KursId });
        }
        catch
        {
            await transaction.RollbackAsync();

            if (!string.IsNullOrWhiteSpace(yuklenenDosyaYolu) && System.IO.File.Exists(yuklenenDosyaYolu))
            {
                System.IO.File.Delete(yuklenenDosyaYolu);
            }

            ModelState.AddModelError(
                "",
                "Kurs oluşturulurken bir hata oluştu. Lütfen tekrar deneyin."
            );

            model.Kategoriler = egitmenProfili.EgitmenBranslari
                .Select(x => x.Kategori)
                .OrderBy(x => x.KategoriAdi)
                .ToList();

            return View(model);
        }
    }

    // =========================
    // KURS DÜZENLEME (TEMEL BİLGİLER)
    // =========================

    [HttpGet]
    public async Task<IActionResult> KursDuzenle(int id)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var egitmenProfili = await _context.EgitmenProfilleri
            .Include(x => x.EgitmenBranslari)
                .ThenInclude(eb => eb.Kategori)
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (egitmenProfili == null)
            return RedirectToAction("AccessDenied", "Account");

        var kurs = await _context.Kurslar
            .Include(x => x.KursKategorileri)
            .FirstOrDefaultAsync(x => x.KursId == id && x.EgitmenId == kullaniciId);

        if (kurs == null)
            return RedirectToAction("AccessDenied", "Account");

        if (!KursDuzenlenebilirMi(kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction(nameof(Kurslarim));
        }

        var model = new KursDuzenleViewModel
        {
            KursId = kurs.KursId,
            KursAdi = kurs.KursAdi,
            Aciklama = kurs.Aciklama,
            MevcutKapakGorselUrl = kurs.KapakGorselUrl,
            SeciliKategoriIdleri = kurs.KursKategorileri.Select(k => k.KategoriId).ToList(),
            Kategoriler = egitmenProfili.EgitmenBranslari.Select(x => x.Kategori).OrderBy(x => x.KategoriAdi).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KursDuzenle(KursDuzenleViewModel model)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var egitmenProfili = await _context.EgitmenProfilleri
            .Include(x => x.EgitmenBranslari)
                .ThenInclude(eb => eb.Kategori)
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (egitmenProfili == null)
            return RedirectToAction("AccessDenied", "Account");

        var kurs = await _context.Kurslar
            .Include(x => x.KursKategorileri)
            .FirstOrDefaultAsync(x => x.KursId == model.KursId && x.EgitmenId == kullaniciId);

        if (kurs == null)
            return RedirectToAction("AccessDenied", "Account");

        if (!KursDuzenlenebilirMi(kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction(nameof(Kurslarim));
        }

        var egitmenKategoriIdleri = egitmenProfili.EgitmenBranslari
            .Select(x => x.KategoriId)
            .ToList();

        if (!model.SeciliKategoriIdleri.Any())
            ModelState.AddModelError(nameof(model.SeciliKategoriIdleri), "En az bir kategori seçmelisiniz.");

        if (model.SeciliKategoriIdleri.Any(k => !egitmenKategoriIdleri.Contains(k)))
            ModelState.AddModelError(nameof(model.SeciliKategoriIdleri), "Sadece yetkili olduğunuz branşları seçebilirsiniz.");

        bool dosyaYuklendiMi = model.KapakGorselDosya != null && model.KapakGorselDosya.Length > 0;
        bool urlGirildiMi = !string.IsNullOrWhiteSpace(model.KapakGorselUrl);

        if (dosyaYuklendiMi)
        {
            KapakGorselDosyasiniDogrula(model.KapakGorselDosya!);
        }

        if (!ModelState.IsValid)
        {
            model.MevcutKapakGorselUrl = kurs.KapakGorselUrl;
            model.Kategoriler = egitmenProfili.EgitmenBranslari.Select(x => x.Kategori).OrderBy(x => x.KategoriAdi).ToList();
            return View(model);
        }

        string eskiKapakGorselUrl = kurs.KapakGorselUrl;
        string? yeniYuklenenKapakFizikselYolu = null;
        var silinecekFizikselDosyalar = new List<string>();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            string sonGorselUrl = kurs.KapakGorselUrl;

            if (dosyaYuklendiMi)
            {
                string uzanti = Path.GetExtension(model.KapakGorselDosya!.FileName).ToLower();

                string klasorYolu = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "kurs-kapaklari");
                if (!Directory.Exists(klasorYolu)) Directory.CreateDirectory(klasorYolu);

                string dosyaAdi = $"{Guid.NewGuid()}{uzanti}";
                string tamYol = Path.Combine(klasorYolu, dosyaAdi);

                using (var fileStream = new FileStream(tamYol, FileMode.Create))
                {
                    await model.KapakGorselDosya.CopyToAsync(fileStream);
                }

                yeniYuklenenKapakFizikselYolu = tamYol;
                sonGorselUrl = $"/uploads/kurs-kapaklari/{dosyaAdi}";
            }
            else if (urlGirildiMi)
            {
                sonGorselUrl = model.KapakGorselUrl!.Trim();
            }

            if (!string.Equals(sonGorselUrl, eskiKapakGorselUrl, StringComparison.Ordinal))
            {
                DosyaYoluEkleEgerUploadIse(
                    silinecekFizikselDosyalar,
                    eskiKapakGorselUrl,
                    "/uploads/kurs-kapaklari/"
                );
            }

            kurs.KursAdi = model.KursAdi.Trim();
            kurs.Aciklama = string.IsNullOrWhiteSpace(model.Aciklama) ? null : model.Aciklama.Trim();
            kurs.KapakGorselUrl = sonGorselUrl;
            kurs.GuncellemeTarihi = DateTime.Now;

            _context.KursKategorileri.RemoveRange(kurs.KursKategorileri);
            foreach (var katId in model.SeciliKategoriIdleri)
            {
                _context.KursKategorileri.Add(new KursKategorisi { KursId = kurs.KursId, KategoriId = katId });
            }

            OnayliKursuTaslakYap(kurs);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            YuklenenDosyalariSil(silinecekFizikselDosyalar);

            TempData["KursBasari"] = "Kurs bilgileri başarıyla güncellendi.";
            return RedirectToAction(nameof(KursIcerik), new { id = kurs.KursId });
        }
        catch
        {
            await transaction.RollbackAsync();

            if (!string.IsNullOrWhiteSpace(yeniYuklenenKapakFizikselYolu))
            {
                YuklenenDosyalariSil(new List<string> { yeniYuklenenKapakFizikselYolu });
            }

            ModelState.AddModelError("", "Güncelleme sırasında bir hata oluştu.");
            model.MevcutKapakGorselUrl = kurs.KapakGorselUrl;
            model.Kategoriler = egitmenProfili.EgitmenBranslari.Select(x => x.Kategori).OrderBy(x => x.KategoriAdi).ToList();
            return View(model);
        }
    }

    // =========================
    // KURS İÇERİK YÖNETİMİ
    // =========================

    [HttpGet]
    public async Task<IActionResult> KursIcerik(int id)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kurs = await _context.Kurslar
            .Include(x => x.Durum)
            .Include(x => x.KursKategorileri)
                .ThenInclude(x => x.Kategori)
            .Include(x => x.Bolumler.OrderBy(b => b.SiraNo))
                .ThenInclude(b => b.Dersler
                    .Where(d => d.AktifMi && !d.SistemDersiMi)
                    .OrderBy(d => d.SiraNo))
                    .ThenInclude(d => d.DersMateryalleri)
            .FirstOrDefaultAsync(x => x.KursId == id && x.EgitmenId == kullaniciId);

        if (kurs == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(kurs);
    }

    [HttpGet]
    public async Task<IActionResult> OnIzle(int id, int soruSayfa = 1, int yorumSayfa = 1)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var model = await KursOnIzleModeliOlusturAsync(id, kullaniciId, soruSayfa, yorumSayfa);

        if (model == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YayindakiKursuDuzenle(int id)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kurs = await _context.Kurslar
            .FirstOrDefaultAsync(x => x.KursId == id && x.EgitmenId == kullaniciId);

        if (kurs == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!KursDuzenlenebilirMi(kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction(nameof(Kurslarim));
        }

        if (kurs.DurumId == 5)
        {
            kurs.DurumId = 3;
            kurs.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["KursBasari"] = "Yayındaki kurs düzenleme için taslağa alındı.";
        }

        return RedirectToAction(nameof(KursIcerik), new { id = kurs.KursId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnaydanGeriCek(int id)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kurs = await _context.Kurslar
            .FirstOrDefaultAsync(x => x.KursId == id && x.EgitmenId == kullaniciId);

        if (kurs == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (kurs.DurumId != 4)
        {
            TempData["KursHata"] = "Sadece onay bekleyen kurslar geri çekilebilir.";
            return RedirectToAction(nameof(Kurslarim));
        }

        kurs.DurumId = 3;
        kurs.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["KursBasari"] = "Kurs onaydan geri çekildi ve taslağa alındı.";
        return RedirectToAction(nameof(Kurslarim));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KursSil(int id)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kurs = await _context.Kurslar
            .FirstOrDefaultAsync(x => x.KursId == id && x.EgitmenId == kullaniciId);

        if (kurs == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (kurs.DurumId == 4)
        {
            TempData["KursHata"] = "Onay bekleyen kurs silinemez. Önce onaydan geri çekmelisiniz.";
            return RedirectToAction(nameof(Kurslarim));
        }

        if (kurs.DurumId == 2)
        {
            TempData["KursHata"] = "Pasif kurs tekrar silinemez.";
            return RedirectToAction(nameof(Kurslarim));
        }

        bool silinebilirDurumMu = kurs.DurumId == 3 ||
                                  kurs.DurumId == 5 ||
                                  kurs.DurumId == 6 ||
                                  kurs.DurumId == 7;

        if (!silinebilirDurumMu)
        {
            TempData["KursHata"] = "Bu kurs şu an silinebilir durumda değil.";
            return RedirectToAction(nameof(Kurslarim));
        }

        bool gecmisVeriVarMi = await KursGecmisVerisiVarMiAsync(kurs.KursId);

        if (gecmisVeriVarMi)
        {
            kurs.DurumId = 2;
            kurs.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["KursBasari"] = "Kurs geçmiş veri içerdiği için fiziksel silinmedi, pasife alındı.";
            return RedirectToAction(nameof(Kurslarim));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var silinecekFizikselDosyalar = new List<string>();

        try
        {
            await KursuFizikselSilAsync(kurs.KursId, silinecekFizikselDosyalar);
            await transaction.CommitAsync();

            YuklenenDosyalariSil(silinecekFizikselDosyalar);

            TempData["KursBasari"] = "Kurs başarıyla silindi.";
            return RedirectToAction(nameof(Kurslarim));
        }
        catch
        {
            await transaction.RollbackAsync();

            TempData["KursHata"] = "Kurs silinirken bir hata oluştu. Lütfen tekrar deneyin.";
            return RedirectToAction(nameof(Kurslarim));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> YayinaGonder(int id)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kurs = await _context.Kurslar
            .Include(x => x.Bolumler)
            .Include(x => x.Dersler)
            .Include(x => x.Sinav)
            .FirstOrDefaultAsync(x => x.KursId == id && x.EgitmenId == kullaniciId);

        if (kurs == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var eksikler = await YayinEksikleriniGetirAsync(kurs);

        if (eksikler.Any())
        {
            TempData["KursHata"] = eksikler.First();
            return RedirectToAction(nameof(OnIzle), new { id });
        }

        kurs.DurumId = 4; // Onay Bekliyor
        kurs.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["KursBasari"] = "Kurs admin onayına gönderildi.";
        return RedirectToAction(nameof(Kurslarim));
    }

    private async Task<KursOnIzleViewModel?> KursOnIzleModeliOlusturAsync(
    int kursId,
    int kullaniciId,
    int soruSayfa,
    int yorumSayfa)
    {
        const int soruSayfaBoyutu = 10;
        const int yorumSayfaBoyutu = 5;

        soruSayfa = Math.Max(1, soruSayfa);
        yorumSayfa = Math.Max(1, yorumSayfa);

        var kurs = await _context.Kurslar
            .AsNoTracking()
            .Include(x => x.Durum)
            .Include(x => x.KursKategorileri)
                .ThenInclude(x => x.Kategori)
            .Include(x => x.Dersler)
            .Include(x => x.Bolumler.OrderBy(b => b.SiraNo))
                .ThenInclude(b => b.Dersler
                    .Where(d => d.AktifMi && !d.SistemDersiMi)
                    .OrderBy(d => d.SiraNo))
                    .ThenInclude(d => d.DersMateryalleri)
            .Include(x => x.Sinav)
            .FirstOrDefaultAsync(x => x.KursId == kursId && x.EgitmenId == kullaniciId);

        if (kurs == null)
        {
            return null;
        }

        var eksikler = await YayinEksikleriniGetirAsync(kurs, durumKontroluYap: false);
        bool onIzleYonetimModuMu = KursYayinaGonderilebilirDurumdaMi(kurs.DurumId);

        var model = new KursOnIzleViewModel
        {
            KursId = kurs.KursId,
            KursAdi = kurs.KursAdi,
            Aciklama = kurs.Aciklama,
            KapakGorselUrl = kurs.KapakGorselUrl,
            DurumId = kurs.DurumId,
            DurumAdi = kurs.Durum.DurumAdi,

            Kategoriler = kurs.KursKategorileri
                .Select(x => x.Kategori.KategoriAdi)
                .OrderBy(x => x)
                .ToList(),

            Bolumler = kurs.Bolumler
                .OrderBy(x => x.SiraNo)
                .Select(x => new KursOnIzleBolumViewModel
                {
                    BolumId = x.BolumId,
                    BolumAdi = x.BolumAdi,
                    SiraNo = x.SiraNo,

                    Dersler = x.Dersler
                        .OrderBy(d => d.SiraNo)
                        .Select(d => new KursOnIzleDersViewModel
                        {
                            DersId = d.DersId,
                            DersAdi = d.DersAdi,
                            Aciklama = d.Aciklama,
                            VideoUrl = d.VideoUrl,
                            SiraNo = d.SiraNo,

                            Materyaller = d.DersMateryalleri
                                .Select(m => new KursOnIzleMateryalViewModel
                                {
                                    Baslik = m.Baslik,
                                    MateryalUrl = m.MateryalUrl
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList(),

            Eksikler = eksikler,
            KursDuzenlenebilirMi = onIzleYonetimModuMu,
            KursYayinaGonderilebilirMi = KursYayinaGonderilebilirDurumdaMi(kurs.DurumId) && !eksikler.Any()
        };

        var degerlendirmeQuery = _context.KursDegerlendirmeleri
            .AsNoTracking()
            .Where(x => x.KursId == kurs.KursId);

        int yorumToplamKayit = await degerlendirmeQuery.CountAsync();

        int yorumToplamSayfa = yorumToplamKayit == 0
            ? 1
            : (int)Math.Ceiling(yorumToplamKayit / (double)yorumSayfaBoyutu);

        if (yorumSayfa > yorumToplamSayfa)
        {
            yorumSayfa = yorumToplamSayfa;
        }

        double ortalamaPuan = await degerlendirmeQuery
            .AverageAsync(x => (double?)x.Puan) ?? 0;

        model.OrtalamaPuan = Math.Round(ortalamaPuan, 1);
        model.DegerlendirmeSayisi = yorumToplamKayit;
        model.YorumSayfa = yorumSayfa;
        model.YorumToplamSayfa = yorumToplamSayfa;
        model.YorumToplamKayit = yorumToplamKayit;
        model.YorumSayfaBoyutu = yorumSayfaBoyutu;

        model.OgrenciSayisi = await _context.KursKayitlari
            .AsNoTracking()
            .CountAsync(x => x.KursId == kurs.KursId && x.AktifMi);

        model.Degerlendirmeler = await degerlendirmeQuery
            .OrderByDescending(x => x.DegerlendirmeTarihi)
            .Skip((yorumSayfa - 1) * yorumSayfaBoyutu)
            .Take(yorumSayfaBoyutu)
            .Select(x => new KursOnIzleDegerlendirmeViewModel
            {
                DegerlendirmeId = x.DegerlendirmeId,
                OgrenciAdSoyad = x.Kullanici.Ad + " " + x.Kullanici.Soyad,
                Puan = x.Puan,
                YorumMetni = x.YorumMetni,
                DegerlendirmeTarihi = x.DegerlendirmeTarihi
            })
            .ToListAsync();

        if (kurs.Sinav != null)
        {
            int havuzdakiSoruSayisi = await _context.Sorular
                .AsNoTracking()
                .CountAsync(x => x.SinavId == kurs.Sinav.SinavId && x.AktifMi);

            int gecerliSoruSayisi = await _context.Sorular
                .AsNoTracking()
                .CountAsync(x =>
                    x.SinavId == kurs.Sinav.SinavId &&
                    x.AktifMi &&
                    x.SoruSecenekleri.Count(s => s.AktifMi) >= 2 &&
                    x.SoruSecenekleri.Count(s => s.AktifMi && s.DogruMu) == 1 &&
                    x.SoruDersleri.Any(sd =>
                        sd.Ders.KursId == kurs.KursId &&
                        sd.Ders.AktifMi &&
                        !sd.Ders.SistemDersiMi) &&
                    !x.SoruDersleri.Any(sd =>
                        sd.Ders.KursId != kurs.KursId ||
                        !sd.Ders.AktifMi ||
                        sd.Ders.SistemDersiMi));

            int toplamSoruSayfa = havuzdakiSoruSayisi == 0
                ? 1
                : (int)Math.Ceiling(havuzdakiSoruSayisi / (double)soruSayfaBoyutu);

            if (soruSayfa > toplamSoruSayfa)
            {
                soruSayfa = toplamSoruSayfa;
            }

            var soruIdleri = await _context.Sorular
                .AsNoTracking()
                .Where(x => x.SinavId == kurs.Sinav.SinavId && x.AktifMi)
                .OrderByDescending(x => x.SoruId)
                .Skip((soruSayfa - 1) * soruSayfaBoyutu)
                .Take(soruSayfaBoyutu)
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
                    .Where(x => soruIdleri.Contains(x.SoruId))
                    .ToListAsync()
                : new List<Soru>();

            model.Sinav = new KursOnIzleSinavViewModel
            {
                SinavId = kurs.Sinav.SinavId,
                SinavAdi = kurs.Sinav.SinavAdi,
                Aciklama = kurs.Sinav.Aciklama,
                GecmeNotu = kurs.Sinav.GecmeNotu,
                SureDakika = kurs.Sinav.SureDakika,
                SoruSayisi = kurs.Sinav.SoruSayisi,
                HavuzdakiSoruSayisi = havuzdakiSoruSayisi,
                GecerliSoruSayisi = gecerliSoruSayisi,
                SoruSayfa = soruSayfa,
                SoruSayfaBoyutu = soruSayfaBoyutu,

                Sorular = sorular
                    .OrderBy(x => soruSiralari[x.SoruId])
                    .Select(x => new KursOnIzleSoruViewModel
                    {
                        SoruId = x.SoruId,
                        SoruMetni = x.SoruMetni,
                        GecerliMi = SoruYayinaHazirMi(x, kurs.KursId),

                        DersAdlari = x.SoruDersleri
                            .Where(sd =>
                                sd.Ders.KursId == kurs.KursId &&
                                sd.Ders.AktifMi &&
                                !sd.Ders.SistemDersiMi)
                            .OrderBy(sd => sd.Ders.Bolum.SiraNo)
                            .ThenBy(sd => sd.Ders.SiraNo)
                            .Select(sd => $"{sd.Ders.Bolum.BolumAdi} / {sd.Ders.DersAdi}")
                            .ToList(),

                        Secenekler = x.SoruSecenekleri
                            .Where(s => s.AktifMi)
                            .Select(s => new KursOnIzleSecenekViewModel
                            {
                                SecenekMetni = s.SecenekMetni,
                                DogruMu = s.DogruMu
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }

        return model;
    }

    private static bool KursYayinaGonderilebilirDurumdaMi(int durumId)
    {
        return durumId == 3 || durumId == 6 || durumId == 7;
    }

    private async Task<List<string>> YayinEksikleriniGetirAsync(Kurs kurs, bool durumKontroluYap = true)
    {
        var eksikler = new List<string>();

        if (durumKontroluYap)
        {
            if (kurs.DurumId == 4)
            {
                eksikler.Add("Bu kurs zaten onay bekliyor.");
            }
            else if (!KursYayinaGonderilebilirDurumdaMi(kurs.DurumId))
            {
                eksikler.Add("Bu kurs şu an yayına gönderilebilir durumda değil.");
            }
        }

        if (!kurs.Bolumler.Any())
        {
            eksikler.Add("En az bir bölüm eklemelisiniz.");
        }

        if (!kurs.Dersler.Any(x => x.AktifMi && !x.SistemDersiMi))
        {
            eksikler.Add("En az bir aktif ders eklemelisiniz.");
        }

        if (kurs.Sinav == null)
        {
            eksikler.Add("Sınav ayarlarını oluşturmalısınız.");
            return eksikler;
        }

        if (kurs.Sinav.SoruSayisi <= 0)
        {
            eksikler.Add("Sınavda çıkacak soru sayısı 0'dan büyük olmalıdır.");
        }

        int havuzdakiSoruSayisi = await _context.Sorular
            .AsNoTracking()
            .CountAsync(x => x.SinavId == kurs.Sinav.SinavId && x.AktifMi);

        if (havuzdakiSoruSayisi == 0)
        {
            eksikler.Add("Soru havuzuna en az bir soru eklemelisiniz.");
            return eksikler;
        }

        int gecerliSoruSayisi = await _context.Sorular
            .AsNoTracking()
            .CountAsync(x =>
                x.SinavId == kurs.Sinav.SinavId &&
                x.AktifMi &&
                x.SoruSecenekleri.Count(s => s.AktifMi) >= 2 &&
                x.SoruSecenekleri.Count(s => s.AktifMi && s.DogruMu) == 1 &&
                x.SoruDersleri.Any(sd =>
                    sd.Ders.KursId == kurs.KursId &&
                    sd.Ders.AktifMi &&
                    !sd.Ders.SistemDersiMi) &&
                !x.SoruDersleri.Any(sd =>
                    sd.Ders.KursId != kurs.KursId ||
                    !sd.Ders.AktifMi ||
                    sd.Ders.SistemDersiMi));

        if (gecerliSoruSayisi < kurs.Sinav.SoruSayisi)
        {
            eksikler.Add("Sınavda çıkacak soru sayısı için yeterli geçerli soru yok.");
        }

        return eksikler;
    }

    private static bool SoruYayinaHazirMi(Soru soru, int kursId)
    {
        return soru.AktifMi &&
               soru.SoruSecenekleri.Count(x => x.AktifMi) >= 2 &&
               soru.SoruSecenekleri.Count(x => x.AktifMi && x.DogruMu) == 1 &&
               SoruDersBaglantilariGecerliMi(soru, kursId);
    }

    private static bool SoruDersBaglantilariGecerliMi(Soru soru, int kursId)
    {
        return soru.SoruDersleri.Any(x =>
                   x.Ders.KursId == kursId &&
                   x.Ders.AktifMi &&
                   !x.Ders.SistemDersiMi) &&
               !soru.SoruDersleri.Any(x =>
                   x.Ders.KursId != kursId ||
                   !x.Ders.AktifMi ||
                   x.Ders.SistemDersiMi);
    }

    private async Task<bool> KursGecmisVerisiVarMiAsync(int kursId)
    {
        return await _context.KursKayitlari.AnyAsync(x => x.KursId == kursId) ||
               await _context.DersIlerlemeleri.AnyAsync(x => x.Ders.KursId == kursId) ||
               await _context.SinavKatilimlari.AnyAsync(x => x.Sinav.KursId == kursId) ||
               await _context.OgrenciCevaplari.AnyAsync(x => x.Soru.Sinav.KursId == kursId) ||
               await _context.KursDegerlendirmeleri.AnyAsync(x => x.KursId == kursId) ||
               await _context.Sertifikalar.AnyAsync(x => x.KursId == kursId);
    }

    private async Task KursuFizikselSilAsync(int kursId, List<string> silinecekFizikselDosyalar)
    {
        var kurs = await _context.Kurslar
            .Include(x => x.KursKategorileri)
            .FirstAsync(x => x.KursId == kursId);

        DosyaYoluEkleEgerUploadIse(
            silinecekFizikselDosyalar,
            kurs.KapakGorselUrl,
            "/uploads/kurs-kapaklari/"
        );

        var dersler = await _context.Dersler
            .Include(x => x.DersMateryalleri)
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        foreach (var ders in dersler)
        {
            DosyaYoluEkleEgerUploadIse(
                silinecekFizikselDosyalar,
                ders.VideoUrl,
                "/uploads/ders-videolari/"
            );

            foreach (var materyal in ders.DersMateryalleri)
            {
                DosyaYoluEkleEgerUploadIse(
                    silinecekFizikselDosyalar,
                    materyal.MateryalUrl,
                    "/uploads/ders-materyalleri/"
                );
            }
        }

        var sinav = await _context.Sinavlar
            .Include(x => x.Sorular)
                .ThenInclude(x => x.SoruDersleri)
            .Include(x => x.Sorular)
                .ThenInclude(x => x.SoruSecenekleri)
            .FirstOrDefaultAsync(x => x.KursId == kursId);

        if (sinav != null)
        {
            foreach (var soru in sinav.Sorular)
            {
                _context.SoruDersleri.RemoveRange(soru.SoruDersleri);
                _context.SoruSecenekleri.RemoveRange(soru.SoruSecenekleri);
            }

            _context.Sorular.RemoveRange(sinav.Sorular);
            _context.Sinavlar.Remove(sinav);
        }

        _context.DersMateryalleri.RemoveRange(dersler.SelectMany(x => x.DersMateryalleri));
        _context.Dersler.RemoveRange(dersler);

        var bolumler = await _context.Bolumler
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        _context.Bolumler.RemoveRange(bolumler);
        _context.KursKategorileri.RemoveRange(kurs.KursKategorileri);

        var favoriler = await _context.Favoriler
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        _context.Favoriler.RemoveRange(favoriler);

        var kursOnaylari = await _context.KursOnaylari
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        _context.KursOnaylari.RemoveRange(kursOnaylari);

        var oneriler = await _context.Oneriler
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        foreach (var oneri in oneriler)
        {
            oneri.KursId = null;
        }

        _context.Kurslar.Remove(kurs);

        await _context.SaveChangesAsync();
    }

    private void KapakGorselDosyasiniDogrula(IFormFile dosya)
    {
        string uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();

        if (!IzinliKapakGorselUzantilari.Contains(uzanti))
        {
            ModelState.AddModelError(
                "KapakGorselDosya",
                "Sadece JPG, PNG veya WEBP formatında görsel yükleyebilirsiniz."
            );
        }

        if (dosya.Length > MaksKapakGorselBoyutu)
        {
            ModelState.AddModelError(
                "KapakGorselDosya",
                "Kapak görseli en fazla 5 MB olabilir."
            );
        }
    }

    private void DosyaYoluEkleEgerUploadIse(List<string> dosyaListesi, string? url, string beklenenPrefix)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith(beklenenPrefix))
        {
            return;
        }

        string webRoot = Path.GetFullPath(_webHostEnvironment.WebRootPath);
        string fizikselYol = Path.GetFullPath(Path.Combine(
            webRoot,
            url.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
        ));

        if (!fizikselYol.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        dosyaListesi.Add(fizikselYol);
    }

    private static void YuklenenDosyalariSil(List<string> dosyaYollari)
    {
        foreach (var dosyaYolu in dosyaYollari.Distinct())
        {
            try
            {
                if (System.IO.File.Exists(dosyaYolu))
                {
                    System.IO.File.Delete(dosyaYolu);
                }
            }
            catch
            {
                // Dosya kilitliyse kullanıcı akışını bozmamak için sessiz geçilir.
            }
        }
    }

    // =========================
    // ŞİMDİLİK PLACEHOLDER
    // =========================

    [HttpGet]
    public IActionResult KursDetay(int id)
    {
        return RedirectToAction(nameof(Kurslarim));
    }


}
