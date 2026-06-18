using CoursVia.Data;
using CoursVia.Models;
using CoursVia.ViewModels.Ogrenci;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CoursVia.Services;

namespace CoursVia.Controllers;

// Öğrencinin sınav listeleme, sınava girme, cevap kaydetme,
// sınavı bitirme ve sınav sonucunu görüntüleme işlemlerini yönetir.
[Authorize(Roles = "Öğrenci")]
public class OgrenciSinavController : Controller
{
    private const int MaksimumSinavHakki = 3;

    private readonly AppDbContext _context;
    private readonly BildirimService _bildirimService;

    public OgrenciSinavController(AppDbContext context, BildirimService bildirimService)
    {
        _context = context;
        _bildirimService = bildirimService;
    }

    // Öğrencinin girebileceği, devam eden, başarılı, başarısız veya kilitli sınavlarını listeler.
    [HttpGet]
    public async Task<IActionResult> Index(string? arama, string durum = "tum", int sayfa = 1)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        const int sayfaBasinaKayit = 5;

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        durum = string.IsNullOrWhiteSpace(durum)
            ? "tum"
            : durum.Trim().ToLower();

        // Geçersiz durum filtresi gelirse tüm sınavlar gösterilir.
        if (durum != "tum" &&
            durum != "girilebilir" &&
            durum != "devam" &&
            durum != "basarili" &&
            durum != "basarisiz" &&
            durum != "kilitli")
        {
            durum = "tum";
        }

        if (sayfa < 1)
        {
            sayfa = 1;
        }

        // Öğrenci sayfaya girdiğinde süresi dolmuş ama kapanmamış sınavlar varsa otomatik değerlendirilir.
        int suresiDolanSinavSayisi = await SuresiDolanDevamEdenSinavlariKapatAsync(kullaniciId);

        if (suresiDolanSinavSayisi > 0)
        {
            TempData["OgrenciHata"] = "Süresi dolan sınavlarınız mevcut cevaplarınıza göre otomatik değerlendirilmiştir.";
        }

