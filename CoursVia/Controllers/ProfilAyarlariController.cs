using CoursVia.Data;
using CoursVia.Models;
using CoursVia.Services;
using CoursVia.ViewModels.Ogrenci;
using CoursVia.ViewModels.ProfilAyarlari;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers;

// Giriş yapmış kullanıcının profil ayarlarını, şifre değişimini,
// profil fotoğrafını, hesap silme ve rol/profil işlemlerini yönetir.
[Authorize]
public class ProfilAyarlariController : Controller
{
    // Profil fotoğrafı için maksimum dosya boyutu: 2 MB.
    private const long MaksProfilFotoBoyutu = 2 * 1024 * 1024;

    private readonly AppDbContext _context;
    private readonly PasswordService _passwordService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IpAdresService _ipAdresService;
    private readonly AdminLogService _adminLogService;
    private readonly KullaniciHesapService _kullaniciHesapService;

    public ProfilAyarlariController(
        AppDbContext context,
        PasswordService passwordService,
        IWebHostEnvironment webHostEnvironment,
        IpAdresService ipAdresService,
        AdminLogService adminLogService,
        KullaniciHesapService kullaniciHesapService)
    {
        _context = context;
        _passwordService = passwordService;
        _webHostEnvironment = webHostEnvironment;
        _ipAdresService = ipAdresService;
        _adminLogService = adminLogService;
        _kullaniciHesapService = kullaniciHesapService;
    }

    // Profil ayarları sayfasını açar.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Giriş yapan kullanıcının temel profil bilgileri alınır.
        var kullanici = await _context.Kullanicilar
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (kullanici == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Kullanıcı bilgileri ViewModel'e aktarılır.
        var model = new ProfilAyarlariViewModel
        {
            Ad = kullanici.Ad,
            Soyad = kullanici.Soyad,
            Eposta = kullanici.Eposta,
            Telefon = kullanici.Telefon
        };

        // Rol, eğitmen başvurusu, profil fotoğrafı gibi ek bilgiler doldurulur.
        await ProfilYanVerileriniDoldurAsync(model, kullaniciId, kullanici);

        return View(model);
    }

