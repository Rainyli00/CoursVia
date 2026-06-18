using CoursVia.Data;
using CoursVia.Models;
using CoursVia.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace CoursVia.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly PasswordService _passwordService;
    private readonly EmailService _emailService;
    private readonly IpAdresService _ipAdresService;
    private readonly AdminLogService _adminLogService;
    private readonly KullaniciHesapService _kullaniciHesapService;

    // Account işlemleri için gerekli servisler constructor üzerinden alınır.
    public AccountController(
        AppDbContext context,
        PasswordService passwordService,
        EmailService emailService,
        IpAdresService ipAdresService,
        AdminLogService adminLogService,
        KullaniciHesapService kullaniciHesapService)
    {
        _context = context;
        _passwordService = passwordService;
        _emailService = emailService;
        _ipAdresService = ipAdresService;
        _adminLogService = adminLogService;
        _kullaniciHesapService = kullaniciHesapService;
    }

    // =========================
    // ADMIN LOGIN
    // =========================

    [HttpGet]
    // Admin giriş sayfasını açar.
    public IActionResult AdminLogin()
    {
        var yonlendirme = GirisYapmisKullaniciyiYonlendir();

        if (yonlendirme != null)
        {
            return yonlendirme;
        }

        if (TempData["Hata"] != null)
        {
            ViewBag.Hata = TempData["Hata"];
        }

        return View();
    }

    [HttpPost]
    // Admin kullanıcısının giriş işlemini yapar.
    public async Task<IActionResult> AdminLogin(string? eposta, string? sifre)
    {
        return await RolBazliLogin(
            eposta,
            sifre,
            "Admin",
            "Admin",
            "AdminLogin"
        );
    }

    // =========================
    // EĞİTMEN LOGIN
    // =========================

    [HttpGet]
    // Eğitmen giriş ve başvuru sayfasını açar.
    public async Task<IActionResult> EgitmenLogin(string? tab)
    {
        var yonlendirme = GirisYapmisKullaniciyiYonlendir();

        if (yonlendirme != null)
        {
            return yonlendirme;
        }

        ViewBag.ActiveTab = tab == "register" ? "register" : "login";

        ViewBag.Kategoriler = await _context.Kategoriler
            .OrderBy(x => x.KategoriAdi)
            .ToListAsync();

        if (TempData["Hata"] != null)
        {
            ViewBag.Hata = TempData["Hata"];
        }

        if (TempData["EpostaKontrolEdildi"] != null)
        {
            ViewBag.EpostaKontrolEdildi = true;
            ViewBag.BasvuruEposta = TempData["BasvuruEposta"];
        }

        if (TempData["EgitmenYenidenBasvuruSifreSor"] != null)
        {
            ViewBag.EgitmenYenidenBasvuruSifreSor = true;
            ViewBag.BasvuruEposta = TempData["BasvuruEposta"];
        }

        if (TempData["EgitmenYenidenBasvuru"] != null)
        {
            ViewBag.EgitmenYenidenBasvuru = true;
        }

        return View();
    }

    [HttpPost]
    // Eğitmen girişinde rol ve başvuru onay durumunu kontrol eder.
    public async Task<IActionResult> EgitmenLogin(string? eposta, string? sifre)
    {
        return await RolBazliLogin(
            eposta,
            sifre,
            "Eğitmen",
            "Egitmen",
            "EgitmenLogin"
        );
    }

    // =========================
    // ÖĞRENCİ LOGIN
    // =========================

    [HttpGet]
    // Öğrenci giriş ve kayıt sayfasını açar.
    public IActionResult OgrenciLogin(string? tab)
    {
        var yonlendirme = GirisYapmisKullaniciyiYonlendir();

        if (yonlendirme != null)
        {
            return yonlendirme;
        }

        ViewBag.ActiveTab = tab == "register" ? "register" : "login";

        if (TempData["Hata"] != null)
        {
            ViewBag.Hata = TempData["Hata"];
        }

        if (TempData["KayitEpostaKontrolEdildi"] != null)
        {
            ViewBag.KayitEpostaKontrolEdildi = true;
        }

        if (TempData["MevcutEgitmenHesabi"] != null)
        {
            ViewBag.MevcutEgitmenHesabi = true;
        }

        if (TempData["KayitEposta"] != null)
        {
            ViewBag.KayitEposta = TempData["KayitEposta"];
        }

        if (TempData["LoginEposta"] != null)
        {
            ViewBag.LoginEposta = TempData["LoginEposta"];
        }

        return View();
    }

    [HttpPost]
    // Öğrenci kullanıcısının giriş işlemini yapar.
    public async Task<IActionResult> OgrenciLogin(string? eposta, string? sifre)
    {
        return await RolBazliLogin(
            eposta,
            sifre,
            "Öğrenci",
            "Ogrenci",
            "OgrenciLogin"
        );
    }

    // =========================
    // ORTAK ROL BAZLI LOGIN
    // =========================

    // Admin, eğitmen ve öğrenci girişleri için ortak login kontrolünü yapar.
    private async Task<IActionResult> RolBazliLogin(
        string? eposta,
        string? sifre,
        string hedefRol,
        string hedefController,
        string loginViewName)
    {
        if (string.IsNullOrWhiteSpace(eposta) || string.IsNullOrWhiteSpace(sifre))
        {
            ViewBag.Hata = "E-posta ve şifre zorunludur.";
            await LoginViewBagHazirlaAsync(loginViewName, eposta);
            return View(loginViewName);
        }

        eposta = eposta.Trim().ToLower();

        var kullanici = await _context.Kullanicilar
            .Include(x => x.KullaniciRolleri)
                .ThenInclude(x => x.Rol)
            .Include(x => x.EgitmenProfili)
            .FirstOrDefaultAsync(x => x.Eposta.ToLower() == eposta);

        if (kullanici == null)
        {
            ViewBag.Hata = "E-posta veya şifre hatalı.";
            await LoginViewBagHazirlaAsync(loginViewName, eposta);
            return View(loginViewName);
        }

        if (kullanici.DurumId != 1)
        {
            ViewBag.Hata = "Hesabınız aktif değil.";
            await LoginViewBagHazirlaAsync(loginViewName, eposta);
            return View(loginViewName);
        }

        bool sifreDogruMu = _passwordService.VerifyPassword(sifre, kullanici.SifreHash);

        if (!sifreDogruMu)
        {
            ViewBag.Hata = "E-posta veya şifre hatalı.";
            await LoginViewBagHazirlaAsync(loginViewName, eposta);
            return View(loginViewName);
        }

        var roller = kullanici.KullaniciRolleri
            .Select(x => x.Rol.RolAdi)
            .ToList();

        bool hedefRolVarMi = roller.Contains(hedefRol);

        if (hedefRol == "Eğitmen")
        {
            // Eğitmen girişinde rol dışında başvuru durumunun da onaylı olması gerekir.
            if (kullanici.EgitmenProfili == null)
            {
                ViewBag.Hata = "Bu hesap için eğitmen başvurusu bulunamadı.";
                await LoginViewBagHazirlaAsync(loginViewName, eposta);
                return View(loginViewName);
            }

            if (kullanici.EgitmenProfili.DurumId == 4)
            {
                ViewBag.Hata = "Eğitmen başvurunuz inceleniyor. Lütfen onay sürecinin tamamlanmasını bekleyin.";
                await LoginViewBagHazirlaAsync(loginViewName, eposta);
                return View(loginViewName);
            }

            if (kullanici.EgitmenProfili.DurumId == 6)
            {
                ViewBag.Hata = "Eğitmen başvurunuz reddedildi.";
                await LoginViewBagHazirlaAsync(loginViewName, eposta);
                return View(loginViewName);
            }

            if (kullanici.EgitmenProfili.DurumId != 8 || !hedefRolVarMi)
            {
                ViewBag.Hata = "Eğitmen hesabınız henüz aktif değil.";
                await LoginViewBagHazirlaAsync(loginViewName, eposta);
                return View(loginViewName);
            }
        }
        else
        {
            if (!hedefRolVarMi)
            {
                ViewBag.Hata = $"Bu hesap {hedefRol} girişi için yetkili değil.";
                await LoginViewBagHazirlaAsync(loginViewName, eposta);
                return View(loginViewName);
            }
        }

        kullanici.SonGirisTarihi = DateTime.Now;
        kullanici.SonIpAdresi = _ipAdresService.IpAdresiGetir();
        kullanici.OnlineMi = true;

        await _context.SaveChangesAsync();

        await _kullaniciHesapService.KullaniciGirisYapAsync(kullanici, hedefRol);

        return RedirectToAction("Index", hedefController);
    }

    // =========================
    // ÖĞRENCİ REGISTER
    // =========================

    [HttpGet]
    // Öğrenci kayıt isteğini login sayfasındaki kayıt sekmesine yönlendirir.
    public IActionResult OgrenciRegister()
    {
        return RedirectToAction(nameof(OgrenciLogin));
    }

    [HttpPost]
    // Yeni öğrenci hesabı oluşturur.
    public async Task<IActionResult> OgrenciRegister(
        string? ad,
        string? soyad,
        string? eposta,
        string? sifre,
        string? telefon)
    {
        ViewBag.ActiveTab = "register";

        eposta = string.IsNullOrWhiteSpace(eposta)
            ? string.Empty
            : eposta.Trim().ToLower();

        ViewBag.KayitEpostaKontrolEdildi = !string.IsNullOrWhiteSpace(eposta);
        ViewBag.KayitEposta = eposta;
        ViewBag.MevcutEgitmenHesabi = false;

        if (string.IsNullOrWhiteSpace(ad) ||
            string.IsNullOrWhiteSpace(soyad) ||
            string.IsNullOrWhiteSpace(eposta) ||
            string.IsNullOrWhiteSpace(sifre))
        {
            ViewBag.Hata = "Lütfen zorunlu alanları doldurun.";
            return View("OgrenciLogin");
        }

        var mevcutKullanici = await _context.Kullanicilar
            .Include(x => x.KullaniciRolleri)
            .Include(x => x.EgitmenProfili)
            .FirstOrDefaultAsync(x => x.Eposta.ToLower() == eposta);

        if (mevcutKullanici != null)
        {
            if (mevcutKullanici.KullaniciRolleri.Any(x => x.RolId == 3))
            {
                ViewBag.ActiveTab = "login";
                ViewBag.Hata = "Bu e-posta ile zaten öğrenci hesabı bulunuyor. Giriş yapabilirsiniz.";
                ViewBag.LoginEposta = eposta;
                return View("OgrenciLogin");
            }

            if (mevcutKullanici.KullaniciRolleri.Any(x => x.RolId == 2) ||
                mevcutKullanici.EgitmenProfili != null)
            {
                ViewBag.KayitEpostaKontrolEdildi = true;
                ViewBag.KayitEposta = eposta;
                ViewBag.MevcutEgitmenHesabi = true;
                ViewBag.Hata = "Bu e-posta ile eğitmen hesabı bulunuyor. Öğrenci kaydını tamamlamak için şifrenizi doğrulayın.";
                return View("OgrenciLogin");
            }

            ViewBag.Hata = "Bu e-posta adresi zaten kullanılıyor.";
            return View("OgrenciLogin");
        }

        var kullanici = new Kullanici
        {
            DurumId = 1,
            Ad = ad.Trim(),
            Soyad = soyad.Trim(),
            Eposta = eposta,
            Telefon = string.IsNullOrWhiteSpace(telefon) ? null : telefon.Trim(),
            SifreHash = _passwordService.HashPassword(sifre),
            KayitTarihi = DateTime.Now
        };

        _context.Kullanicilar.Add(kullanici);
        await _context.SaveChangesAsync();

        await _kullaniciHesapService.RolEkleAsync(kullanici.KullaniciId, 3);

        await _adminLogService.KaydetAsync(
            null,
            AdminLogService.SistemKullanici,
            $"{kullanici.Ad} {kullanici.Soyad} öğrenci olarak kayıt oldu. E-posta: {kullanici.Eposta}"
        );

        await _context.SaveChangesAsync();

        TempData["Basari"] = "Kayıt başarılı. Öğrenci girişi yapabilirsiniz.";
        TempData["LoginEposta"] = eposta;

        return RedirectToAction(nameof(OgrenciLogin), new { tab = "login" });
    }

    [HttpPost]
    // Öğrenci kayıt ekranında e-posta adresinin kullanılabilirliğini kontrol eder.
    public async Task<IActionResult> OgrenciKayitEpostaKontrol(string? eposta)
    {
        ViewBag.ActiveTab = "register";

        if (string.IsNullOrWhiteSpace(eposta))
        {
            ViewBag.Hata = "Lütfen e-posta adresinizi girin.";
            return View("OgrenciLogin");
        }

        eposta = eposta.Trim().ToLower();

        var kullanici = await _context.Kullanicilar
            .Include(x => x.KullaniciRolleri)
            .Include(x => x.EgitmenProfili)
            .FirstOrDefaultAsync(x => x.Eposta.ToLower() == eposta);

        if (kullanici == null)
        {
            ViewBag.KayitEpostaKontrolEdildi = true;
            ViewBag.KayitEposta = eposta;
            ViewBag.MevcutEgitmenHesabi = false;
            return View("OgrenciLogin");
        }

        if (kullanici.DurumId != 1)
        {
            ViewBag.Hata = "Bu e-posta ile kayıtlı hesap aktif değil. Lütfen sistem yöneticisiyle iletişime geçin.";
            return View("OgrenciLogin");
        }

        if (kullanici.KullaniciRolleri.Any(x => x.RolId == 3))
        {
            ViewBag.ActiveTab = "login";
            ViewBag.LoginEposta = eposta;
            ViewBag.Hata = "Bu e-posta ile zaten öğrenci hesabı bulunuyor. Giriş yapabilirsiniz.";
            return View("OgrenciLogin");
        }

        if (kullanici.KullaniciRolleri.Any(x => x.RolId == 2) ||
            kullanici.EgitmenProfili != null)
        {
            ViewBag.KayitEpostaKontrolEdildi = true;
            ViewBag.KayitEposta = eposta;
            ViewBag.MevcutEgitmenHesabi = true;
            return View("OgrenciLogin");
        }

        ViewBag.Hata = "Bu e-posta adresi farklı bir hesap için kullanılıyor.";
        return View("OgrenciLogin");
    }

    [HttpPost]
    // Mevcut eğitmen hesabına öğrenci rolü ekler.
    public async Task<IActionResult> OgrenciRolAktiflestir(string? eposta, string? sifre)
    {
        ViewBag.ActiveTab = "register";

        eposta = string.IsNullOrWhiteSpace(eposta)
            ? string.Empty
            : eposta.Trim().ToLower();
        // 
        ViewBag.KayitEpostaKontrolEdildi = true;
        ViewBag.KayitEposta = eposta;
        ViewBag.MevcutEgitmenHesabi = true;

        if (string.IsNullOrWhiteSpace(eposta) || string.IsNullOrWhiteSpace(sifre))
        {
            ViewBag.Hata = "E-posta ve şifre zorunludur.";
            return View("OgrenciLogin");
        }

        var kullanici = await _context.Kullanicilar
            .Include(x => x.KullaniciRolleri)
            .Include(x => x.EgitmenProfili)
            .FirstOrDefaultAsync(x => x.Eposta.ToLower() == eposta);

        if (kullanici == null ||
            kullanici.DurumId != 1 ||
            (!kullanici.KullaniciRolleri.Any(x => x.RolId == 2) &&
             kullanici.EgitmenProfili == null))
        {
            ViewBag.Hata = "Bu e-posta için uygun bir eğitmen hesabı bulunamadı.";
            return View("OgrenciLogin");
        }

        if (kullanici.KullaniciRolleri.Any(x => x.RolId == 3))
        {
            ViewBag.ActiveTab = "login";
            ViewBag.LoginEposta = eposta;
            ViewBag.Hata = "Bu hesap zaten öğrenci girişi için yetkili.";
            return View("OgrenciLogin");
        }

        if (!_passwordService.VerifyPassword(sifre, kullanici.SifreHash))
        {
            ViewBag.Hata = "Şifre hatalı.";
            return View("OgrenciLogin");
        }

        await _kullaniciHesapService.RolEkleAsync(kullanici.KullaniciId, 3);

        await _adminLogService.KaydetAsync(
            null,
            AdminLogService.SistemKullanici,
            $"{kullanici.Ad} {kullanici.Soyad} mevcut eğitmen hesabına öğrenci profili oluşturdu. E-posta: {kullanici.Eposta}"
        );

        await _context.SaveChangesAsync();

        TempData["Basari"] = "Öğrenci kaydınız tamamlandı. Öğrenci girişi yapabilirsiniz.";
        TempData["LoginEposta"] = eposta;

        return RedirectToAction(nameof(OgrenciLogin), new { tab = "login" });
    }

    // =========================
    // EĞİTMEN REGISTER / BAŞVURU
    // =========================

    [HttpGet]
    // Eğitmen kayıt isteğini başvuru sekmesine yönlendirir.
    public IActionResult EgitmenRegister()
    {
        return RedirectToAction(nameof(EgitmenLogin), new { tab = "register" });
    }

    [HttpPost]
    // Yeni eğitmen başvurusu oluşturur veya reddedilen başvuruyu yeniden gönderir.
    public async Task<IActionResult> EgitmenRegister(
        string? ad,
        string? soyad,
        string? eposta,
        string? sifre,
        string? telefon,
        string? uzmanlikAlani,
        string? biyografi,
        int? deneyimYili,
        string? websiteUrl,
        int[]? kategoriIdleri)
    {
        ViewBag.ActiveTab = "register";

        ViewBag.Kategoriler = await _context.Kategoriler
            .OrderBy(x => x.KategoriAdi)
            .ToListAsync();

        eposta = eposta?.Trim().ToLower() ?? "";

        var mevcutKullanici = await _context.Kullanicilar
            .Include(x => x.KullaniciRolleri)
            .Include(x => x.EgitmenProfili)
                .ThenInclude(p => p!.EgitmenBranslari)
            .FirstOrDefaultAsync(x => x.Eposta.ToLower() == eposta);

        bool yenidenBasvuruMu = false;

        if (mevcutKullanici != null)
        {
            if (mevcutKullanici.KullaniciRolleri.Any(x => x.RolId == 3))
            {
                ViewBag.Hata = "Bu e-posta ile kayıtlı bir öğrenci hesabınız var. Lütfen öğrenci panelinizden eğitmen başvurusu yapın.";
                ViewBag.EpostaKontrolEdildi = false;
                ViewBag.BasvuruEposta = null;
                return View("EgitmenLogin");
            }

            if (mevcutKullanici.KullaniciRolleri.Any(x => x.RolId == 2) ||
                mevcutKullanici.KullaniciRolleri.Any(x => x.RolId == 1))
            {
                ViewBag.Hata = "Bu e-posta ile zaten yetkili bir hesabınız var.";
                ViewBag.EpostaKontrolEdildi = false;
                ViewBag.BasvuruEposta = null;
                return View("EgitmenLogin");
            }

            if (mevcutKullanici.EgitmenProfili != null)
            {
                if (mevcutKullanici.EgitmenProfili.DurumId == 4 ||
                    mevcutKullanici.EgitmenProfili.DurumId == 8)
                {
                    ViewBag.Hata = "Bu e-posta ile mevcut veya onaylanmış bir eğitmen başvurunuz bulunuyor.";
                    ViewBag.EpostaKontrolEdildi = false;
                    ViewBag.BasvuruEposta = null;
                    return View("EgitmenLogin");
                }

                if (mevcutKullanici.EgitmenProfili.DurumId == 6)
                {
                    yenidenBasvuruMu = true;
                }
            }

            if (!yenidenBasvuruMu)
            {
                ViewBag.Hata = "Bu e-posta ile kayıtlı bir hesabınız var. Lütfen öğrenci panelinizden eğitmen başvurusu yapın.";
                ViewBag.EpostaKontrolEdildi = false;
                ViewBag.BasvuruEposta = null;
                return View("EgitmenLogin");
            }
        }

        bool eksikAlanVar = false;

        if (yenidenBasvuruMu)
        {
            if (string.IsNullOrWhiteSpace(uzmanlikAlani) ||
                string.IsNullOrWhiteSpace(biyografi) ||
                !deneyimYili.HasValue)
            {
                eksikAlanVar = true;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(ad) ||
                string.IsNullOrWhiteSpace(soyad) ||
                string.IsNullOrWhiteSpace(eposta) ||
                string.IsNullOrWhiteSpace(sifre) ||
                string.IsNullOrWhiteSpace(telefon) ||
                string.IsNullOrWhiteSpace(uzmanlikAlani) ||
                string.IsNullOrWhiteSpace(biyografi) ||
                !deneyimYili.HasValue)
            {
                eksikAlanVar = true;
            }
        }

        if (eksikAlanVar)
        {
            ViewBag.Hata = "Lütfen zorunlu alanları eksiksiz doldurun.";
            ViewBag.EpostaKontrolEdildi = true;
            ViewBag.BasvuruEposta = eposta;

            if (yenidenBasvuruMu)
            {
                ViewBag.EgitmenYenidenBasvuru = true;
            }

            return View("EgitmenLogin");
        }

        if (kategoriIdleri == null || kategoriIdleri.Length == 0)
        {
            ViewBag.Hata = "Lütfen en az bir branş seçin.";
            ViewBag.EpostaKontrolEdildi = true;
            ViewBag.BasvuruEposta = eposta;

            if (yenidenBasvuruMu)
            {
                ViewBag.EgitmenYenidenBasvuru = true;
            }

            return View("EgitmenLogin");
        }

        if (deneyimYili.HasValue && deneyimYili.Value < 0)
        {
            ViewBag.Hata = "Deneyim yılı negatif olamaz.";
            ViewBag.EpostaKontrolEdildi = true;
            ViewBag.BasvuruEposta = eposta;

            if (yenidenBasvuruMu)
            {
                ViewBag.EgitmenYenidenBasvuru = true;
            }

            return View("EgitmenLogin");
        }

        if (!string.IsNullOrEmpty(uzmanlikAlani) && uzmanlikAlani.Length > 300)
        {
            ViewBag.Hata = "Uzmanlık alanı en fazla 300 karakter olabilir.";
            ViewBag.EpostaKontrolEdildi = true;
            ViewBag.BasvuruEposta = eposta;

            if (yenidenBasvuruMu)
            {
                ViewBag.EgitmenYenidenBasvuru = true;
            }

            return View("EgitmenLogin");
        }

        var secilenKategoriIdleri = kategoriIdleri
            .Distinct()
            .ToList();

        var varOlanKategoriIdleri = await _context.Kategoriler
            .Where(x => secilenKategoriIdleri.Contains(x.KategoriId))
            .Select(x => x.KategoriId)
            .ToListAsync();

        if (varOlanKategoriIdleri.Count != secilenKategoriIdleri.Count)
        {
            ViewBag.Hata = "Seçilen branşlardan biri geçersiz.";
            ViewBag.EpostaKontrolEdildi = true;
            ViewBag.BasvuruEposta = eposta;

            if (yenidenBasvuruMu)
            {
                ViewBag.EgitmenYenidenBasvuru = true;
            }

            return View("EgitmenLogin");
        }

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            Kullanici kullanici;
            EgitmenProfili egitmenProfili;

            if (yenidenBasvuruMu && mevcutKullanici != null)
            {
                kullanici = mevcutKullanici;

                egitmenProfili = mevcutKullanici.EgitmenProfili!;
                egitmenProfili.DurumId = 4;
                egitmenProfili.UzmanlikAlani = uzmanlikAlani!.Trim();
                egitmenProfili.Biyografi = biyografi!.Trim();
                egitmenProfili.DeneyimYili = deneyimYili;
                egitmenProfili.WebsiteUrl = string.IsNullOrWhiteSpace(websiteUrl)
                    ? null
                    : websiteUrl.Trim();

                if (egitmenProfili.EgitmenBranslari.Any())
                {
                    _context.EgitmenBranslari.RemoveRange(egitmenProfili.EgitmenBranslari);
                }
            }
            else
            {
                kullanici = new Kullanici
                {
                    DurumId = 1,
                    Ad = ad!.Trim(),
                    Soyad = soyad!.Trim(),
                    Eposta = eposta,
                    Telefon = telefon!.Trim(),
                    SifreHash = _passwordService.HashPassword(sifre!),
                    KayitTarihi = DateTime.Now
                };

                _context.Kullanicilar.Add(kullanici);
                await _context.SaveChangesAsync();

                egitmenProfili = new EgitmenProfili
                {
                    KullaniciId = kullanici.KullaniciId,
                    DurumId = 4,
                    UzmanlikAlani = uzmanlikAlani!.Trim(),
                    Biyografi = biyografi!.Trim(),
                    DeneyimYili = deneyimYili,
                    WebsiteUrl = string.IsNullOrWhiteSpace(websiteUrl)
                        ? null
                        : websiteUrl.Trim()
                };

                _context.EgitmenProfilleri.Add(egitmenProfili);
                await _context.SaveChangesAsync();
            }

            foreach (var kategoriId in secilenKategoriIdleri)
            {
                _context.EgitmenBranslari.Add(new EgitmenBransi
                {
                    EgitmenProfilId = egitmenProfili.EgitmenProfilId,
                    KategoriId = kategoriId
                });
            }

            await _adminLogService.KaydetAsync(
                null,
                AdminLogService.EgitmenBasvurulari,
                yenidenBasvuruMu
                    ? $"{kullanici.Ad} {kullanici.Soyad} reddedilen eğitmen başvurusunu yeniledi. E-posta: {kullanici.Eposta}"
                    : $"{kullanici.Ad} {kullanici.Soyad} eğitmen başvurusu oluşturdu. E-posta: {kullanici.Eposta}"
            );

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            try
            {
                await _emailService.SendEmailAsync(
                    kullanici.Eposta,
                    "CoursVia Eğitmen Başvurunuz Alındı",
                    $@"
                    <h2>Merhaba {kullanici.Ad},</h2>

                    <p>CoursVia eğitmen başvurunuz başarıyla alınmıştır.</p>

                    <p>
                        Başvurunuz admin tarafından incelendikten sonra
                        onay durumunuz size bildirilecektir.
                    </p>

                    <p>
                        Başvurunuz onaylandığında eğitmen paneline giriş yapabileceksiniz.
                    </p>

                    <br />

                    <p>
                        Teşekkürler,<br />
                        <strong>CoursVia Ekibi</strong>
                    </p>
                    "
                );
            }
            catch
            {
                // Mail gönderilemese bile başvuru kaydı bozulmasın.
            }

            TempData["Basari"] = "Eğitmen başvurunuz başarıyla alındı. Başvurunuz onaylandığında eğitmen girişi yapabilirsiniz.";

            return RedirectToAction(nameof(EgitmenLogin));
        }
        catch
        {
            await transaction.RollbackAsync();

            ViewBag.Hata = "Başvuru sırasında bir hata oluştu. Lütfen tekrar deneyin.";
            ViewBag.EpostaKontrolEdildi = true;
            ViewBag.BasvuruEposta = eposta;

            if (yenidenBasvuruMu)
            {
                ViewBag.EgitmenYenidenBasvuru = true;
            }

            return View("EgitmenLogin");
        }
    }

    // =========================
    // ŞİFREMİ UNUTTUM
    // =========================

    [HttpGet]
    // Şifre sıfırlama sayfasını açar.
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    // Kullanıcıya e-posta ile şifre sıfırlama kodu gönderir. Eğer kod zaten oluşturulmuşsa eski kodları geçersiz yapar ve yeni kod oluşturur.
    public async Task<IActionResult> ForgotPassword(string? eposta)
    {
        if (string.IsNullOrWhiteSpace(eposta))
        {
            ViewBag.Hata = "E-posta adresi zorunludur.";
            return View();
        }

        eposta = eposta.Trim().ToLower();

        var kullanici = await _context.Kullanicilar
            .FirstOrDefaultAsync(x => x.Eposta.ToLower() == eposta);

        if (kullanici == null)
        {
            ViewBag.Hata = "Bu e-posta adresi ile kayıtlı kullanıcı bulunamadı.";
            return View();
        }

        var eskiKodlar = await _context.SifreSifirlamalari
            .Where(x => x.KullaniciId == kullanici.KullaniciId && !x.KullanildiMi)
            .ToListAsync();

        foreach (var eskiKod in eskiKodlar)
        {
            eskiKod.KullanildiMi = true;
        }

        string kod = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        var sifreSifirlama = new SifreSifirlama
        {
            KullaniciId = kullanici.KullaniciId,
            Kod = kod,
            OlusturmaTarihi = DateTime.Now,
            GecerlilikTarihi = DateTime.Now.AddMinutes(5),
            KullanildiMi = false
        };

        _context.SifreSifirlamalari.Add(sifreSifirlama);
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendEmailAsync(
                kullanici.Eposta,
                "CoursVia Şifre Sıfırlama Kodu",
                $@"
                <h2>Merhaba {kullanici.Ad},</h2>

                <p>Şifrenizi sıfırlamak için aşağıdaki doğrulama kodunu kullanabilirsiniz:</p>

                <h1 style='letter-spacing: 6px; font-size: 32px;'>{kod}</h1>

                <p>Bu kod <strong>5 dakika</strong> boyunca geçerlidir.</p>

                <p>Eğer bu işlemi siz yapmadıysanız bu e-postayı dikkate almayabilirsiniz.</p>

                <br />

                <p>
                    Teşekkürler,<br />
                    <strong>CoursVia Ekibi</strong>
                </p>
                "
            );
        }
        catch
        {
            ViewBag.Hata = "Kod oluşturuldu fakat e-posta gönderilirken hata oluştu.";
            return View();
        }

        ViewBag.KodGonderildi = true;
        ViewBag.Eposta = kullanici.Eposta;
        TempData["Basari"] = "Şifre sıfırlama kodu e-posta adresinize gönderildi.";

        return View();
    }

    [HttpPost]
    // Doğrulama kodu doğruysa kullanıcının şifresini yeniler.
    public async Task<IActionResult> ResetPassword(
        string? eposta,
        string? kod,
        string? yeniSifre)
    {
        ViewBag.KodGonderildi = true;
        ViewBag.Eposta = eposta;

        if (string.IsNullOrWhiteSpace(eposta) ||
            string.IsNullOrWhiteSpace(kod) ||
            string.IsNullOrWhiteSpace(yeniSifre))
        {
            ViewBag.Hata = "Lütfen tüm alanları doldurun.";
            return View("ForgotPassword");
        }

        eposta = eposta.Trim().ToLower();
        kod = kod.Trim();

        var kullanici = await _context.Kullanicilar
            .FirstOrDefaultAsync(x => x.Eposta.ToLower() == eposta);

        if (kullanici == null)
        {
            ViewBag.Hata = "Kullanıcı bulunamadı.";
            return View("ForgotPassword");
        }

        var sifreSifirlama = await _context.SifreSifirlamalari
            .Where(x =>
                x.KullaniciId == kullanici.KullaniciId &&
                x.Kod == kod &&
                !x.KullanildiMi &&
                x.GecerlilikTarihi > DateTime.Now)
            .OrderByDescending(x => x.OlusturmaTarihi)
            .FirstOrDefaultAsync();

        if (sifreSifirlama == null)
        {
            ViewBag.Hata = "Kod hatalı, kullanılmış veya süresi dolmuş.";
            return View("ForgotPassword");
        }

        kullanici.SifreHash = _passwordService.HashPassword(yeniSifre);
        sifreSifirlama.KullanildiMi = true;

        await _context.SaveChangesAsync();

        TempData["Basari"] = "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz.";

        return RedirectToAction(nameof(OgrenciLogin));
    }

    // =========================
    // LOGOUT / ACCESS DENIED
    // =========================

    // Kullanıcı oturumunu kapatır ve aktif rolüne göre login sayfasına yönlendirir.
    public async Task<IActionResult> Logout()
    {
        var aktifRol = User.FindFirst("AktifRol")?.Value;

        if (User.Identity?.IsAuthenticated == true)
        {
            string? kullaniciIdDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(kullaniciIdDegeri, out int kullaniciId))
            {
                var kullanici = await _context.Kullanicilar
                    .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

                if (kullanici != null)
                {
                    kullanici.SonIpAdresi = _ipAdresService.IpAdresiGetir();
                    kullanici.OnlineMi = false;

                    await _context.SaveChangesAsync();
                }
            }
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        TempData.Clear();

        if (aktifRol == "Admin")
        {
            return RedirectToAction(nameof(AdminLogin));
        }

        if (aktifRol == "Eğitmen")
        {
            return RedirectToAction(nameof(EgitmenLogin));
        }

        return RedirectToAction(nameof(OgrenciLogin));
    }

    [HttpGet]
    // Yetkisiz erişim sayfasını açar.
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    // Çok rollü kullanıcıların aktif panelini değiştirir.
    public async Task<IActionResult> PanelDegistir(string? rol, string? returnUrl = null)
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
        {
            return RedirectToAction(nameof(OgrenciLogin));
        }

        if (string.IsNullOrWhiteSpace(rol))
        {
            return RedirectToAction("Index", "Home");
        }

        if (!User.IsInRole(rol))
        {
            return RedirectToAction(nameof(AccessDenied));
        }


        // Aktif rolü günceller ve kullanıcıyı yeni rolüne göre yönlendirir.
        await _kullaniciHesapService.AktifRolDegistirAsync(rol);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        if (rol == "Admin")
            return RedirectToAction("Index", "Admin");

        if (rol == "Eğitmen")
            return RedirectToAction("Index", "Egitmen");

        if (rol == "Öğrenci")
            return RedirectToAction("Index", "Ogrenci");

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    // Eğitmen başvurusundan önce e-posta durumunu kontrol eder.
    public async Task<IActionResult> EgitmenBasvuruEpostaKontrol(string? eposta)
    {
        if (string.IsNullOrWhiteSpace(eposta))
        {
            TempData["Hata"] = "Lütfen e-posta adresinizi girin.";
            return RedirectToAction(nameof(EgitmenLogin), new { tab = "register" });
        }

        eposta = eposta.Trim().ToLower();

        var kullanici = await _context.Kullanicilar
            .Include(x => x.KullaniciRolleri)
                .ThenInclude(x => x.Rol)
            .Include(x => x.EgitmenProfili)
            .FirstOrDefaultAsync(x => x.Eposta.ToLower() == eposta);

        if (kullanici != null)
        {
            if (kullanici.DurumId != 1)
            {
                TempData["Hata"] = "Bu e-posta ile kayıtlı hesap aktif değil. Lütfen sistem yöneticisiyle iletişime geçin.";
                return RedirectToAction(nameof(EgitmenLogin), new { tab = "register" });
            }

            bool egitmenRoluVarMi = kullanici.KullaniciRolleri
                .Any(x => x.Rol.RolAdi == "Eğitmen");

            if (egitmenRoluVarMi)
            {
                TempData["Hata"] = "Bu e-posta ile zaten eğitmen hesabınız bulunuyor. Lütfen eğitmen girişi yapın.";
                return RedirectToAction(nameof(EgitmenLogin), new { tab = "register" });
            }

            if (kullanici.EgitmenProfili != null)
            {
                if (kullanici.EgitmenProfili.DurumId == 4)
                {
                    TempData["Hata"] = "Bu e-posta ile oluşturulmuş eğitmen başvurunuz zaten onay bekliyor.";
                    return RedirectToAction(nameof(EgitmenLogin), new { tab = "register" });
                }

                if (kullanici.EgitmenProfili.DurumId == 8)
                {
                    TempData["Hata"] = "Bu e-posta ile zaten onaylı bir eğitmen hesabınız bulunuyor. Lütfen eğitmen girişi yapın.";
                    return RedirectToAction(nameof(EgitmenLogin), new { tab = "register" });
                }

                if (kullanici.EgitmenProfili.DurumId == 6)
                {
                    bool ogrenciMi = kullanici.KullaniciRolleri.Any(x => x.Rol.RolAdi == "Öğrenci");

                    if (!ogrenciMi)
                    {
                        TempData["EgitmenYenidenBasvuruSifreSor"] = true;
                        TempData["BasvuruEposta"] = eposta;
                        return RedirectToAction(nameof(EgitmenLogin), new { tab = "register" });
                    }
                }
            }

            TempData["Hata"] = "Bu e-posta ile kayıtlı bir hesabınız var. Lütfen öğrenci panelinizden eğitmen başvurusu yapın.";
            return RedirectToAction(nameof(EgitmenLogin), new { tab = "register" });
        }

        TempData["EpostaKontrolEdildi"] = true;
        TempData["BasvuruEposta"] = eposta;

        return RedirectToAction(nameof(EgitmenLogin), new { tab = "register" });
    }

    [HttpPost]
    // Reddedilmiş eğitmen başvurusu için şifre doğrulaması yapar.
    public async Task<IActionResult> EgitmenYenidenBasvuruSifreKontrol(string? eposta, string? sifre)
    {
        if (string.IsNullOrWhiteSpace(eposta) || string.IsNullOrWhiteSpace(sifre))
        {
            TempData["Hata"] = "E-posta ve şifre zorunludur.";
            TempData["EgitmenYenidenBasvuruSifreSor"] = true;
            TempData["BasvuruEposta"] = eposta;
            return RedirectToAction(nameof(EgitmenLogin), new { tab = "register" });
        }

        eposta = eposta.Trim().ToLower();

        var kullanici = await _context.Kullanicilar
            .Include(x => x.KullaniciRolleri)
                .ThenInclude(x => x.Rol)
            .Include(x => x.EgitmenProfili)
            .FirstOrDefaultAsync(x => x.Eposta.ToLower() == eposta);

        if (kullanici == null ||
            kullanici.DurumId != 1 ||
            kullanici.EgitmenProfili == null ||
            kullanici.EgitmenProfili.DurumId != 6 ||
            kullanici.KullaniciRolleri.Any(x => x.Rol.RolAdi == "Öğrenci") ||
            !_passwordService.VerifyPassword(sifre, kullanici.SifreHash))
        {
            TempData["Hata"] = "Şifre hatalı veya yeniden başvuru için uygun hesap bulunamadı.";
            TempData["EgitmenYenidenBasvuruSifreSor"] = true;
            TempData["BasvuruEposta"] = eposta;
            return RedirectToAction(nameof(EgitmenLogin), new { tab = "register" });
        }

        TempData["EpostaKontrolEdildi"] = true;
        TempData["BasvuruEposta"] = eposta;
        TempData["EgitmenYenidenBasvuru"] = true;

        return RedirectToAction(nameof(EgitmenLogin), new { tab = "register" });
    }

    // =========================
    // HELPER
    // =========================

    // Giriş yapmış kullanıcıyı aktif rolüne göre ilgili panele yönlendirir.
    private IActionResult? GirisYapmisKullaniciyiYonlendir()
    {
        if (User.Identity == null || !User.Identity.IsAuthenticated)
            return null;

        var aktifRol = User.FindFirst("AktifRol")?.Value;

        if (aktifRol == "Admin")
            return RedirectToAction("Index", "Admin");

        if (aktifRol == "Eğitmen")
            return RedirectToAction("Index", "Egitmen");

        if (aktifRol == "Öğrenci")
            return RedirectToAction("Index", "Ogrenci");

        return RedirectToAction("Index", "Home");
    }

    // Login hatalarında view için gerekli sekme ve form bilgilerini hazırlar.
    private async Task LoginViewBagHazirlaAsync(string loginViewName, string? eposta)
    {
        if (loginViewName == "OgrenciLogin")
        {
            ViewBag.ActiveTab = "login";
            ViewBag.LoginEposta = eposta ?? "";
        }

        if (loginViewName == "EgitmenLogin")
        {
            ViewBag.ActiveTab = "login";

            ViewBag.Kategoriler = await _context.Kategoriler
                .OrderBy(x => x.KategoriAdi)
                .ToListAsync();
        }
    }
}