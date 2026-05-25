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

[Authorize]
public class ProfilAyarlariController : Controller
{
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

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kullanici = await _context.Kullanicilar
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (kullanici == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var model = new ProfilAyarlariViewModel
        {
            Ad = kullanici.Ad,
            Soyad = kullanici.Soyad,
            Eposta = kullanici.Eposta,
            Telefon = kullanici.Telefon
        };

        await ProfilYanVerileriniDoldurAsync(model, kullaniciId, kullanici);

        return View(model);
    }

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

        await ProfilYanVerileriniDoldurAsync(model, kullaniciId, kullanici);

        string eposta = string.IsNullOrWhiteSpace(model.Eposta)
            ? ""
            : model.Eposta.Trim().ToLower();

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

        bool sifreDegistirilecekMi =
            !string.IsNullOrWhiteSpace(model.MevcutSifre) ||
            !string.IsNullOrWhiteSpace(model.YeniSifre) ||
            !string.IsNullOrWhiteSpace(model.YeniSifreTekrar);

        if (sifreDegistirilecekMi)
        {
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

            if (string.IsNullOrWhiteSpace(model.YeniSifre) || model.YeniSifre.Length < 6)
            {
                ModelState.AddModelError(
                    nameof(model.YeniSifre),
                    "Yeni şifre en az 6 karakter olmalıdır."
                );
            }

            if (model.YeniSifre != model.YeniSifreTekrar)
            {
                ModelState.AddModelError(
                    nameof(model.YeniSifreTekrar),
                    "Yeni şifreler eşleşmiyor."
                );
            }
        }

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

        if (!ModelState.IsValid)
        {
            model.MevcutSifre = null;
            model.YeniSifre = null;
            model.YeniSifreTekrar = null;
            model.KirpilmisProfilFotoBase64 = null;

            return View(model);
        }

        kullanici.Ad = model.Ad.Trim();
        kullanici.Soyad = model.Soyad.Trim();
        kullanici.Eposta = eposta;
        kullanici.Telefon = string.IsNullOrWhiteSpace(model.Telefon)
            ? null
            : model.Telefon.Trim();

        if (sifreDegistirilecekMi)
        {
            kullanici.SifreHash = _passwordService.HashPassword(model.YeniSifre!);
        }

        string? eskiProfilFotoUrl = kullanici.ProfilFotoUrl;
        string? silinecekProfilFotoFizikselYolu = null;

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