    // Profil bilgilerini, şifreyi ve profil fotoğrafını günceller.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfilAyarlariViewModel model)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kullanici = await _context.Kullanicilar
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (kullanici == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Sayfa tekrar dönerse yan bilgilerin boş kalmaması için model doldurulur.
        await ProfilYanVerileriniDoldurAsync(model, kullaniciId, kullanici);

        // E-posta küçük harfe çevrilir ve boşlukları temizlenir.
        string eposta = string.IsNullOrWhiteSpace(model.Eposta)
            ? ""
            : model.Eposta.Trim().ToLower();

        // E-posta başka kullanıcı tarafından kullanılıyor mu kontrol edilir.
        if (!string.IsNullOrWhiteSpace(eposta))
        {
            bool epostaKullaniliyorMu = await _context.Kullanicilar
                .AnyAsync(x => x.KullaniciId != kullaniciId && x.Eposta.ToLower() == eposta);

            if (epostaKullaniliyorMu)
            {
                ModelState.AddModelError(
                    nameof(model.Eposta),
                    "Bu e-posta adresi başka bir hesap tarafından kullanılıyor."
                );
            }
        }

        // Şifre alanlarından herhangi biri doluysa şifre değiştirme işlemi yapılacak kabul edilir.
        bool sifreDegistirilecekMi =
            !string.IsNullOrWhiteSpace(model.MevcutSifre) ||
            !string.IsNullOrWhiteSpace(model.YeniSifre) ||
            !string.IsNullOrWhiteSpace(model.YeniSifreTekrar);

        if (sifreDegistirilecekMi)
        {
            // Şifre değiştirmek için mevcut şifre zorunludur.
            if (string.IsNullOrWhiteSpace(model.MevcutSifre))
            {
                ModelState.AddModelError(
                    nameof(model.MevcutSifre),
                    "Şifre değiştirmek için mevcut şifrenizi girin."
                );
            }
            else if (!_passwordService.VerifyPassword(model.MevcutSifre, kullanici.SifreHash))
            {
                ModelState.AddModelError(
                    nameof(model.MevcutSifre),
                    "Mevcut şifre hatalı."
                );
            }

            // Yeni şifre minimum uzunluk kontrolü.
            if (string.IsNullOrWhiteSpace(model.YeniSifre) || model.YeniSifre.Length < 6)
            {
                ModelState.AddModelError(
                    nameof(model.YeniSifre),
                    "Yeni şifre en az 6 karakter olmalıdır."
                );
            }

            // Yeni şifre tekrar alanı ile eşleşmeli.
            if (model.YeniSifre != model.YeniSifreTekrar)
            {
                ModelState.AddModelError(
                    nameof(model.YeniSifreTekrar),
                    "Yeni şifreler eşleşmiyor."
                );
            }
        }

        // Kırpılmış profil fotoğrafı geldiyse kaydetmeden önce format ve boyut kontrolü yapılır.
        if (!string.IsNullOrWhiteSpace(model.KirpilmisProfilFotoBase64))
        {
            try
            {
                ProfilFotoBase64Coz(model.KirpilmisProfilFotoBase64, out _);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(
                    nameof(model.KirpilmisProfilFotoBase64),
                    ex.Message
                );
            }
        }

        // Eğitmen başvuru form alanı profil güncelleme ile aynı sayfada (modal içinde) yer aldığı için
        // profil güncellenirken bu alt modelin validasyona takılması engellenir.
        var egitmenBasvuruKeys = ModelState.Keys.Where(k => k.StartsWith(nameof(model.EgitmenBasvuru))).ToList();
        foreach (var key in egitmenBasvuruKeys)
        {
            ModelState.Remove(key);
        }

        // Hata varsa hassas alanlar temizlenerek sayfa geri döndürülür.
        if (!ModelState.IsValid)
        {
            model.MevcutSifre = null;
            model.YeniSifre = null;
            model.YeniSifreTekrar = null;
            model.KirpilmisProfilFotoBase64 = null;

            return View(model);
        }

        // Temel profil bilgileri güncellenir.
        kullanici.Ad = model.Ad.Trim();
        kullanici.Soyad = model.Soyad.Trim();
        kullanici.Eposta = eposta;
        kullanici.Telefon = string.IsNullOrWhiteSpace(model.Telefon)
            ? null
            : model.Telefon.Trim();

        // Şifre değiştirilecekse yeni şifre hashlenerek kaydedilir.
        if (sifreDegistirilecekMi)
        {
            kullanici.SifreHash = _passwordService.HashPassword(model.YeniSifre!);
        }

        string? eskiProfilFotoUrl = kullanici.ProfilFotoUrl;
        string? silinecekProfilFotoFizikselYolu = null;

        // Yeni profil fotoğrafı geldiyse dosya olarak kaydedilir.
        if (!string.IsNullOrWhiteSpace(model.KirpilmisProfilFotoBase64))
        {
            try
            {
                kullanici.ProfilFotoUrl = ProfilFotosunuKaydet(model.KirpilmisProfilFotoBase64);
            }
            catch
            {
                ModelState.AddModelError(
                    nameof(model.KirpilmisProfilFotoBase64),
                    "Profil fotoğrafı kaydedilirken bir hata oluştu. Lütfen tekrar deneyin."
                );

                model.MevcutSifre = null;
                model.YeniSifre = null;
                model.YeniSifreTekrar = null;
                model.KirpilmisProfilFotoBase64 = null;

                await ProfilYanVerileriniDoldurAsync(model, kullaniciId, kullanici);

                return View(model);
            }

            // Yeni fotoğraf kaydedildikten sonra eski fotoğraf silinmek üzere hazırlanır.
            silinecekProfilFotoFizikselYolu = FizikselUploadYoluOlustur(
                eskiProfilFotoUrl,
                "/uploads/profil-fotolari/"
            );
        }

        await _context.SaveChangesAsync();

        // Kullanıcının claim bilgileri güncellenir.
        // Böylece ad, e-posta, fotoğraf gibi bilgiler oturumda da yenilenir.
        await _kullaniciHesapService.KullaniciClaimleriniYenileAsync(kullanici);

        // Veritabanı güncellemesi başarılı olduktan sonra eski profil fotoğrafı silinir.
        DosyaSil(silinecekProfilFotoFizikselYolu);

        TempData["ProfilBasari"] = "Profil ayarları başarıyla güncellendi.";

        return RedirectToAction(nameof(Index));
    }

