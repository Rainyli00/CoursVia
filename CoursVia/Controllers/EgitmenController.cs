using CoursVia.ViewModels.Egitmen.Ogrencilerim;
using CoursVia.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers;

// Eğitmen panelindeki ana sayfa ve öğrencilerim ekranını yönetir.
[Authorize(Roles = "Eğitmen")]
public class EgitmenController : Controller
{
    private readonly AppDbContext _context;

    public EgitmenController(AppDbContext context)
    {
        _context = context;
    }

    // Eğitmen dashboard ekranını hazırlar.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var egitmenProfili = await _context.EgitmenProfilleri
            .Include(x => x.Kullanici)
            .Include(x => x.EgitmenBranslari)
                .ThenInclude(x => x.Kategori)
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        // Eğitmen profili yoksa veya onaylı değilse erişim engellenir.
        if (egitmenProfili == null || egitmenProfili.DurumId != 8)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Eğitmenin yayındaki kurs sayısı hesaplanır.
        ViewBag.YayindakiKurs = await _context.Kurslar
            .CountAsync(x => x.EgitmenId == kullaniciId && x.DurumId == 5);

        // Eğitmenin admin onayı bekleyen kurs sayısı hesaplanır.
        ViewBag.OnayBekleyenKurs = await _context.Kurslar
            .CountAsync(x => x.EgitmenId == kullaniciId && x.DurumId == 4);

        // Eğitmenin taslak durumundaki kurs sayısı hesaplanır.
        ViewBag.TaslakKurs = await _context.Kurslar
            .CountAsync(x => x.EgitmenId == kullaniciId && x.DurumId == 3);

        // Dashboard üzerinde gösterilecek son eklenen kurslar alınır.
        ViewBag.SonKurslar = await _context.Kurslar
            .Include(x => x.Durum)
            .Include(x => x.KursKategorileri)
                .ThenInclude(x => x.Kategori)
            .Where(x => x.EgitmenId == kullaniciId)
            .OrderByDescending(x => x.OlusturmaTarihi)
            .Take(5)
            .ToListAsync();

        // Eğitmenin kurslarına yapılan son öğrenci kayıtları alınır.
        ViewBag.SonKayitlar = await _context.KursKayitlari
            .Include(x => x.Kullanici)
            .Include(x => x.Kurs)
            .Where(x => x.Kurs.EgitmenId == kullaniciId && x.AktifMi)
            .OrderByDescending(x => x.KayitTarihi)
            .Take(5)
            .ToListAsync();

        return View(egitmenProfili);
    }

    // Eğitmenin kurslarına kayıtlı öğrencileri arama, kurs filtresi ve sayfalama ile listeler.
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

        // Filtreleme alanında kullanılacak eğitmene ait kurslar alınır.
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

        // Kurs seçildiyse sadece o kursa kayıtlı öğrenciler listelenir.
        if (kursId.HasValue)
        {
            query = query.Where(x => x.KursId == kursId.Value);
        }

        // Öğrenci adı, soyadı veya ad-soyad birleşimine göre arama yapılır.
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

        // Tüm filtrelenmiş öğrenciler için özet bilgiler alınır.
        // Bu veriler ortalama ilerleme ve sınav istatistiklerini hesaplamak için kullanılır.
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

        // Filtrelenmiş tüm öğrencilerin sınav katılım bilgileri tek sorguda alınır.
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

        // Her kurs kaydı için en son sınav denemesi bulunur.
        var tumSonSinavlar = tumSinavKatilimlari
            .GroupBy(x => x.KursKayitId)
            .ToDictionary(
                x => x.Key,
                x => x.First()
            );

        // Her öğrencinin sınava kaç kez girdiği hesaplanır.
        var tumSinavGirisSayilari = tumSinavKatilimlari
            .GroupBy(x => x.KursKayitId)
            .ToDictionary(
                x => x.Key,
                x => x.Count()
            );

        // Tüm öğrenciler için ilerleme yüzdesi ve genel sınav bilgileri hesaplanır.
        foreach (var ogrenci in tumOgrenciOzetleri)
        {

            // İlerleme yüzdesi, tamamlanan ders sayısının toplam ders sayısına oranı olarak hesaplanır.
            ogrenci.IlerlemeYuzdesi = ogrenci.ToplamDersSayisi == 0
                ? 0
                : (int)Math.Round((ogrenci.TamamlananDersSayisi * 100.0) / ogrenci.ToplamDersSayisi);
            // Sınava giriş sayısı, öğrencinin kurs kaydına ait sınav katılım sayısı olarak alınır.
            ogrenci.SinavGirisSayisi = tumSinavGirisSayilari.TryGetValue(ogrenci.KursKayitId, out int girisSayisi)
                ? girisSayisi
                : 0;

            if (tumSonSinavlar.TryGetValue(ogrenci.KursKayitId, out var sonSinav))
            {
                ogrenci.SinavdanGectiMi = sonSinav.GectiMi;
            }
        }

        // Sayfada gösterilecek öğrenciler alınır.
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

        // Sadece mevcut sayfada gösterilen öğrencilerin sınav bilgileri ayrılır.
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

        // Sayfada gösterilen her öğrenci için ilerleme ve son sınav durumu hesaplanır.
        foreach (var ogrenci in ogrenciler)
        {
            // İlerleme yüzdesi, tamamlanan ders sayısının toplam ders sayısına oranı olarak hesaplanır.
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

        // Liste verileri, filtreler ve özet istatistikler view tarafına gönderilir.
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