            silinecekProfilFotoFizikselYolu = FizikselUploadYoluOlustur(
                eskiProfilFotoUrl,
                "/uploads/profil-fotolari/"
            );
        }

        await _context.SaveChangesAsync();

        await _kullaniciHesapService.KullaniciClaimleriniYenileAsync(kullanici);

        DosyaSil(silinecekProfilFotoFizikselYolu);

        TempData["ProfilBasari"] = "Profil ayarları başarıyla güncellendi.";

        return RedirectToAction(nameof(Index));
    }

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

        bool yonetimGecmisiVar = await HesapYonetimGecmisiVarMiAsync(kullanici.KullaniciId);

        if (adminMi && await SonAktifAdminMiAsync(kullanici.KullaniciId))
        {
            TempData["ProfilHata"] = "Sistemde en az bir aktif admin kalmalıdır. Bu hesap profil sayfasından silinemez veya pasife alınamaz.";
            return RedirectToAction(nameof(Index));
        }

        string? profilFotoFizikselYolu = FizikselUploadYoluOlustur(
            kullanici.ProfilFotoUrl,
            "/uploads/profil-fotolari/"
        );

        if (sadeceOgrenciMi && !yonetimGecmisiVar)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
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

        bool kritikBagliKayitVar = await HesapKritikBagliKayitVarMiAsync(kullanici.KullaniciId);

        if (!kritikBagliKayitVar)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await KullaniciYanVerileriniSilAsync(kullanici.KullaniciId);

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OgrenciRolAktiflestirPanelden()
    {
        string? kullaniciIdDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(kullaniciIdDegeri, out int kullaniciId))
        {
            return RedirectToAction("OgrenciLogin", "Account");
        }

        bool ogrenciRoluVarMi = await _context.KullaniciRolleri
            .AnyAsync(x => x.KullaniciId == kullaniciId && x.RolId == 3);

        if (!ogrenciRoluVarMi)
        {
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

        if (!adminMi && !egitmenMi)
        {
            TempData["ProfilHata"] = "Öğrenci profilini ayrı silebilmek için hesabınızda admin veya eğitmen rolü bulunmalıdır.";
            return RedirectToAction(nameof(Index));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
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

        await _kullaniciHesapService.KullaniciClaimleriniYenileAsync(kullanici);

        TempData["ProfilBasari"] = "Öğrenci profiliniz ve öğrenciye ait kayıtlar silindi.";

        return RedirectToAction(nameof(Index));
    }

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

        var roller = await _context.KullaniciRolleri
            .AsNoTracking()
            .Include(x => x.Rol)
            .Where(x => x.KullaniciId == kullaniciId)
            .Select(x => x.Rol.RolAdi)
            .ToListAsync();

        bool ogrenciMi = roller.Contains("Öğrenci");
        bool egitmenMi = roller.Contains("Eğitmen");
        bool adminMi = roller.Contains("Admin");

        var egitmenProfili = await _context.EgitmenProfilleri
            .AsNoTracking()
            .Include(x => x.Durum)
            .Include(x => x.EgitmenBranslari)
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

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

    private string ProfilFotosunuKaydet(string base64Veri)
    {
        byte[] dosyaBytes = ProfilFotoBase64Coz(base64Veri, out string uzanti);

        string klasorYolu = Path.Combine(
            _webHostEnvironment.WebRootPath,
            "uploads",
            "profil-fotolari"
        );

        Directory.CreateDirectory(klasorYolu);

        string dosyaAdi = $"{Guid.NewGuid()}{uzanti}";
        string fizikselYol = Path.Combine(klasorYolu, dosyaAdi);

        System.IO.File.WriteAllBytes(fizikselYol, dosyaBytes);

        return $"/uploads/profil-fotolari/{dosyaAdi}";
    }

    private static byte[] ProfilFotoBase64Coz(string base64Veri, out string uzanti)
    {
        const string base64Ayirici = ";base64,";

        if (!base64Veri.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ||
            !base64Veri.Contains(base64Ayirici, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Profil fotoğrafı geçerli bir görsel formatında değil.");
        }

        string[] parcalar = base64Veri.Split(base64Ayirici, 2, StringSplitOptions.None);
        string mime = parcalar[0].Replace("data:", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant();

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

        if (!fizikselYol.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fizikselYol;
    }

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

    private async Task<bool> HesapKritikBagliKayitVarMiAsync(int kullaniciId)
    {
        if (await _context.Kurslar.AsNoTracking().AnyAsync(x => x.EgitmenId == kullaniciId))
        {
            return true;
        }

        if (await _context.KursKayitlari.AsNoTracking().AnyAsync(x => x.KullaniciId == kullaniciId))
        {
            return true;
        }

        if (await _context.Sertifikalar.AsNoTracking().AnyAsync(x => x.KullaniciId == kullaniciId))
        {
            return true;
        }

        if (await HesapYonetimGecmisiVarMiAsync(kullaniciId))
        {
            return true;
        }

        return false;
    }

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

    private async Task OgrenciHesapVerileriniSilAsync(int kullaniciId)
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

        _context.OgrenciCevaplari.RemoveRange(ogrenciCevaplari);
        _context.SinavKatilimlari.RemoveRange(sinavKatilimlari);
        _context.DersIlerlemeleri.RemoveRange(dersIlerlemeleri);
        _context.KursKayitlari.RemoveRange(kursKayitlari);
        _context.Sertifikalar.RemoveRange(sertifikalar);

        await KullaniciYanVerileriniSilAsync(kullaniciId);
    }

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