    // Kullanıcının kendi hesabını silmesini veya kritik kayıt varsa pasife almasını sağlar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HesapSil(string? sifre)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kullanici = await _context.Kullanicilar
            .Include(x => x.KullaniciRolleri)
            .Include(x => x.EgitmenProfili)
                .ThenInclude(x => x!.EgitmenBranslari)
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (kullanici == null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("OgrenciLogin", "Account");
        }

        // Hesap silme için mevcut şifre doğrulanır.
        if (string.IsNullOrWhiteSpace(sifre) ||
            !_passwordService.VerifyPassword(sifre, kullanici.SifreHash))
        {
            TempData["ProfilHata"] = "Hesabı silmek için mevcut şifrenizi doğru girmelisiniz.";
            return RedirectToAction(nameof(Index));
        }

        bool adminMi = kullanici.KullaniciRolleri.Any(x => x.RolId == 1);

        bool egitmenMi =
            kullanici.KullaniciRolleri.Any(x => x.RolId == 2) ||
            kullanici.EgitmenProfili != null;

        bool sadeceOgrenciMi =
            kullanici.KullaniciRolleri.Any(x => x.RolId == 3) &&
            !adminMi &&
            !egitmenMi;

        string silinenKullaniciAdSoyad = $"{kullanici.Ad} {kullanici.Soyad}".Trim();

        if (string.IsNullOrWhiteSpace(silinenKullaniciAdSoyad))
        {
            silinenKullaniciAdSoyad = kullanici.Eposta;
        }

        string silinenKullaniciEposta = kullanici.Eposta;

        // Admin/eğitmen onayları veya admin log geçmişi var mı kontrol edilir.
        bool yonetimGecmisiVar = await HesapYonetimGecmisiVarMiAsync(kullanici.KullaniciId);

        // Sistemde tek aktif admin kalıyorsa bu adminin silinmesine izin verilmez.
        if (adminMi && await SonAktifAdminMiAsync(kullanici.KullaniciId))
        {
            TempData["ProfilHata"] = "Sistemde en az bir aktif admin kalmalıdır. Bu hesap profil sayfasından silinemez veya pasife alınamaz.";
            return RedirectToAction(nameof(Index));
        }

        string? profilFotoFizikselYolu = FizikselUploadYoluOlustur(
            kullanici.ProfilFotoUrl,
            "/uploads/profil-fotolari/"
        );

        // Kullanıcı sadece öğrenci ise ve yönetim geçmişi yoksa tamamen silinebilir.
        if (sadeceOgrenciMi && !yonetimGecmisiVar)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Öğrenciye ait kurs kayıtları, sınavlar, cevaplar, sertifikalar vb. silinir.
                await OgrenciHesapVerileriniSilAsync(kullanici.KullaniciId);

                await _adminLogService.KaydetAsync(
                    null,
                    AdminLogService.SistemKullanici,
                    $"{silinenKullaniciAdSoyad} kullanıcısı kendi hesabını sildi. E-posta: {silinenKullaniciEposta}"
                );