        // Öğrencinin aktif kayıtlı olduğu ve sınavı bulunan kurslar alınır.
        var kayitliSinavlar = await _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                x.AktifMi &&
                x.Kurs.Sinav != null &&
                x.Kurs.Dersler.Any(d =>
                    d.AktifMi &&
                    !d.SistemDersiMi))
            .OrderByDescending(x => x.KayitTarihi)
            .Select(x => new
            {
                x.KursKayitId,
                x.KursId,
                x.TamamlandiMi,

                KursAdi = x.Kurs.KursAdi,
                KapakGorselUrl = x.Kurs.KapakGorselUrl,
                KursDurumId = x.Kurs.DurumId,
                KursDurumAdi = x.Kurs.Durum.DurumAdi,

                SinavId = x.Kurs.Sinav!.SinavId,
                SinavAdi = x.Kurs.Sinav.SinavAdi,
                GecmeNotu = x.Kurs.Sinav.GecmeNotu,
                SureDakika = x.Kurs.Sinav.SureDakika,
                SoruSayisi = x.Kurs.Sinav.SoruSayisi,

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

        var kursKayitIdleri = kayitliSinavlar
            .Select(x => x.KursKayitId)
            .ToList();

        // Öğrencinin bu kurs kayıtları üzerinden daha önce girdiği sınav denemeleri alınır.
        var sinavKatilimlari = await _context.SinavKatilimlari
            .AsNoTracking()
            .Where(x => kursKayitIdleri.Contains(x.KursKayitId))
            .OrderByDescending(x => x.BaslamaTarihi)
            .Select(x => new
            {
                x.SinavKatilimId,
                x.KursKayitId,
                x.SinavId,
                x.BaslamaTarihi,
                x.BitisTarihi,
                x.AlinanPuan,
                x.GectiMi
            })
            .ToListAsync();

        var tumSinavlar = new List<OgrenciSinavListeItemViewModel>();

        // Her kurs için sınav durumu, giriş hakkı, başarı durumu ve girilebilirlik hesaplanır.
        foreach (var item in kayitliSinavlar)
        {
            var denemeler = sinavKatilimlari
                .Where(x =>
                    x.KursKayitId == item.KursKayitId &&
                    x.SinavId == item.SinavId)
                .OrderByDescending(x => x.BaslamaTarihi)
                .ToList();

            var devamEdenDeneme = denemeler
                .FirstOrDefault(x => x.BitisTarihi == null);

            var bitmisDenemeler = denemeler
                .Where(x => x.BitisTarihi != null)
                .ToList();

            var sonBitmisDeneme = bitmisDenemeler
                .OrderByDescending(x => x.BaslamaTarihi)
                .FirstOrDefault();

            bool sinavGecildiMi = bitmisDenemeler
                .Any(x => x.GectiMi == true);

            int girisSayisi = bitmisDenemeler.Count;

            int kalanHak = Math.Max(0, MaksimumSinavHakki - girisSayisi);

            bool derslerTamamlandiMi =
                item.ToplamDersSayisi > 0 &&
                item.TamamlananDersSayisi == item.ToplamDersSayisi;

            bool kursGuncelleniyorMu = item.KursDurumId == 7;

            // Sınava girebilmek için kurs güncellenmiyor olmalı,
            // tüm dersler tamamlanmalı, kurs daha önce başarıyla bitmemiş olmalı
            // ve sınav hakkı bulunmalı veya devam eden sınav olmalıdır.
            bool sinavaGirebilirMi =
                !kursGuncelleniyorMu &&
                derslerTamamlandiMi &&
                !item.TamamlandiMi &&
                !sinavGecildiMi &&
                (devamEdenDeneme != null || kalanHak > 0);

            string durumMetni;

            if (sinavGecildiMi || item.TamamlandiMi)
            {
                durumMetni = "Başarılı";
            }
            else if (kursGuncelleniyorMu)
            {
                durumMetni = "Kurs güncelleniyor";
            }
            else if (devamEdenDeneme != null)
            {
                durumMetni = "Devam ediyor";
            }
            else if (!derslerTamamlandiMi)
            {
                durumMetni = "Dersleri tamamla";
            }
            else if (kalanHak <= 0)
            {
                durumMetni = "Hak doldu";
            }
            else if (girisSayisi == 0)
            {
                durumMetni = "Sınava hazır";
            }
            else
            {
                durumMetni = "Tekrar girilebilir";
            }

            tumSinavlar.Add(new OgrenciSinavListeItemViewModel
            {
                KursId = item.KursId,
                KursKayitId = item.KursKayitId,
                SinavId = item.SinavId,

                DevamEdenSinavKatilimId = devamEdenDeneme?.SinavKatilimId,
                SonSinavKatilimId = sonBitmisDeneme?.SinavKatilimId,

                KursAdi = item.KursAdi,
                KapakGorselUrl = item.KapakGorselUrl,
                KursDurumId = item.KursDurumId,
                KursDurumAdi = item.KursDurumAdi,

                SinavAdi = item.SinavAdi,
                GecmeNotu = item.GecmeNotu,
                SureDakika = item.SureDakika,
                SoruSayisi = item.SoruSayisi,

                ToplamDersSayisi = item.ToplamDersSayisi,
                TamamlananDersSayisi = item.TamamlananDersSayisi,
                DerslerTamamlandiMi = derslerTamamlandiMi,

                GirisSayisi = girisSayisi,
                KalanHak = kalanHak,

                SonPuan = sonBitmisDeneme?.AlinanPuan,
                SonucGectiMi = sonBitmisDeneme?.GectiMi,

                KursTamamlandiMi = item.TamamlandiMi,
                SinavaGirebilirMi = sinavaGirebilirMi,
                DurumMetni = durumMetni
            });
        }

        int toplamSinavSayisi = tumSinavlar.Count;

        int girilebilirSinavSayisi = tumSinavlar.Count(x =>
            x.SinavaGirebilirMi &&
            !x.DevamEdenSinavKatilimId.HasValue);

        int gecilenSinavSayisi = tumSinavlar.Count(x =>
            x.KursTamamlandiMi ||
            x.SonucGectiMi == true);

        int devamEdenSinavSayisi = tumSinavlar.Count(x =>
            x.DevamEdenSinavKatilimId.HasValue &&
            !x.GuncelleniyorMu);

        IEnumerable<OgrenciSinavListeItemViewModel> filtreliSinavlar = tumSinavlar;

        // Arama filtresi kurs adı veya sınav adına göre uygulanır.
        if (!string.IsNullOrWhiteSpace(arama))
        {
            filtreliSinavlar = filtreliSinavlar.Where(x =>
                x.KursAdi.Contains(arama, StringComparison.OrdinalIgnoreCase) ||
                x.SinavAdi.Contains(arama, StringComparison.OrdinalIgnoreCase));
        }

        // Seçilen durum filtresine göre sınav listesi daraltılır.
        filtreliSinavlar = durum switch
        {
            "girilebilir" => filtreliSinavlar.Where(x =>
                x.SinavaGirebilirMi &&
                !x.DevamEdenSinavKatilimId.HasValue),

            "devam" => filtreliSinavlar.Where(x =>
                x.DevamEdenSinavKatilimId.HasValue &&
                !x.GuncelleniyorMu),

            "basarili" => filtreliSinavlar.Where(x =>
                x.KursTamamlandiMi ||
                x.SonucGectiMi == true),

            "basarisiz" => filtreliSinavlar.Where(x =>
                x.SonucGectiMi == false),

            "kilitli" => filtreliSinavlar.Where(x =>
                !x.SinavaGirebilirMi &&
                !x.DevamEdenSinavKatilimId.HasValue &&
                x.SonucGectiMi != true &&
                !x.KursTamamlandiMi),

            _ => filtreliSinavlar
        };

        int toplamKayit = filtreliSinavlar.Count();

        int toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);

        if (toplamSayfa < 1)
        {
            toplamSayfa = 1;
        }

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        var sayfalanmisSinavlar = filtreliSinavlar
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .ToList();

        var model = new OgrenciSinavlarimViewModel
        {
            Sinavlar = sayfalanmisSinavlar,

            ToplamSinavSayisi = toplamSinavSayisi,
            GirilebilirSinavSayisi = girilebilirSinavSayisi,
            GecilenSinavSayisi = gecilenSinavSayisi,
            DevamEdenSinavSayisi = devamEdenSinavSayisi,

            Arama = arama,
            Durum = durum,

            Sayfa = sayfa,
            ToplamSayfa = toplamSayfa,
            ToplamKayit = toplamKayit,
            SayfaBasinaKayit = sayfaBasinaKayit
        };

        return View(model);
    }

    // Öğrencinin sınavı başlatmasını sağlar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Baslat(int kursId)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kursKaydi = await _context.KursKayitlari
            .Include(x => x.Kurs)
                .ThenInclude(x => x.Sinav)
            .FirstOrDefaultAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == kursId &&
                x.AktifMi);

        if (kursKaydi == null)
        {
            TempData["OgrenciHata"] = "Bu kursa kayıtlı değilsiniz.";
            return RedirectToAction("Kurslarim", "OgrenciKurs");
        }

        if (kursKaydi.Kurs.DurumId == 7)
        {
            TempData["OgrenciHata"] = "Bu kurs şu anda güncelleniyor.";
            return RedirectToAction("Kurslarim", "OgrenciKurs");
        }

        if (kursKaydi.TamamlandiMi)
        {
            TempData["OgrenciHata"] = "Bu kursu zaten başarıyla tamamladınız.";
            return RedirectToAction(nameof(Index));
        }

        if (kursKaydi.Kurs.Sinav == null)
        {
            TempData["OgrenciHata"] = "Bu kurs için sınav bulunmuyor.";
            return RedirectToAction(nameof(Index));
        }

        var sinav = kursKaydi.Kurs.Sinav;

        if (sinav.SoruSayisi <= 0)
        {
            TempData["OgrenciHata"] = "Bu sınav için geçerli soru sayısı tanımlanmamış.";
            return RedirectToAction(nameof(Index));
        }

        if (sinav.SureDakika <= 0)
        {
            TempData["OgrenciHata"] = "Bu sınav için geçerli süre tanımlanmamış.";
            return RedirectToAction(nameof(Index));
        }

        // Öğrencinin sınava girebilmesi için tüm aktif normal dersleri tamamlamış olması gerekir.
        int toplamDersSayisi = await _context.Dersler
            .AsNoTracking()
            .CountAsync(x =>
                x.KursId == kursId &&
                x.AktifMi &&
                !x.SistemDersiMi);

        int tamamlananDersSayisi = await _context.DersIlerlemeleri
            .AsNoTracking()
            .CountAsync(x =>
                x.KursKayitId == kursKaydi.KursKayitId &&
                x.TamamlandiMi &&
                x.Ders.AktifMi &&
                !x.Ders.SistemDersiMi);

        bool derslerTamamlandiMi =
            toplamDersSayisi > 0 &&
            tamamlananDersSayisi == toplamDersSayisi;

        if (!derslerTamamlandiMi)
        {
            TempData["OgrenciHata"] = "Sınava girebilmek için kurs derslerini tamamlamalısınız.";
            return RedirectToAction(nameof(Index));
        }

        // Öğrencinin daha önceki sınav denemeleri alınır.
        var denemeler = await _context.SinavKatilimlari
            .AsNoTracking()
            .Where(x =>
                x.KursKayitId == kursKaydi.KursKayitId &&
                x.SinavId == sinav.SinavId)
            .OrderByDescending(x => x.BaslamaTarihi)
            .ToListAsync();

        var devamEdenDeneme = denemeler
            .FirstOrDefault(x => x.BitisTarihi == null);

        // Devam eden sınav varsa yeni sınav açılmaz, mevcut sınava yönlendirilir.
        if (devamEdenDeneme != null)
        {
            return RedirectToAction(nameof(SinavGiris), new { id = devamEdenDeneme.SinavKatilimId });
        }

        var gecilenDeneme = denemeler
            .Where(x => x.BitisTarihi != null)
            .OrderByDescending(x => x.BaslamaTarihi)
            .FirstOrDefault(x => x.GectiMi == true);

        // Daha önce sınav geçildiyse sonuç ekranına yönlendirilir.
        if (gecilenDeneme != null)
        {
            return RedirectToAction(nameof(Sonuc), new { id = gecilenDeneme.SinavKatilimId });
        }

        int bitmisDenemeSayisi = denemeler.Count(x => x.BitisTarihi != null);

        // Maksimum sınav hakkı dolduysa kurs kaydı pasife alınır.
        if (bitmisDenemeSayisi >= MaksimumSinavHakki)
        {
            await BasarisizHakDolduysaKursKaydiniPasifeAlAsync(
                kursKaydi.KursKayitId,
                sinav.SinavId
            );

            TempData["OgrenciHata"] = "Bu sınav için 3 hakkınız da doldu. Kursa yeniden kayıt olarak tekrar başlayabilirsiniz.";
            return RedirectToAction(nameof(Index));
        }

        // Sınav için geçerli soru havuzundan öğrenciye gösterilecek sorular seçilir.
        var secilenSoruIdleri = await SinavSorulariniSecAsync(
            sinav.SinavId,
            kursKaydi.KursKayitId,
            sinav.SoruSayisi);

        if (secilenSoruIdleri.Count < sinav.SoruSayisi)
        {
            TempData["OgrenciHata"] = "Sınav başlatılamadı. Bu sınav için yeterli sayıda geçerli ve aktif soru bulunmuyor.";
            return RedirectToAction(nameof(Index));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Transaction içinde tekrar devam eden sınav kontrolü yapılır.
        // Böylece çift tıklama veya aynı anda iki istek gelmesi durumunda ikinci sınav açılması engellenir.
        var tekrarDevamEdenDeneme = await _context.SinavKatilimlari
            .FirstOrDefaultAsync(x =>
                x.KursKayitId == kursKaydi.KursKayitId &&
                x.SinavId == sinav.SinavId &&
                x.BitisTarihi == null);

        if (tekrarDevamEdenDeneme != null)
        {
            await transaction.RollbackAsync();
            return RedirectToAction(nameof(SinavGiris), new { id = tekrarDevamEdenDeneme.SinavKatilimId });
        }

        var sinavKatilimi = new SinavKatilimi
        {
            KursKayitId = kursKaydi.KursKayitId,
            SinavId = sinav.SinavId,
            BaslamaTarihi = DateTime.Now,
            BitisTarihi = null,
            AlinanPuan = null,
            GectiMi = null
        };

        _context.SinavKatilimlari.Add(sinavKatilimi);
        await _context.SaveChangesAsync();

        // Seçilen her soru için boş cevap kaydı oluşturulur.
        foreach (int soruId in secilenSoruIdleri)
        {
            _context.OgrenciCevaplari.Add(new OgrenciCevabi
            {
                SinavKatilimId = sinavKatilimi.SinavKatilimId,
                SoruId = soruId,
                SecenekId = null,
                DogruMu = false,
                VerilmeTarihi = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return RedirectToAction(nameof(SinavGiris), new { id = sinavKatilimi.SinavKatilimId });
    }

    // Öğrencinin devam eden sınav ekranını açar.
    [HttpGet]
    public async Task<IActionResult> SinavGiris(int id)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var sinavKatilimi = await _context.SinavKatilimlari
            .AsNoTracking()
            .Where(x =>
                x.SinavKatilimId == id &&
                x.KursKaydi.KullaniciId == kullaniciId &&
                x.KursKaydi.AktifMi)
            .Select(x => new
            {
                x.SinavKatilimId,
                x.KursKayitId,
                x.SinavId,
                x.BaslamaTarihi,
                x.BitisTarihi,

                KursId = x.KursKaydi.KursId,
                KursAdi = x.KursKaydi.Kurs.KursAdi,
                KursDurumId = x.KursKaydi.Kurs.DurumId,

                x.Sinav.SinavAdi,
                x.Sinav.Aciklama,
                x.Sinav.GecmeNotu,
                x.Sinav.SureDakika,
                x.Sinav.SoruSayisi
            })
            .FirstOrDefaultAsync();

        if (sinavKatilimi == null)
        {
            TempData["OgrenciHata"] = "Sınav oturumu bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        if (sinavKatilimi.KursDurumId == 7)
        {
            TempData["OgrenciHata"] = "Bu kurs şu anda güncelleniyor.";
            return RedirectToAction("Kurslarim", "OgrenciKurs");
        }

        // Sınav zaten bitmişse doğrudan sonuç ekranına gidilir.
        if (sinavKatilimi.BitisTarihi != null)
        {
            return RedirectToAction(nameof(Sonuc), new { id = sinavKatilimi.SinavKatilimId });
        }

        DateTime sinavBitisLimiti = sinavKatilimi.BaslamaTarihi
            .AddMinutes(sinavKatilimi.SureDakika);

        // Süre dolmuşsa sınav mevcut cevaplarla otomatik değerlendirilir.
        if (DateTime.Now > sinavBitisLimiti)
        {
            var takipliSinavKatilimi = await _context.SinavKatilimlari
                .Include(x => x.Sinav)
                .Include(x => x.KursKaydi)
                .FirstOrDefaultAsync(x =>
                    x.SinavKatilimId == id &&
                    x.KursKaydi.KullaniciId == kullaniciId &&
                    x.KursKaydi.AktifMi);

            if (takipliSinavKatilimi != null && takipliSinavKatilimi.BitisTarihi == null)
            {
                await SinavKatiliminiDegerlendirAsync(
                    takipliSinavKatilimi,
                    kullaniciId,
                    gelenCevapSozlugu: null);

                TempData["OgrenciHata"] = "Sınav süresi dolduğu için sınavınız mevcut cevaplarla değerlendirilmiştir.";
            }

            return RedirectToAction(nameof(Sonuc), new { id = sinavKatilimi.SinavKatilimId });
        }

        // Sınav başlatılırken oluşturulan cevap kayıtları alınır.
        var cevapKayitlari = await _context.OgrenciCevaplari
            .AsNoTracking()
            .Where(x => x.SinavKatilimId == id)
            .OrderBy(x => x.OgrenciCevapId)
            .Select(x => new
            {
                x.SoruId,
                x.SecenekId
            })
            .ToListAsync();

        if (!cevapKayitlari.Any())
        {
            TempData["OgrenciHata"] = "Bu sınav için soru kaydı bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var soruIdleri = cevapKayitlari
            .Select(x => x.SoruId)
            .ToList();

        // Sınavdaki sorular ve aktif seçenekleri alınır.
        var sorular = await _context.Sorular
            .AsNoTracking()
            .Where(x => soruIdleri.Contains(x.SoruId))
            .Select(x => new
            {
                x.SoruId,
                x.SoruMetni,

                Secenekler = x.SoruSecenekleri
                    .Where(s => s.AktifMi)
                    .OrderBy(s => s.SecenekId)
                    .Select(s => new OgrenciSinavSecenekViewModel
                    {
                        SecenekId = s.SecenekId,
                        SecenekMetni = s.SecenekMetni
                    })
                    .ToList()
            })
            .ToListAsync();

        var soruSozlugu = sorular.ToDictionary(x => x.SoruId);

        var model = new OgrenciSinavaGirisViewModel
        {
            SinavKatilimId = sinavKatilimi.SinavKatilimId,
            KursId = sinavKatilimi.KursId,
            KursKayitId = sinavKatilimi.KursKayitId,
            SinavId = sinavKatilimi.SinavId,

            KursAdi = sinavKatilimi.KursAdi,
            SinavAdi = sinavKatilimi.SinavAdi,
            Aciklama = sinavKatilimi.Aciklama,

            GecmeNotu = sinavKatilimi.GecmeNotu,
            SureDakika = sinavKatilimi.SureDakika,
            SoruSayisi = sinavKatilimi.SoruSayisi,
            BaslamaTarihi = sinavKatilimi.BaslamaTarihi
        };

        int siraNo = 1;

        // Sorular cevap kayıtlarının sırasına göre modele eklenir.
        foreach (var cevap in cevapKayitlari)
        {
            // Soru kaydı bulunamazsa o soru atlanır.
            if (!soruSozlugu.TryGetValue(cevap.SoruId, out var soru))
            {
                continue;
            }

            // Soru ve seçenekler modele eklenir.
            model.Sorular.Add(new OgrenciSinavSoruViewModel
            {
                SoruId = soru.SoruId,
                SiraNo = siraNo++,
                SoruMetni = soru.SoruMetni,
                SeciliSecenekId = cevap.SecenekId,
                Secenekler = soru.Secenekler
            });
        }

        return View(model);
    }

    // Sınav ekranında öğrencinin seçtiği cevabı AJAX ile kaydeder.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CevapKaydet(int sinavKatilimId, int soruId, int? secenekId)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var sinavKatilimi = await _context.SinavKatilimlari
            .Include(x => x.Sinav)
            .Include(x => x.KursKaydi)
                .ThenInclude(x => x.Kurs)
            .FirstOrDefaultAsync(x =>
                x.SinavKatilimId == sinavKatilimId &&
                x.KursKaydi.KullaniciId == kullaniciId &&
                x.KursKaydi.AktifMi);

        if (sinavKatilimi == null)
        {
            return BadRequest(new
            {
                basarili = false,
                mesaj = "Sınav oturumu bulunamadı."
            });
        }

        if (sinavKatilimi.KursKaydi.Kurs.DurumId == 7)
        {
            return StatusCode(423, new
            {
                basarili = false,
                kursGuncelleniyor = true,
                yonlendirUrl = Url.Action("Kurslarim", "OgrenciKurs"),
                mesaj = "Bu kurs şu anda güncelleniyor."
            });
        }

        if (sinavKatilimi.BitisTarihi != null)
        {
            return BadRequest(new
            {
                basarili = false,
                mesaj = "Bu sınav zaten tamamlanmış."
            });
        }

        DateTime sinavBitisLimiti = sinavKatilimi.BaslamaTarihi
            .AddMinutes(sinavKatilimi.Sinav.SureDakika);

        // Süre dolduysa cevap kaydedilmez, sınav mevcut cevaplarla değerlendirilir.
        if (DateTime.Now > sinavBitisLimiti)
        {
            await SinavKatiliminiDegerlendirAsync(
                sinavKatilimi,
                kullaniciId,
                gelenCevapSozlugu: null);

            return StatusCode(409, new
            {
                basarili = false,
                sureDoldu = true,
                mesaj = "Sınav süresi dolduğu için cevap kaydedilemedi."
            });
        }

        var cevap = await _context.OgrenciCevaplari
            .FirstOrDefaultAsync(x =>
                x.SinavKatilimId == sinavKatilimId &&
                x.SoruId == soruId);

        if (cevap == null)
        {
            return BadRequest(new
            {
                basarili = false,
                mesaj = "Cevap kaydı bulunamadı."
            });
        }

        // Öğrenci seçimi kaldırdıysa cevap boş olarak kaydedilir.
        if (!secenekId.HasValue)
        {
            cevap.SecenekId = null;
            cevap.DogruMu = false;
            cevap.VerilmeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new
            {
                basarili = true
            });
        }

        // Seçilen seçeneğin ilgili soruya ait aktif seçenek olup olmadığı kontrol edilir.
        var secenek = await _context.SoruSecenekleri
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SecenekId == secenekId.Value &&
                x.SoruId == soruId &&
                x.AktifMi);

        if (secenek == null)
        {
            return BadRequest(new
            {
                basarili = false,
                mesaj = "Geçersiz seçenek."
            });
        }

        cevap.SecenekId = secenek.SecenekId;
        cevap.DogruMu = secenek.DogruMu;
        cevap.VerilmeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();

        return Json(new
        {
            basarili = true
        });
    }

    // Öğrenci sınavı bitirdiğinde cevapları değerlendirir ve sonuç ekranına yönlendirir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bitir(OgrenciSinavBitirViewModel model)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var sinavKatilimi = await _context.SinavKatilimlari
            .Include(x => x.Sinav)
            .Include(x => x.KursKaydi)
                .ThenInclude(x => x.Kurs)
            .FirstOrDefaultAsync(x =>
                x.SinavKatilimId == model.SinavKatilimId &&
                x.KursKaydi.KullaniciId == kullaniciId &&
                x.KursKaydi.AktifMi);

        if (sinavKatilimi == null)
        {
            TempData["OgrenciHata"] = "Sınav oturumu bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        if (sinavKatilimi.KursKaydi.Kurs.DurumId == 7)
        {
            TempData["OgrenciHata"] = "Bu kurs şu anda güncelleniyor.";
            return RedirectToAction("Kurslarim", "OgrenciKurs");
        }

        if (sinavKatilimi.BitisTarihi != null)
        {
            return RedirectToAction(nameof(Sonuc), new { id = sinavKatilimi.SinavKatilimId });
        }

        var gelenCevaplar = model.Cevaplar ?? new List<OgrenciSinavCevapViewModel>();

        // Formdan gelen cevaplar soru Id değerine göre sözlüğe çevrilir.
        var gelenCevapSozlugu = gelenCevaplar
            .GroupBy(x => x.SoruId)
            .ToDictionary(x => x.Key, x => x.First().SecenekId);

        // Sınav bitiş limiti hesaplanır ve sürenin dolup dolmadığı kontrol edilir.
        DateTime sinavBitisLimiti = sinavKatilimi.BaslamaTarihi
            .AddMinutes(sinavKatilimi.Sinav.SureDakika);

        bool sureDoldu = DateTime.Now > sinavBitisLimiti;

        // Süre dolmadıysa gelen cevaplar değerlendirmeye dahil edilir.
        // Süre dolduysa sistemde kayıtlı mevcut cevaplar üzerinden değerlendirme yapılır.
        bool degerlendirildi = await SinavKatiliminiDegerlendirAsync(
            sinavKatilimi,
            kullaniciId,
            sureDoldu ? null : gelenCevapSozlugu);

        if (!degerlendirildi)
        {
            TempData["OgrenciHata"] = "Sınav cevap kayıtları bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        if (sureDoldu)
        {
            TempData["OgrenciHata"] = "Sınav süresi dolduğu için sınavınız mevcut cevaplarla değerlendirilmiştir.";
        }

        return RedirectToAction(nameof(Sonuc), new { id = sinavKatilimi.SinavKatilimId });
    }

    // Öğrencinin tamamlanan sınav sonucunu gösterir.
    [HttpGet]
    public async Task<IActionResult> Sonuc(int id)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var sonuc = await _context.SinavKatilimlari
            .AsNoTracking()
            .Where(x =>
                x.SinavKatilimId == id &&
                x.KursKaydi.KullaniciId == kullaniciId)
            .Select(x => new
            {
                x.SinavKatilimId,
                x.KursKayitId,
                x.SinavId,
                x.BitisTarihi,
                x.AlinanPuan,
                x.GectiMi,

                KursId = x.KursKaydi.KursId,
                KursAdi = x.KursKaydi.Kurs.KursAdi,
                KursTamamlandiMi = x.KursKaydi.TamamlandiMi,

                x.Sinav.SinavAdi,
                x.Sinav.GecmeNotu
            })
            .FirstOrDefaultAsync();

        if (sonuc == null)
        {
            TempData["OgrenciHata"] = "Sınav sonucu bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        // Sınav henüz bitmemişse sonuç yerine sınav giriş ekranına yönlendirilir.
        if (sonuc.BitisTarihi == null)
        {
            return RedirectToAction(nameof(SinavGiris), new { id = sonuc.SinavKatilimId });
        }

        int toplamSoruSayisi = await _context.OgrenciCevaplari
            .AsNoTracking()
            .CountAsync(x => x.SinavKatilimId == sonuc.SinavKatilimId);

        int dogruSayisi = await _context.OgrenciCevaplari
            .AsNoTracking()
            .CountAsync(x =>
                x.SinavKatilimId == sonuc.SinavKatilimId &&
                x.DogruMu);

        int girisSayisi = await _context.SinavKatilimlari
            .AsNoTracking()
            .CountAsync(x =>
                x.KursKayitId == sonuc.KursKayitId &&
                x.SinavId == sonuc.SinavId &&
                x.BitisTarihi != null);

        int kalanHak = Math.Max(0, MaksimumSinavHakki - girisSayisi);

        var sertifika = await _context.Sertifikalar
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == sonuc.KursId);

        var model = new OgrenciSinavSonucViewModel
        {
            SinavKatilimId = sonuc.SinavKatilimId,
            KursId = sonuc.KursId,
            KursKayitId = sonuc.KursKayitId,

            KursAdi = sonuc.KursAdi,
            SinavAdi = sonuc.SinavAdi,

            GecmeNotu = sonuc.GecmeNotu,
            AlinanPuan = sonuc.AlinanPuan ?? 0,
            GectiMi = sonuc.GectiMi == true,

            DogruSayisi = dogruSayisi,
            ToplamSoruSayisi = toplamSoruSayisi,

            GirisSayisi = girisSayisi,
            KalanHak = kalanHak,

            KursTamamlandiMi = sonuc.KursTamamlandiMi,

            SertifikaOlustuMu = sertifika != null,
            SertifikaKodu = sertifika?.SertifikaKodu
        };

        return View(model);
    }

    // Sınav başlatılırken öğrenciye gösterilecek soruları seçer.
    private async Task<List<int>> SinavSorulariniSecAsync(
        int sinavId,
        int kursKayitId,
        int soruSayisi)
    {
        var sinavBilgisi = await _context.Sinavlar
            .AsNoTracking()
            .Where(x => x.SinavId == sinavId)
            .Select(x => new
            {
                x.SinavId,
                x.KursId
            })
            .FirstOrDefaultAsync();

        if (sinavBilgisi == null)
        {
            return new List<int>();
        }

        // Sınava uygun sorular aktif, yeterli seçenekli, tek doğru cevaplı
        // ve kursun aktif normal derslerine bağlı olmalıdır.
        var gecerliSoruIdleri = await _context.Sorular
            .AsNoTracking()
            .Where(x =>
                x.SinavId == sinavId &&
                x.AktifMi &&
                x.SoruSecenekleri.Count(s => s.AktifMi) >= 2 &&
                x.SoruSecenekleri.Count(s => s.AktifMi && s.DogruMu) == 1 &&
                x.SoruDersleri.Any(sd =>
                    sd.Ders.KursId == sinavBilgisi.KursId &&
                    sd.Ders.AktifMi &&
                    !sd.Ders.SistemDersiMi))
            .Select(x => x.SoruId)
            .ToListAsync();

        if (gecerliSoruIdleri.Count < soruSayisi)
        {
            return new List<int>();
        }

        // Öğrencinin önceki bitmiş denemeleri alınır.
        var oncekiBitmisDenemeler = await _context.SinavKatilimlari
            .AsNoTracking()
            .Where(x =>
                x.SinavId == sinavId &&
                x.KursKayitId == kursKayitId &&
                x.BitisTarihi != null)
            .OrderByDescending(x => x.BaslamaTarihi)
            .Select(x => new
            {
                x.SinavKatilimId,
                x.BaslamaTarihi
            })
            .ToListAsync();

        var gecerliSoruSeti = gecerliSoruIdleri.ToHashSet();

        var secilenSoruIdleri = new List<int>();
        var secilenSet = new HashSet<int>();

        var oncekiDenemeIdleri = oncekiBitmisDenemeler
            .Select(x => x.SinavKatilimId)
            .ToList();

        // Önceki denemelerde görülen sorular bulunur.
        var dahaOnceGorulenSoruIdleri = oncekiDenemeIdleri.Any()
            ? await _context.OgrenciCevaplari
                .AsNoTracking()
                .Where(x => oncekiDenemeIdleri.Contains(x.SinavKatilimId))
                .Select(x => x.SoruId)
                .Distinct()
                .ToListAsync()
            : new List<int>();

        var sonBitmisDeneme = oncekiBitmisDenemeler.FirstOrDefault();

        // Son denemede yanlış yapılan sorular öncelikli olarak tekrar seçilmeye çalışılır.
        if (sonBitmisDeneme != null)
        {
            var sonDenemeYanlisSoruIdleri = await _context.OgrenciCevaplari
                .AsNoTracking()
                .Where(x =>
                    x.SinavKatilimId == sonBitmisDeneme.SinavKatilimId &&
                    !x.DogruMu)
                .Select(x => x.SoruId)
                .Distinct()
                .ToListAsync();

            ListeyeRandomEkle(
                secilenSoruIdleri,
                secilenSet,
                sonDenemeYanlisSoruIdleri.Where(gecerliSoruSeti.Contains),
                soruSayisi);
        }

        // Daha önce görülmemiş sorular ikinci öncelik olarak eklenir.
        var gorulmemisSoruIdleri = gecerliSoruIdleri
            // exceptin anlamı verilen iki kümenin farkını almaktır. Yani gecerliSoruIdleri kümesinden dahaOnceGorulenSoruIdleri kümesinde olmayanları alırız.
            .Except(dahaOnceGorulenSoruIdleri)
            .ToList();

        ListeyeRandomEkle(
            secilenSoruIdleri,
            secilenSet,
            gorulmemisSoruIdleri,
            soruSayisi);

        // Eksik kalırsa daha önce görülmüş geçerli sorulardan tamamlanır.
        var dahaOnceGorulenGecerliSoruIdleri = gecerliSoruIdleri
            .Intersect(dahaOnceGorulenSoruIdleri)
            .ToList();

        ListeyeRandomEkle(
            secilenSoruIdleri,
            secilenSet,
            dahaOnceGorulenGecerliSoruIdleri,
            soruSayisi);

        return secilenSoruIdleri;
    }

    // Süresi dolmuş ama bitiş tarihi yazılmamış sınavları otomatik değerlendirir.
    private async Task<int> SuresiDolanDevamEdenSinavlariKapatAsync(int kullaniciId)
    {
        var devamEdenSinavlar = await _context.SinavKatilimlari
            .Include(x => x.Sinav)
            .Include(x => x.KursKaydi)
            .Where(x =>
                x.BitisTarihi == null &&
                x.KursKaydi.KullaniciId == kullaniciId &&
                x.KursKaydi.AktifMi)
            .ToListAsync();

        int kapatilanSinavSayisi = 0;

        foreach (var sinavKatilimi in devamEdenSinavlar)
        {
            if (sinavKatilimi.Sinav.SureDakika <= 0)
            {
                continue;
            }

            DateTime sinavBitisLimiti = sinavKatilimi.BaslamaTarihi
                .AddMinutes(sinavKatilimi.Sinav.SureDakika);

            if (DateTime.Now <= sinavBitisLimiti)
            {
                continue;
            }

            bool degerlendirildi = await SinavKatiliminiDegerlendirAsync(
                sinavKatilimi,
                kullaniciId,
                gelenCevapSozlugu: null);

            if (degerlendirildi)
            {
                kapatilanSinavSayisi++;
            }
        }

        return kapatilanSinavSayisi;
    }

    // Bir sınav katılımını değerlendirir, puan hesaplar, geçme durumunu belirler
    // ve başarılıysa kurs tamamlama ile sertifika oluşturma işlemlerini yapar.
    private async Task<bool> SinavKatiliminiDegerlendirAsync(
        SinavKatilimi sinavKatilimi,
        int kullaniciId,
        Dictionary<int, int?>? gelenCevapSozlugu)
    {
        var cevaplar = await _context.OgrenciCevaplari
            .Where(x => x.SinavKatilimId == sinavKatilimi.SinavKatilimId)
            .ToListAsync();

        // Sınav cevap kayıtları yoksa değerlendirme yapılamaz.
        if (!cevaplar.Any())
        {
            return false;
        }

        DateTime simdi = DateTime.Now;

        if (gelenCevapSozlugu != null)
        {
            // Formdan gelen cevaplar veritabanındaki cevap kayıtlarına işlenir.
            foreach (var cevap in cevaplar)
            {
                // Eğer öğrenci o soruya cevap vermediyse secenekId null olarak kalır.
                gelenCevapSozlugu.TryGetValue(cevap.SoruId, out int? secilenSecenekId);

               
                cevap.SecenekId = secilenSecenekId;
            
                cevap.DogruMu = false;
                cevap.VerilmeTarihi = simdi;
            }
        }
        else
        {
            // Süre dolduğunda yeni cevap alınmaz, mevcut kayıtlar üzerinden değerlendirme yapılır.
            foreach (var cevap in cevaplar)
            {
                cevap.DogruMu = false;
                cevap.VerilmeTarihi = simdi;
            }
        }

        var secilenSecenekIdleri = cevaplar
            .Where(x => x.SecenekId.HasValue)
            .Select(x => x.SecenekId!.Value)
            .Distinct()
            .ToList();

        // Seçilen seçeneklerin doğru/yanlış bilgisi tek sorguyla alınır.
        var secenekler = await _context.SoruSecenekleri
            .AsNoTracking()
            .Where(x => secilenSecenekIdleri.Contains(x.SecenekId))
            .Select(x => new
            {
                x.SecenekId,
                x.SoruId,
                x.DogruMu,
                x.AktifMi
            })
            .ToDictionaryAsync(x => x.SecenekId);

        // Her cevap için seçilen seçeneğin gerçekten o soruya ait ve aktif olup olmadığı kontrol edilir.
        foreach (var cevap in cevaplar)
        {
            
            if (!cevap.SecenekId.HasValue)
            {
                cevap.DogruMu = false;
                continue;
            }
            // Eğer seçilen seçenek veritabanında bulunamazsa secenekId null olarak kalır ve cevap yanlış sayılır.
            if (!secenekler.TryGetValue(cevap.SecenekId.Value, out var secenek))
            {
                cevap.SecenekId = null;
                cevap.DogruMu = false;
                continue;
            }

            if (!secenek.AktifMi || secenek.SoruId != cevap.SoruId)
            {
                cevap.SecenekId = null;
                cevap.DogruMu = false;
                continue;
            }

            cevap.DogruMu = secenek.DogruMu;
        }

        int toplamSoruSayisi = cevaplar.Count;

        int dogruSayisi = cevaplar.Count(x => x.DogruMu);

        int alinanPuan = toplamSoruSayisi == 0
            ? 0
            // Puan yüzdelik olarak hesaplanır ve tam sayıya yuvarlanır.
            : (int)Math.Round((dogruSayisi * 100.0) / toplamSoruSayisi);

        bool gectiMi = alinanPuan >= sinavKatilimi.Sinav.GecmeNotu;

        sinavKatilimi.AlinanPuan = alinanPuan;
        sinavKatilimi.GectiMi = gectiMi;
        sinavKatilimi.BitisTarihi = simdi;

        if (gectiMi)
        {
            // Öğrenci sınavı geçerse kurs tamamlandı olarak işaretlenir.
            sinavKatilimi.KursKaydi.TamamlandiMi = true;
            sinavKatilimi.KursKaydi.TamamlanmaTarihi ??= simdi;

            bool sertifikaVarMi = await _context.Sertifikalar
                .AnyAsync(x =>
                    x.KullaniciId == kullaniciId &&
                    x.KursId == sinavKatilimi.KursKaydi.KursId);

            if (!sertifikaVarMi)
            {
                // Daha önce sertifika yoksa benzersiz sertifika kodu ile sertifika oluşturulur.
                var sertifika = new Sertifika
                {
                    KullaniciId = kullaniciId,
                    KursId = sinavKatilimi.KursKaydi.KursId,
                    SertifikaKodu = await BenzersizSertifikaKoduUretAsync(),
                    VerilmeTarihi = simdi
                };

                _context.Sertifikalar.Add(sertifika);

                string kursAdi = await _context.Kurslar
                    .AsNoTracking()
                    .Where(x => x.KursId == sinavKatilimi.KursKaydi.KursId)
                    .Select(x => x.KursAdi)
                    .FirstOrDefaultAsync() ?? "Kurs";

                // Sertifika oluşturulduğunda öğrenciye bildirim gönderilir.
                await _bildirimService.BildirimOlusturAsync(
                    kullaniciId,
                    "Bilgilendirme",
                    "Sınavı başarıyla geçtiniz",
                    $"Tebrikler! \"{kursAdi}\" kursunu başarıyla tamamladınız. Sertifikanız oluşturuldu."
                );
            }
        }

        await _context.SaveChangesAsync();

        if (!gectiMi)
        {
            // Sınav geçilemediyse 3 başarısız hak dolmuş mu kontrol edilir.
            bool kursKaydiPasifeAlindi = await BasarisizHakDolduysaKursKaydiniPasifeAlAsync(
                sinavKatilimi.KursKayitId,
                sinavKatilimi.SinavId
            );

            if (kursKaydiPasifeAlindi)
            {
                TempData["OgrenciHata"] = "Bu sınavdaki 3 hakkınızı da başarısız kullandınız. Kurs kaydınız sonlandırıldı. Kursa yeniden kayıt olarak tekrar başlayabilirsiniz.";
            }
        }

        return true;
    }

    // Verilen aday soru listesinden hedef sayıya ulaşana kadar rastgele ve tekrarsız soru ekler.
    private static void ListeyeRandomEkle(
        List<int> secilenSoruIdleri,
        HashSet<int> secilenSet,
        IEnumerable<int> adaySoruIdleri,
        int hedefSoruSayisi)
    {

        // Aday sorular karıştırılır ve tekrarsız olarak seçilir.
        var karisikAdaylar = adaySoruIdleri
            .Distinct()
            .OrderBy(_ => Guid.NewGuid())
            .ToList();

        foreach (int soruId in karisikAdaylar)
        {
            if (secilenSoruIdleri.Count >= hedefSoruSayisi)
            {
                break;
            }

            // Soru zaten seçilmişse tekrar eklenmez.
            if (secilenSet.Add(soruId))
            {
                secilenSoruIdleri.Add(soruId);
            }
        }
    }

    // Sertifika için benzersiz sertifika kodu üretir.
    private async Task<string> BenzersizSertifikaKoduUretAsync()
    {
        string kod;

        do
        {
            kod = $"CV-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }
        while (await _context.Sertifikalar.AnyAsync(x => x.SertifikaKodu == kod));

        return kod;
    }

    // Öğrenci sınavı 3 kez başarısız olursa kurs kaydını pasife alır.
    private async Task<bool> BasarisizHakDolduysaKursKaydiniPasifeAlAsync(int kursKayitId, int sinavId)
    {
        // Daha önce başarılı bir deneme varsa kurs kaydı pasife alınmaz.
        bool basariliDenemeVar = await _context.SinavKatilimlari
            .AnyAsync(x =>
                x.KursKayitId == kursKayitId &&
                x.SinavId == sinavId &&
                x.BitisTarihi != null &&
                x.GectiMi == true);

        if (basariliDenemeVar)
        {
            return false;
        }

        int basarisizBitmisDenemeSayisi = await _context.SinavKatilimlari
            .CountAsync(x =>
                x.KursKayitId == kursKayitId &&
                x.SinavId == sinavId &&
                x.BitisTarihi != null &&
                x.GectiMi == false);

        if (basarisizBitmisDenemeSayisi < MaksimumSinavHakki)
        {
            return false;
        }

        var kursKaydi = await _context.KursKayitlari
            .Include(x => x.Kurs)
            .FirstOrDefaultAsync(x =>
                x.KursKayitId == kursKayitId &&
                x.AktifMi);

        if (kursKaydi == null)
        {
            return false;
        }

        // 3 başarısız denemeden sonra kurs kaydı pasife alınır.
        kursKaydi.AktifMi = false;
        kursKaydi.TamamlandiMi = false;
        kursKaydi.TamamlanmaTarihi = null;

        await _bildirimService.BildirimOlusturAsync(
            kursKaydi.KullaniciId,
            "Uyarı",
            "Sınav hakkınız sona erdi",
            $"\"{kursKaydi.Kurs.KursAdi}\" kursundaki sınav hakkınız sona erdi. Kurs kaydınız pasife alındı. Dilerseniz kursa tekrar kayıt olarak yeniden başlayabilirsiniz."
        );

        await _context.SaveChangesAsync();

        return true;
    }
}