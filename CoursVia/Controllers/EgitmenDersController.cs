using CoursVia.Data;
using CoursVia.Models;
using CoursVia.ViewModels.Egitmen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers;

// Eğitmen panelinde ders ekleme, düzenleme, silme, sıralama ve materyal yükleme işlemlerini yönetir.
[Authorize(Roles = "Eğitmen")]
public class EgitmenDersController : EgitmenBaseController
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public EgitmenDersController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        : base(context)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    // Eğitmenin kendi kursuna yeni ders eklemesini sağlar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DersEkle(
        int kursId,
        int bolumId,
        string dersAdi,
        string videoTipi,
        string? videoUrl,
        IFormFile? videoDosyasi,
        string? aciklama,
        List<string>? materyalBasliklari,
        List<IFormFile>? materyalDosyalari)
    {
        int kullaniciId = AktifKullaniciId;

        var kurs = await _context.Kurslar
            .Include(x => x.Bolumler)
            .FirstOrDefaultAsync(x => x.KursId == kursId && x.EgitmenId == kullaniciId);

        if (kurs == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Kurs düzenlenebilir durumda değilse yeni ders eklenmesine izin verilmez.
        if (!KursDuzenlenebilirMi(kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
        }

        // Seçilen bölümün gerçekten bu kursa ait olup olmadığı kontrol edilir.
        bool bolumBuKursaAitMi = kurs.Bolumler.Any(x => x.BolumId == bolumId);

        if (!bolumBuKursaAitMi)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (string.IsNullOrWhiteSpace(dersAdi))
        {
            TempData["KursHata"] = "Ders adı zorunludur.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
        }

        videoTipi = videoTipi?.Trim().ToLowerInvariant() ?? "";

        string finalVideoUrl;
        var yuklenenFizikselDosyalar = new List<string>();

        // Ders, video ve materyal kayıtları tek işlem gibi yönetilir.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (videoTipi == "link")
            {
                if (string.IsNullOrWhiteSpace(videoUrl))
                {
                    TempData["KursHata"] = "İnternet linki seçildiğinde video linki boş bırakılamaz.";
                    return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
                }

                finalVideoUrl = videoUrl.Trim();
            }
            else if (videoTipi == "dosya")
            {
                if (videoDosyasi == null || videoDosyasi.Length == 0)
                {
                    TempData["KursHata"] = "Lütfen bir video dosyası seçin.";
                    return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
                }

                var izinliVideoUzantilari = new[] { ".mp4", ".webm" };
                string videoUzanti = Path.GetExtension(videoDosyasi.FileName).ToLowerInvariant();

                // Sadece izin verilen video formatları kabul edilir.
                if (!izinliVideoUzantilari.Contains(videoUzanti))
                {
                    TempData["KursHata"] = "Sadece .mp4 ve .webm formatında videolar yükleyebilirsiniz.";
                    return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
                }
                // Videolar için uploads/ders-videolari klasörüne fiziksel dosya kaydedilir ve URL oluşturulur.
                string videoKlasoru = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "uploads",
                    "ders-videolari"
                );
                // Klasör yoksa oluşturulur.
                if (!Directory.Exists(videoKlasoru))
                {
                    Directory.CreateDirectory(videoKlasoru);
                }
                // Dosya adı olarak benzersiz bir GUID kullanılır, böylece aynı isimle dosya yüklenmesi durumunda çakışma olmaz.
                string videoDosyaAdi = $"{Guid.NewGuid()}{videoUzanti}";
                string videoFizikselYol = Path.Combine(videoKlasoru, videoDosyaAdi);
                
                using (var fileStream = new FileStream(videoFizikselYol, FileMode.Create))
                {
                    await videoDosyasi.CopyToAsync(fileStream);
                }

                // Hata olursa yüklenen fiziksel dosya geri silinebilmesi için listeye eklenir.
                yuklenenFizikselDosyalar.Add(videoFizikselYol);

                finalVideoUrl = $"/uploads/ders-videolari/{videoDosyaAdi}";
            }
            else
            {
                TempData["KursHata"] = "Geçersiz video kaynağı.";
                return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
            }

            // Yeni ders aktif normal derslerin sonuna eklenir.
            int yeniSira = (await _context.Dersler
                .Where(x => x.KursId == kursId && x.AktifMi && !x.SistemDersiMi)
                .MaxAsync(x => (int?)x.SiraNo) ?? 0) + 1;

            var yeniDers = new Ders
            {
                KursId = kursId,
                BolumId = bolumId,
                DersAdi = dersAdi.Trim(),
                VideoUrl = finalVideoUrl,
                Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim(),
                SiraNo = yeniSira,
                OlusturmaTarihi = DateTime.Now,
                AktifMi = true,
                SistemDersiMi = false
            };

            _context.Dersler.Add(yeniDers);

            await _context.SaveChangesAsync();

            // Ders kaydedildikten sonra varsa ders materyalleri de yüklenir.
            await DersMateryalleriniKaydetAsync(
                yeniDers.DersId,
                materyalBasliklari,
                materyalDosyalari,
                yuklenenFizikselDosyalar
            );

            // Yayındaki kursa ders eklenirse kurs tekrar taslak durumuna alınır.
            OnayliKursuTaslakYap(kurs);
            kurs.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["KursBasari"] = "Ders başarıyla eklendi.";

            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            // Veritabanı işlemi başarısız olursa yüklenen fiziksel dosyalar da temizlenir.
            YuklenenDosyalariSil(yuklenenFizikselDosyalar);

            TempData["KursHata"] = ex is InvalidOperationException
                ? ex.Message
                : "Ders eklenirken bir hata oluştu. Lütfen tekrar deneyin.";

            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
        }
    }

    // Eğitmenin dersi aktif veya pasif duruma almasını sağlar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DersDurumDegistir(int dersId, bool aktifMi)
    {
        int kullaniciId = AktifKullaniciId;

        var ders = await _context.Dersler
            .Include(x => x.Kurs)
            .FirstOrDefaultAsync(x =>
                x.DersId == dersId &&
                x.Kurs.EgitmenId == kullaniciId &&
                !x.SistemDersiMi);

        if (ders == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!KursDuzenlenebilirMi(ders.Kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
        }

        if (ders.AktifMi == aktifMi)
        {
            TempData["KursHata"] = aktifMi
                ? "Ders zaten aktif durumda."
                : "Ders zaten pasif durumda.";

            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
        }

        ders.AktifMi = aktifMi;

        if (aktifMi)
        {
            // Pasif ders tekrar aktif yapılırsa aktif derslerin sonuna eklenir.
            int yeniSira = (await _context.Dersler
                .Where(x =>
                    x.KursId == ders.KursId &&
                    x.AktifMi &&
                    !x.SistemDersiMi &&
                    x.DersId != ders.DersId)
                .MaxAsync(x => (int?)x.SiraNo) ?? 0) + 1;

            ders.SiraNo = yeniSira;
        }
        else
        {
            // Pasif ders unique sıra çakışması oluşturmaması için negatif sıraya alınır.
            ders.SiraNo = -ders.DersId;
        }

        OnayliKursuTaslakYap(ders.Kurs);
        ders.Kurs.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();

        await AktifDersleriYenidenSiralaAsync(ders.KursId);

        TempData["KursBasari"] = aktifMi
            ? "Ders başarıyla aktif hale getirildi."
            : "Ders başarıyla pasif hale getirildi.";

        return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
    }

    // Eğitmenin mevcut dersi, videosunu ve materyallerini güncellemesini sağlar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DersDuzenle(DersDuzenleViewModel model)
    {
        int kullaniciId = AktifKullaniciId;

        var ders = await _context.Dersler
            .Include(x => x.Kurs)
            .Include(x => x.DersMateryalleri)
            .FirstOrDefaultAsync(x =>
                x.DersId == model.DersId &&
                x.Kurs.EgitmenId == kullaniciId &&
                x.AktifMi &&
                !x.SistemDersiMi);

        if (ders == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!KursDuzenlenebilirMi(ders.Kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
        }

        // Yeni seçilen bölümün bu kursa ait olup olmadığı kontrol edilir.
        bool bolumBuKursaAitMi = await _context.Bolumler
            .AnyAsync(x => x.BolumId == model.BolumId && x.KursId == ders.KursId);

        if (!bolumBuKursaAitMi)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!ModelState.IsValid)
        {
            string hataMesaji = ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage)
                .FirstOrDefault() ?? "Ders bilgilerini kontrol edip tekrar deneyin.";

            TempData["KursHata"] = hataMesaji;
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
        }

        // Formdan gelen materyal Id listeleri temizlenir ve geçerli hale getirilir.
        // Sıralama ve tekrar eden Id'ler dikkate alınmaz.
        model.MevcutMateryalIdleri = model.MevcutMateryalIdleri
            .Where(x => x > 0)
            .ToList();

        model.MevcutMateryalBasliklari ??= new List<string>();

        model.SilinecekMateryalIdleri = model.SilinecekMateryalIdleri
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        var dersMateryalIdleri = ders.DersMateryalleri
            .Select(x => x.MateryalId)
            .ToHashSet();

        // Kullanıcının bu derse ait olmayan materyal Id göndermesi engellenir.
        bool gecersizMateryalVarMi =
            // Mevcut materyal Id'leri arasında dersin materyallerinde olmayan bir Id var mı?
            model.MevcutMateryalIdleri.Any(x => !dersMateryalIdleri.Contains(x)) ||
            model.SilinecekMateryalIdleri.Any(x => !dersMateryalIdleri.Contains(x));

        if (gecersizMateryalVarMi)
        {
            TempData["KursHata"] = "Materyal bilgisi geçersiz. Sayfayı yenileyip tekrar deneyin.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
        }
        // Video kaynağına göre final video URL'si belirlenir. Eğer video değiştirilmeyecekse mevcut URL korunur.
        string finalVideoUrl = ders.VideoUrl;
        var yuklenenFizikselDosyalar = new List<string>();
        var silinecekFizikselDosyalar = new List<string>();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            string videoTipi = model.VideoTipi?.Trim().ToLowerInvariant() ?? "mevcut";

            if (videoTipi == "mevcut")
            {
                finalVideoUrl = ders.VideoUrl;
            }
            else if (videoTipi == "link")
            {
                if (string.IsNullOrWhiteSpace(model.VideoUrl))
                {
                    TempData["KursHata"] = "Video linki boş bırakılamaz.";
                    return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
                }

                finalVideoUrl = model.VideoUrl.Trim();
            }
            else if (videoTipi == "dosya")
            {
                if (model.VideoDosyasi == null || model.VideoDosyasi.Length == 0)
                {
                    TempData["KursHata"] = "Video dosyası seçmelisiniz.";
                    return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
                }

                var izinliVideoUzantilari = new[] { ".mp4", ".webm" };
                string uzanti = Path.GetExtension(model.VideoDosyasi.FileName).ToLowerInvariant();

                if (!izinliVideoUzantilari.Contains(uzanti))
                {
                    TempData["KursHata"] = "Sadece .mp4 ve .webm formatında video yükleyebilirsiniz.";
                    return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
                }
                // Yeni video dosyası uploads/ders-videolari klasörüne kaydedilir. 
                string videoKlasoru = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "uploads",
                    "ders-videolari"
                );
                // Klasör yoksa oluşturulur.
                if (!Directory.Exists(videoKlasoru))
                {
                    Directory.CreateDirectory(videoKlasoru);
                }
                // Yeni dosya adı olarak benzersiz bir GUID kullanılır, böylece aynı isimle dosya yüklenmesi durumunda çakışma olmaz.
                string dosyaAdi = $"{Guid.NewGuid()}{uzanti}";
                string fizikselYol = Path.Combine(videoKlasoru, dosyaAdi);

                using (var fileStream = new FileStream(fizikselYol, FileMode.Create))
                {
                    await model.VideoDosyasi.CopyToAsync(fileStream);
                }

                yuklenenFizikselDosyalar.Add(fizikselYol);
                finalVideoUrl = $"/uploads/ders-videolari/{dosyaAdi}";
            }
            else
            {// Geçersiz video tipi gönderilmesi durumunda işlem iptal edilir.
                TempData["KursHata"] = "Geçersiz video kaynağı.";
                return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
            }

            // Video değiştiyse eski local video dosyası sonradan silinmek üzere listeye alınır.
            if (!string.Equals(finalVideoUrl, ders.VideoUrl, StringComparison.Ordinal))
            {
                DosyaYoluEkleEgerUploadIse(
                    silinecekFizikselDosyalar,
                    ders.VideoUrl,
                    "/uploads/ders-videolari/"
                );
            }

            var silinecekMateryalIdleri = model.SilinecekMateryalIdleri.ToHashSet();

            // Silinmesi istenen materyaller hem veritabanından silinir hem de dosya listesine eklenir.
            foreach (var materyal in ders.DersMateryalleri
                .Where(x => silinecekMateryalIdleri.Contains(x.MateryalId))
                .ToList())
            {
                DosyaYoluEkleEgerUploadIse(
                    silinecekFizikselDosyalar,
                    materyal.MateryalUrl,
                    "/uploads/ders-materyalleri/"
                );

                _context.DersMateryalleri.Remove(materyal);
            }

            var materyallerById = ders.DersMateryalleri
                .ToDictionary(x => x.MateryalId);

            // Mevcut materyallerin başlıkları güncellenir.
            for (int i = 0; i < model.MevcutMateryalIdleri.Count; i++)
            {
                int materyalId = model.MevcutMateryalIdleri[i];

                if (silinecekMateryalIdleri.Contains(materyalId) ||
                    !materyallerById.TryGetValue(materyalId, out var materyal))
                {
                    continue;
                }

                if (i < model.MevcutMateryalBasliklari.Count &&
                    !string.IsNullOrWhiteSpace(model.MevcutMateryalBasliklari[i]))
                {
                    materyal.Baslik = model.MevcutMateryalBasliklari[i].Trim();
                }
            }

            ders.BolumId = model.BolumId;
            ders.DersAdi = model.DersAdi.Trim();
            ders.Aciklama = string.IsNullOrWhiteSpace(model.Aciklama) ? null : model.Aciklama.Trim();
            ders.VideoUrl = finalVideoUrl;

            // Yeni yüklenen materyaller derse eklenir.
            await DersMateryalleriniKaydetAsync(
                ders.DersId,
                model.MateryalBasliklari,
                model.MateryalDosyalari,
                yuklenenFizikselDosyalar
            );

            OnayliKursuTaslakYap(ders.Kurs);
            ders.Kurs.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Veritabanı işlemi başarılı olduktan sonra eski fiziksel dosyalar silinir.
            YuklenenDosyalariSil(silinecekFizikselDosyalar);

            TempData["KursBasari"] = "Ders başarıyla güncellendi.";

            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            // Hata olursa bu işlem sırasında yüklenen yeni dosyalar temizlenir.
            YuklenenDosyalariSil(yuklenenFizikselDosyalar);

            TempData["KursHata"] = ex is InvalidOperationException
                ? ex.Message
                : "Ders güncellenirken bir hata oluştu.";

            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
        }
    }

    // Eğitmenin kendi kursundaki dersi silmesini veya pasife almasını sağlar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DersSil(int dersId)
    {
        int kullaniciId = AktifKullaniciId;

        var ders = await _context.Dersler
            .Include(x => x.Kurs)
            .Include(x => x.DersMateryalleri)
            .FirstOrDefaultAsync(x =>
                x.DersId == dersId &&
                x.Kurs.EgitmenId == kullaniciId);

        if (ders == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        if (!KursDuzenlenebilirMi(ders.Kurs.DurumId))
        {
            TempData["KursHata"] = "Bu kurs şu an düzenlenebilir durumda değil.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
        }

        if (!ders.AktifMi)
        {
            TempData["KursHata"] = "Pasif ders tekrar silinemez.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
        }

        if (ders.SistemDersiMi)
        {
            TempData["KursHata"] = "Sistem dersi silinemez.";
            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = ders.KursId });
        }

        int kursId = ders.KursId;
        var kurs = ders.Kurs;

        // Öğrenci ilerlemesi varsa ders tamamen silinmez, sadece pasife alınır.
        bool ogrenciIlerlemesiVarMi = await _context.DersIlerlemeleri
            .AnyAsync(x => x.DersId == dersId);

        var silinecekFizikselDosyalar = new List<string>();

        // Ders hiç kullanılmadıysa video ve materyal dosyaları da fiziksel olarak silinebilir.
        if (!ogrenciIlerlemesiVarMi)
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

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var soruDersleri = await _context.SoruDersleri
                .Where(x => x.DersId == dersId)
                .ToListAsync();

            // Silinen derse bağlı sınav soruları varsa bağlantılar Diğer sistem dersine aktarılır.
            if (soruDersleri.Any())
            {
                var digerDers = await DigerSistemDersiniGetirVeyaOlusturAsync(kursId);

                var etkilenenSoruIdleri = soruDersleri
                    .Select(x => x.SoruId)
                    .Distinct()
                    .ToList();

                var digerDerseBagliSoruIdListesi = await _context.SoruDersleri
                    .Where(x =>
                        x.DersId == digerDers.DersId &&
                        etkilenenSoruIdleri.Contains(x.SoruId))
                    .Select(x => x.SoruId)
                    .ToListAsync();

                var digerDerseBagliSoruIdleri = digerDerseBagliSoruIdListesi.ToHashSet();

                foreach (var soruDersi in soruDersleri)
                {
                    if (digerDerseBagliSoruIdleri.Contains(soruDersi.SoruId))
                    {
                        _context.SoruDersleri.Remove(soruDersi);
                        continue;
                    }
                    // DersId güncellenerek soru Diğer sistem dersine aktarılır.

                    soruDersi.DersId = digerDers.DersId;
                    digerDerseBagliSoruIdleri.Add(soruDersi.SoruId);
                }

                await _context.SaveChangesAsync();
            }

            if (ogrenciIlerlemesiVarMi)
            {
                ders.AktifMi = false;
            }
            else
            {// Ders tamamen silinir, önce ders materyalleri silinir.
                if (ders.DersMateryalleri.Any())
                {
                    _context.DersMateryalleri.RemoveRange(ders.DersMateryalleri);
                }

                _context.Dersler.Remove(ders);
            }

            await _context.SaveChangesAsync();

            // Ders silindikten veya pasife alındıktan sonra aktif derslerin sırası düzeltilir.
            await AktifDersleriYenidenSiralaAsync(kursId);

            OnayliKursuTaslakYap(kurs);
            kurs.GuncellemeTarihi = DateTime.Now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (!ogrenciIlerlemesiVarMi)
            {
                YuklenenDosyalariSil(silinecekFizikselDosyalar);
            }

            TempData["KursBasari"] = ogrenciIlerlemesiVarMi
                ? soruDersleri.Any()
                    ? "Ders öğrenciler tarafından kullanıldığı için pasife alındı. Bu derse bağlı sorular geçici olarak Diğer dersine aktarıldı."
                    : "Ders öğrenciler tarafından kullanıldığı için pasife alındı."
                : soruDersleri.Any()
                    ? "Ders silindi. Bu derse bağlı sorular geçici olarak Diğer dersine aktarıldı."
                    : "Ders başarıyla silindi.";

            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
        }
        catch
        {
            await transaction.RollbackAsync();

            TempData["KursHata"] = "Ders silinirken bir hata oluştu. Lütfen tekrar deneyin.";

            return RedirectToAction("KursIcerik", "EgitmenKurs", new { id = kursId });
        }
    }

    // Derslerin AJAX ile yeniden sıralanmasını ve farklı bölümlere taşınmasını sağlar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DersSiralaAjax([FromBody] List<DersSiralaViewModel> dersler)
    {
        if (dersler == null || !dersler.Any())
        {
            return BadRequest(new
            {
                success = false,
                message = "Ders listesi boş olamaz."
            });
        }

        // Geçersiz ders/bölüm Id'leri temizlenir ve tekrar eden ders kayıtları teke düşürülür.
        dersler = dersler
            .Where(x => x.DersId > 0 && x.BolumId > 0)
            .GroupBy(x => x.DersId)
            .Select(x => x.First())
            .ToList();

        if (!dersler.Any())
        {
            return BadRequest(new
            {
                success = false,
                message = "Geçerli ders bilgisi gönderilmedi."
            });
        }

        int kullaniciId = AktifKullaniciId;
        int ilkDersId = dersler.First().DersId;

        var kurs = await _context.Kurslar
            .Include(x => x.Bolumler)
            .Include(x => x.Dersler)
            .FirstOrDefaultAsync(x =>
                x.EgitmenId == kullaniciId &&
                x.Dersler.Any(d =>
                    d.DersId == ilkDersId &&
                    d.AktifMi &&
                    !d.SistemDersiMi));

        if (kurs == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Kurs bulunamadı veya yetkiniz yok."
            });
        }

        if (!KursDuzenlenebilirMi(kurs.DurumId))
        {
            return BadRequest(new
            {
                success = false,
                message = "Bu kurs şu an düzenlenebilir durumda değil."
            });
        }

        var aktifNormalDersler = kurs.Dersler
            .Where(x => x.AktifMi && !x.SistemDersiMi)
            .ToList();

        // Frontend tüm aktif dersleri göndermediyse sıralama işlemi kabul edilmez.
        if (dersler.Count != aktifNormalDersler.Count)
        {
            return BadRequest(new
            {
                success = false,
                message = "Ders listesi eksik veya hatalı gönderildi."
            });
        }

        var kursBolumIdleri = kurs.Bolumler
            .Select(x => x.BolumId)
            .ToHashSet();

        // Derslerin taşınacağı bölümlerin bu kursa ait olup olmadığı kontrol edilir.
        bool gecersizBolumVarMi = dersler
            .Any(x => !kursBolumIdleri.Contains(x.BolumId));

        if (gecersizBolumVarMi)
        {
            return BadRequest(new
            {
                success = false,
                message = "Geçersiz bölüm tespit edildi."
            });
        }

        var gonderilenDersIdleri = dersler
            .Select(x => x.DersId)
            .ToHashSet();

        // Gönderilen ders listesinin bu kursun aktif normal dersleriyle eşleştiği kontrol edilir.
        bool gecersizDersVarMi = aktifNormalDersler
            .Any(x => !gonderilenDersIdleri.Contains(x.DersId));

        if (gecersizDersVarMi)
        {
            return BadRequest(new
            {
                success = false,
                message = "Geçersiz veya eksik ders tespit edildi."
            });
        }

        // Sıra numarası çakışmasını önlemek için önce geçici negatif değerler atanır.
        foreach (var ders in aktifNormalDersler)
        {
            ders.SiraNo = -ders.DersId;
        }

        await _context.SaveChangesAsync();

        int sira = 1;

        // Frontend'den gelen sıraya göre derslerin bölümü ve sıra numarası güncellenir.
        foreach (var item in dersler)
        {
            var ders = aktifNormalDersler.First(x => x.DersId == item.DersId);

            ders.BolumId = item.BolumId;
            ders.SiraNo = sira++;
        }

        OnayliKursuTaslakYap(kurs);
        kurs.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true
        });
    }

    // Ders materyallerini fiziksel olarak yükler ve veritabanına kaydeder.
    private async Task DersMateryalleriniKaydetAsync(
        int dersId,
        List<string>? materyalBasliklari,
        List<IFormFile>? materyalDosyalari,
        List<string> yuklenenFizikselDosyalar)
    {
        if (materyalDosyalari == null || !materyalDosyalari.Any())
        {
            return;
        }

        string materyalFolder = Path.Combine(
            _webHostEnvironment.WebRootPath,
            "uploads",
            "ders-materyalleri"
        );

        if (!Directory.Exists(materyalFolder))
        {
            Directory.CreateDirectory(materyalFolder);
        }

        var izinliMateryalUzantilari = new[]
        {
            ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".txt",
            ".jpg", ".jpeg", ".png", ".webp",
            ".mp3", ".wav",
            ".mp4", ".webm",
            ".zip", ".rar",
            ".cs", ".js", ".ts", ".html", ".css", ".json", ".xml",
            ".sql", ".java", ".py", ".cpp", ".c", ".php"
        };

        for (int i = 0; i < materyalDosyalari.Count; i++)
        {
            var dosya = materyalDosyalari[i];

            if (dosya == null || dosya.Length == 0)
            {
                continue;
            }

            string uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();

            // Desteklenmeyen materyal dosyaları yüklenmez.
            if (!izinliMateryalUzantilari.Contains(uzanti))
            {
                throw new InvalidOperationException($"{dosya.FileName} dosya türü desteklenmiyor.");
            }

            string dosyaAdi = $"{Guid.NewGuid()}{uzanti}";
            string tamYol = Path.Combine(materyalFolder, dosyaAdi);

            using (var fileStream = new FileStream(tamYol, FileMode.Create))
            {
                await dosya.CopyToAsync(fileStream);
            }

            // Hata durumunda geri silinebilmesi için yüklenen dosya listeye eklenir.
            yuklenenFizikselDosyalar.Add(tamYol);

            string baslik =
                materyalBasliklari != null &&
                i < materyalBasliklari.Count &&
                !string.IsNullOrWhiteSpace(materyalBasliklari[i])
                    ? materyalBasliklari[i].Trim()
                    : Path.GetFileNameWithoutExtension(dosya.FileName);

            _context.DersMateryalleri.Add(new DersMateryali
            {
                DersId = dersId,
                MateryalTipId = MateryalTipIdBul(uzanti),
                Baslik = baslik,
                MateryalUrl = $"/uploads/ders-materyalleri/{dosyaAdi}",
                YuklenmeTarihi = DateTime.Now
            });
        }
    }

    // Dosya uzantısına göre materyal tipi Id değerini belirler.
    private static int MateryalTipIdBul(string uzanti)
    {
        return uzanti switch
        {
            ".pdf" or ".doc" or ".docx" or ".ppt" or ".pptx" or ".xls" or ".xlsx" or ".txt" => 1,
            ".jpg" or ".jpeg" or ".png" or ".webp" => 2,
            ".mp3" or ".wav" => 3,
            ".mp4" or ".webm" => 4,

            ".cs" or ".js" or ".ts" or ".html" or ".css" or ".json" or ".xml" or
            ".sql" or ".java" or ".py" or ".cpp" or ".c" or ".php" or ".zip" or ".rar" => 5,

            _ => 1
        };
    }

    // Silinen derslere bağlı soru ilişkileri için "Diğer" sistem dersini getirir veya oluşturur.
    private async Task<Ders> DigerSistemDersiniGetirVeyaOlusturAsync(int kursId)
    {
        var digerDers = await _context.Dersler
            .FirstOrDefaultAsync(x =>
                x.KursId == kursId &&
                x.SistemDersiMi &&
                x.AktifMi);

        if (digerDers != null)
        {
            return digerDers;
        }

        var ilkBolum = await _context.Bolumler
            .Where(x => x.KursId == kursId)
            .OrderBy(x => x.SiraNo)
            .FirstOrDefaultAsync();

        if (ilkBolum == null)
        {
            throw new InvalidOperationException("Diğer sistem dersini oluşturmak için kursun en az bir bölümü olmalıdır.");
        }

        // Sistem dersinin normal ders sıralamasında görünmemesi için sıra numarası geriye alınır.
        int sistemSiraNo = (await _context.Dersler
            .Where(x => x.KursId == kursId)
            .MinAsync(x => (int?)x.SiraNo) ?? 0) - 1;

        digerDers = new Ders
        {
            KursId = kursId,
            BolumId = ilkBolum.BolumId,
            DersAdi = "Diğer",
            Aciklama = "Silinen derslerden aktarılan soru bağlantıları için sistem dersidir.",
            VideoUrl = "system://other",
            SiraNo = sistemSiraNo,
            OlusturmaTarihi = DateTime.Now,
            AktifMi = true,
            SistemDersiMi = true
        };

        _context.Dersler.Add(digerDers);

        await _context.SaveChangesAsync();

        return digerDers;
    }

    // Aktif normal dersleri 1'den başlayacak şekilde yeniden sıralar.
    private async Task AktifDersleriYenidenSiralaAsync(int kursId)
    {
        var kursDersleri = await _context.Dersler
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        var aktifDersler = kursDersleri
            .Where(x => x.AktifMi && !x.SistemDersiMi)
            .OrderBy(x => x.SiraNo)
            .ToList();

        // Unique SiraNo çakışmasını önlemek için kurstaki tüm dersler önce geçici negatif değere alınır.
        foreach (var ders in kursDersleri)
        {
            ders.SiraNo = -ders.DersId;
        }

        await _context.SaveChangesAsync();

        int yeniSira = 1;

        // Sadece aktif normal dersler öğrenciye görünecek şekilde 1, 2, 3 diye sıralanır.
        foreach (var ders in aktifDersler)
        {
            ders.SiraNo = yeniSira++;
        }

        await _context.SaveChangesAsync();
    }

    // Verilen URL beklenen upload klasöründeyse fiziksel dosya yolunu listeye ekler.
    private void DosyaYoluEkleEgerUploadIse(List<string> dosyaListesi, string? url, string beklenenPrefix)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (!url.StartsWith(beklenenPrefix))
        {
            return;
        }

        string fizikselYol = Path.Combine(
            _webHostEnvironment.WebRootPath,
            url.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
        );

        dosyaListesi.Add(fizikselYol);
    }

    // Verilen fiziksel dosya yollarındaki dosyaları sistemden siler.
    private static void YuklenenDosyalariSil(List<string> dosyaYollari)
    {
        foreach (var dosyaYolu in dosyaYollari)
        {  // Dosya var mı kontrol edilir ve varsa silinir.
            if (System.IO.File.Exists(dosyaYolu))
            {
                System.IO.File.Delete(dosyaYolu);
            }
        }
    }
}