                _context.KullaniciRolleri.RemoveRange(kullanici.KullaniciRolleri);
                _context.Kullanicilar.Remove(kullanici);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["ProfilHata"] = "Hesap silinirken beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Index));
            }

            DosyaSil(profilFotoFizikselYolu);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData.Clear();
            TempData["Basari"] = "Hesabınız kalıcı olarak silindi.";

            return RedirectToAction("OgrenciLogin", "Account");
        }

        // Kullanıcının kurs, kayıt, sertifika veya yönetim gibi kritik bağlı kaydı var mı kontrol edilir.
        bool kritikBagliKayitVar = await HesapKritikBagliKayitVarMiAsync(kullanici.KullaniciId);

        // Kritik bağlı kayıt yoksa hesap tamamen silinebilir.
        if (!kritikBagliKayitVar)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Kullanıcıya ait bildirim, öneri, favori gibi yan veriler silinir.
                await KullaniciYanVerileriniSilAsync(kullanici.KullaniciId);

                // Eğitmen profili varsa branşlarıyla birlikte silinir.
                if (kullanici.EgitmenProfili != null)
                {
                    _context.EgitmenBranslari.RemoveRange(kullanici.EgitmenProfili.EgitmenBranslari);
                    _context.EgitmenProfilleri.Remove(kullanici.EgitmenProfili);
                }

                await _adminLogService.KaydetAsync(
                    null,
                    AdminLogService.SistemKullanici,
                    $"{silinenKullaniciAdSoyad} kullanıcısı kendi hesabını sildi. E-posta: {silinenKullaniciEposta}"
                );

                _context.KullaniciRolleri.RemoveRange(kullanici.KullaniciRolleri);
                _context.Kullanicilar.Remove(kullanici);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["ProfilHata"] = "Hesap silinirken beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Index));
            }

            DosyaSil(profilFotoFizikselYolu);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData.Clear();
            TempData["Basari"] = "Hesabınız kalıcı olarak silindi.";

            return RedirectToAction("OgrenciLogin", "Account");
        }

        // Kritik kayıt varsa kullanıcı fiziksel olarak silinmez, pasife alınır.
        kullanici.DurumId = 2;
        kullanici.OnlineMi = false;
        kullanici.SonIpAdresi = _ipAdresService.IpAdresiGetir();

        await _adminLogService.KaydetAsync(
            null,
            AdminLogService.SistemKullanici,
            $"{silinenKullaniciAdSoyad} kullanıcısı hesap silme talebinde bulundu. Kritik kayıtları olduğu için hesabı pasife alındı. E-posta: {silinenKullaniciEposta}"
        );

        await _context.SaveChangesAsync();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        TempData.Clear();
        TempData["Basari"] = "Hesabınız bağlı kayıtlar bulunduğu için kalıcı silinmedi, pasife alındı.";

        return RedirectToAction("OgrenciLogin", "Account");
    }

    // Admin/eğitmen olan kullanıcının hesabına öğrenci rolü ekler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OgrenciRolAktiflestirPanelden()
    {
        string? kullaniciIdDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(kullaniciIdDegeri, out int kullaniciId))
        {
            return RedirectToAction("OgrenciLogin", "Account");
        }

        // Kullanıcının zaten öğrenci rolü var mı kontrol edilir.
        bool ogrenciRoluVarMi = await _context.KullaniciRolleri
            .AnyAsync(x => x.KullaniciId == kullaniciId && x.RolId == 3);

        if (!ogrenciRoluVarMi)
        {
            // Öğrenci rolü yoksa kullanıcıya öğrenci rolü eklenir.
            await _kullaniciHesapService.RolEkleAsync(kullaniciId, 3);

            var kullanici = await _context.Kullanicilar
                .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

            if (kullanici != null)
            {
                await _adminLogService.KaydetAsync(
                    null,
                    AdminLogService.SistemKullanici,
                    $"{kullanici.Ad} {kullanici.Soyad} mevcut hesabına öğrenci profili oluşturdu. E-posta: {kullanici.Eposta}"
                );

                await _context.SaveChangesAsync();

                // Yeni rol claimlere yansısın diye oturum claimleri yenilenir.
                await _kullaniciHesapService.KullaniciClaimleriniYenileAsync(kullanici);
            }

            TempData["ProfilBasari"] = "Öğrenci profiliniz başarıyla oluşturuldu.";
        }
        else
        {
            TempData["ProfilHata"] = "Zaten öğrenci profiline sahipsiniz.";
        }

        return RedirectToAction(nameof(Index));
    }

    // Admin/eğitmen hesabından sadece öğrenci profilini ve öğrenciye ait kayıtları siler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OgrenciProfilSil()
    {
        string? kullaniciIdDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(kullaniciIdDegeri, out int kullaniciId))
        {
            return RedirectToAction("OgrenciLogin", "Account");
        }

        var kullanici = await _context.Kullanicilar
            .Include(x => x.KullaniciRolleri)
                .ThenInclude(x => x.Rol)
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (kullanici == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        bool ogrenciMi = kullanici.KullaniciRolleri.Any(x => x.RolId == 3);
        bool adminMi = kullanici.KullaniciRolleri.Any(x => x.RolId == 1);
        bool egitmenMi = kullanici.KullaniciRolleri.Any(x => x.RolId == 2);

        if (!ogrenciMi)
        {
            TempData["ProfilHata"] = "Silinecek öğrenci profiliniz bulunmuyor.";
            return RedirectToAction(nameof(Index));
        }

        // Sadece admin/eğitmen olan kullanıcılar öğrenci profilini ayrı silebilir.
        // Sadece öğrenciyse bu işlem yerine hesap silme kullanılmalıdır.
        if (!adminMi && !egitmenMi)
        {
            TempData["ProfilHata"] = "Öğrenci profilini ayrı silebilmek için hesabınızda admin veya eğitmen rolü bulunmalıdır.";
            return RedirectToAction(nameof(Index));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Öğrenci rolüne ait kurs kayıtları, sınavlar, sertifikalar, favoriler vb. silinir.
            await OgrenciProfilVerileriniSilAsync(kullaniciId);

            var ogrenciRolu = kullanici.KullaniciRolleri
                .FirstOrDefault(x => x.RolId == 3);

            if (ogrenciRolu != null)
            {
                _context.KullaniciRolleri.Remove(ogrenciRolu);
            }

            string adSoyad = $"{kullanici.Ad} {kullanici.Soyad}".Trim();

            await _adminLogService.KaydetAsync(
                null,
                AdminLogService.SistemKullanici,
                $"{adSoyad} kullanıcısı kendi öğrenci profilini sildi. E-posta: {kullanici.Eposta}"
            );

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            TempData["ProfilHata"] = "Öğrenci profili silinirken beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";
            return RedirectToAction(nameof(Index));
        }

        // Öğrenci rolü silindiği için claimler yenilenir.
        await _kullaniciHesapService.KullaniciClaimleriniYenileAsync(kullanici);

        TempData["ProfilBasari"] = "Öğrenci profiliniz ve öğrenciye ait kayıtlar silindi.";

        return RedirectToAction(nameof(Index));
    }

    // Profil ayarları sayfasında gereken yan bilgileri doldurur.
    private async Task ProfilYanVerileriniDoldurAsync(
        ProfilAyarlariViewModel model,
        int kullaniciId,
        Kullanici? kullanici = null)
    {
        kullanici ??= await _context.Kullanicilar
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (kullanici == null)
        {
            return;
        }

        // Kullanıcının sahip olduğu roller alınır.
        var roller = await _context.KullaniciRolleri
            .AsNoTracking()
            .Include(x => x.Rol)
            .Where(x => x.KullaniciId == kullaniciId)
            .Select(x => x.Rol.RolAdi)
            .ToListAsync();

        bool ogrenciMi = roller.Contains("Öğrenci");
        bool egitmenMi = roller.Contains("Eğitmen");
        bool adminMi = roller.Contains("Admin");

        // Eğitmen profili varsa başvuru bilgileriyle birlikte alınır.
        var egitmenProfili = await _context.EgitmenProfilleri
            .AsNoTracking()
            .Include(x => x.Durum)
            .Include(x => x.EgitmenBranslari)
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        // Eğitmen başvurusu için kategori/branş seçenekleri hazırlanır.
        var kategoriler = await _context.Kategoriler
            .AsNoTracking()
            .OrderBy(x => x.KategoriAdi)
            .Select(x => new
            {
                x.KategoriId,
                x.KategoriAdi
            })
            .ToListAsync();

        var seciliBransIdleri = egitmenProfili == null
            ? new List<int>()
            : egitmenProfili.EgitmenBranslari
                .Select(x => x.KategoriId)
                .ToList();

        model.KayitTarihi = kullanici.KayitTarihi;
        model.SonGirisTarihi = kullanici.SonGirisTarihi;
        model.AktifRol = User.FindFirst("AktifRol")?.Value;
        model.ProfilFotoUrl = kullanici.ProfilFotoUrl;

        model.OgrenciMi = ogrenciMi;
        model.EgitmenMi = egitmenMi;
        model.AdminMi = adminMi;

        // Profil ayarları ekranındaki eğitmen başvuru bölümü doldurulur.
        model.EgitmenBasvuru = new OgrenciEgitmenBasvuruViewModel
        {
            EgitmenProfilId = egitmenProfili?.EgitmenProfilId,
            MevcutDurumId = egitmenProfili?.DurumId,
            MevcutDurumAdi = egitmenProfili?.Durum?.DurumAdi,

            UzmanlikAlani = egitmenProfili?.UzmanlikAlani,
            Biyografi = egitmenProfili?.Biyografi,
            DeneyimYili = egitmenProfili?.DeneyimYili,
            WebsiteUrl = egitmenProfili?.WebsiteUrl,

            SeciliBransIdleri = seciliBransIdleri,

            BasvuruVarMi = egitmenProfili != null,

            BransSecenekleri = kategoriler
                .Select(x => new OgrenciEgitmenBransSecimViewModel
                {
                    KategoriId = x.KategoriId,
                    KategoriAdi = x.KategoriAdi,
                    SeciliMi = seciliBransIdleri.Contains(x.KategoriId)
                })
                .ToList()
        };
    }

    // Base64 olarak gelen kırpılmış profil fotoğrafını fiziksel dosya olarak kaydeder.
    private string ProfilFotosunuKaydet(string base64Veri)
    {
        // Base64 görsel çözülür ve uzantısı belirlenir.
        byte[] dosyaBytes = ProfilFotoBase64Coz(base64Veri, out string uzanti);

        string klasorYolu = Path.Combine(
            _webHostEnvironment.WebRootPath,
            "uploads",
            "profil-fotolari"
        );

        Directory.CreateDirectory(klasorYolu);

        // Dosya adı GUID ile oluşturulur, böylece aynı isim çakışması engellenir.
        string dosyaAdi = $"{Guid.NewGuid()}{uzanti}";
        string fizikselYol = Path.Combine(klasorYolu, dosyaAdi);

        System.IO.File.WriteAllBytes(fizikselYol, dosyaBytes);

        return $"/uploads/profil-fotolari/{dosyaAdi}";
    }

    // Base64 formatındaki profil fotoğrafını çözer, format ve boyut kontrolü yapar.
    private static byte[] ProfilFotoBase64Coz(string base64Veri, out string uzanti)
    {
        const string base64Ayirici = ";base64,";

        // Gelen verinin data:image/...;base64,... formatında olup olmadığı kontrol edilir.
        if (!base64Veri.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ||
            !base64Veri.Contains(base64Ayirici, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Profil fotoğrafı geçerli bir görsel formatında değil.");
        }

        string[] parcalar = base64Veri.Split(base64Ayirici, 2, StringSplitOptions.None);
        string mime = parcalar[0].Replace("data:", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();

        // Sadece izin verilen görsel türleri kabul edilir.
        uzanti = mime switch
        {
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => throw new InvalidOperationException("Profil fotoğrafı JPG, PNG veya WEBP olmalıdır.")
        };

        byte[] dosyaBytes;

        try
        {
            dosyaBytes = Convert.FromBase64String(parcalar[1]);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Profil fotoğrafı okunamadı. Lütfen tekrar seçin.");
        }

        if (dosyaBytes.Length == 0)
        {
            throw new InvalidOperationException("Profil fotoğrafı boş olamaz.");
        }

        if (dosyaBytes.Length > MaksProfilFotoBoyutu)
        {
            throw new InvalidOperationException("Profil fotoğrafı en fazla 2 MB olabilir.");
        }

        return dosyaBytes;
    }

    // Upload URL bilgisinden güvenli fiziksel dosya yolu üretir.
    private string? FizikselUploadYoluOlustur(string? url, string beklenenPrefix)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith(beklenenPrefix))
        {
            return null;
        }

        string webRoot = Path.GetFullPath(_webHostEnvironment.WebRootPath);

        string fizikselYol = Path.GetFullPath(Path.Combine(
            webRoot,
            url.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
        ));

        // Dosya yolunun wwwroot dışına çıkması engellenir.
        if (!fizikselYol.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fizikselYol;
    }

    // Fiziksel dosyayı siler. Silme başarısız olsa bile ana işlem bozulmaz.
    private static void DosyaSil(string? dosyaYolu)
    {
        if (string.IsNullOrWhiteSpace(dosyaYolu) || !System.IO.File.Exists(dosyaYolu))
        {
            return;
        }

        try
        {
            System.IO.File.Delete(dosyaYolu);
        }
        catch
        {
            // Dosya silinemese de profil kaydı bozulmasın.
        }
    }

    // Silinmek istenen admin sistemdeki son aktif admin mi kontrol eder.
    private async Task<bool> SonAktifAdminMiAsync(int kullaniciId)
    {
        bool hedefAktifAdminMi = await _context.Kullanicilar
            .AsNoTracking()
            .AnyAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.DurumId == 1 &&
                x.KullaniciRolleri.Any(r => r.RolId == 1));

        if (!hedefAktifAdminMi)
        {
            return false;
        }

        int digerAktifAdminSayisi = await _context.Kullanicilar
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciId != kullaniciId &&
                x.DurumId == 1 &&
                x.KullaniciRolleri.Any(r => r.RolId == 1));

        return digerAktifAdminSayisi == 0;
    }

    // Kullanıcının kalıcı silinmesini engelleyecek kritik bağlı kaydı var mı kontrol eder.
    private async Task<bool> HesapKritikBagliKayitVarMiAsync(int kullaniciId)
    {
        if (await _context.Kurslar.AsNoTracking().AnyAsync(x => x.EgitmenId == kullaniciId))
        {
            return true;
        }

        if (await HesapYonetimGecmisiVarMiAsync(kullaniciId))
        {
            return true;
        }


       

        return false;
    }

    // Kullanıcının admin veya onay geçmişi var mı kontrol eder.
    private async Task<bool> HesapYonetimGecmisiVarMiAsync(int kullaniciId)
    {
        if (await _context.AdminLoglari.AsNoTracking().AnyAsync(x => x.AdminId == kullaniciId))
        {
            return true;
        }

        if (await _context.EgitmenOnaylari.AsNoTracking().AnyAsync(x => x.AdminId == kullaniciId))
        {
            return true;
        }

        if (await _context.KursOnaylari.AsNoTracking().AnyAsync(x => x.AdminId == kullaniciId))
        {
            return true;
        }

        return false;
    }

    // Sadece öğrenci hesabı tamamen silinirken öğrenciye ait verileri temizler.
    private async Task OgrenciHesapVerileriniSilAsync(int kullaniciId)
    {
        // Kullanıcının kurs kayıt Id'leri alınır.
        var kursKayitIdleri = await _context.KursKayitlari
            .Where(x => x.KullaniciId == kullaniciId)
            .Select(x => x.KursKayitId)
            .ToListAsync();

        // Bu kurs kayıtlarına bağlı sınav katılım Id'leri alınır.
        var sinavKatilimIdleri = await _context.SinavKatilimlari
            .Where(x => kursKayitIdleri.Contains(x.KursKayitId))
            .Select(x => x.SinavKatilimId)
            .ToListAsync();

        var ogrenciCevaplari = await _context.OgrenciCevaplari
            .Where(x => sinavKatilimIdleri.Contains(x.SinavKatilimId))
            .ToListAsync();

        var sinavKatilimlari = await _context.SinavKatilimlari
            .Where(x => sinavKatilimIdleri.Contains(x.SinavKatilimId))
            .ToListAsync();

        var dersIlerlemeleri = await _context.DersIlerlemeleri
            .Where(x => kursKayitIdleri.Contains(x.KursKayitId))
            .ToListAsync();

        var kursKayitlari = await _context.KursKayitlari
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var sertifikalar = await _context.Sertifikalar
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        // İlişkisel hata olmaması için önce alt kayıtlar, sonra üst kayıtlar silinir.
        _context.OgrenciCevaplari.RemoveRange(ogrenciCevaplari);
        _context.SinavKatilimlari.RemoveRange(sinavKatilimlari);
        _context.DersIlerlemeleri.RemoveRange(dersIlerlemeleri);
        _context.KursKayitlari.RemoveRange(kursKayitlari);
        _context.Sertifikalar.RemoveRange(sertifikalar);

        // Bildirim, öneri, favori gibi genel yan veriler de silinir.
        await KullaniciYanVerileriniSilAsync(kullaniciId);
    }

    // Admin/eğitmen hesabından sadece öğrenci profili silinirken öğrenciye ait verileri temizler.
    private async Task OgrenciProfilVerileriniSilAsync(int kullaniciId)
    {
        var kursKayitIdleri = await _context.KursKayitlari
            .Where(x => x.KullaniciId == kullaniciId)
            .Select(x => x.KursKayitId)
            .ToListAsync();

        var sinavKatilimIdleri = await _context.SinavKatilimlari
            .Where(x => kursKayitIdleri.Contains(x.KursKayitId))
            .Select(x => x.SinavKatilimId)
            .ToListAsync();

        var ogrenciCevaplari = await _context.OgrenciCevaplari
            .Where(x => sinavKatilimIdleri.Contains(x.SinavKatilimId))
            .ToListAsync();

        var sinavKatilimlari = await _context.SinavKatilimlari
            .Where(x => sinavKatilimIdleri.Contains(x.SinavKatilimId))
            .ToListAsync();

        var dersIlerlemeleri = await _context.DersIlerlemeleri
            .Where(x => kursKayitIdleri.Contains(x.KursKayitId))
            .ToListAsync();

        var kursKayitlari = await _context.KursKayitlari
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var sertifikalar = await _context.Sertifikalar
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var favoriler = await _context.Favoriler
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var degerlendirmeler = await _context.KursDegerlendirmeleri
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        // Öğrenciye özel AI çalışma önerileri silinir.
        var ogrenciAiOnerileri = await _context.Oneriler
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                x.OneriTipi.OneriTipAdi == "Öğrenci Çalışma Önerisi")
            .ToListAsync();

        _context.OgrenciCevaplari.RemoveRange(ogrenciCevaplari);
        _context.SinavKatilimlari.RemoveRange(sinavKatilimlari);
        _context.DersIlerlemeleri.RemoveRange(dersIlerlemeleri);
        _context.KursKayitlari.RemoveRange(kursKayitlari);
        _context.Sertifikalar.RemoveRange(sertifikalar);
        _context.Favoriler.RemoveRange(favoriler);
        _context.KursDegerlendirmeleri.RemoveRange(degerlendirmeler);
        _context.Oneriler.RemoveRange(ogrenciAiOnerileri);
    }

    // Kullanıcıya ait genel yan verileri siler.
    private async Task KullaniciYanVerileriniSilAsync(int kullaniciId)
    {
        var bildirimler = await _context.Bildirimler
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var oneriler = await _context.Oneriler
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var sifreSifirlamalari = await _context.SifreSifirlamalari
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var favoriler = await _context.Favoriler
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var degerlendirmeler = await _context.KursDegerlendirmeleri
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var egitmenOnaylari = await _context.EgitmenOnaylari
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        _context.Bildirimler.RemoveRange(bildirimler);
        _context.Oneriler.RemoveRange(oneriler);
        _context.SifreSifirlamalari.RemoveRange(sifreSifirlamalari);
        _context.Favoriler.RemoveRange(favoriler);
        _context.KursDegerlendirmeleri.RemoveRange(degerlendirmeler);
        _context.EgitmenOnaylari.RemoveRange(egitmenOnaylari);
    }
}