using CoursVia.Data;
using CoursVia.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Services;

public class KullaniciHesapService
{
    private readonly AppDbContext _context;
    // IHttpContextAccessor, servis içinde HTTP context'e erişim sağlayarak cookie işlemlerini mümkün kılar.
    private readonly IHttpContextAccessor _httpContextAccessor;

    public KullaniciHesapService(
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    // Kullanıcıya yeni bir rol tanımlar (örneğin Öğrenci rolünün yanına Eğitmen rolü eklemek).
    public async Task RolEkleAsync(int kullaniciId, int rolId)
    {
        // Aynı rol ikinci kez eklenmesin diye önce mevcut ilişki kontrol edilir.
        bool rolVarMi = await _context.KullaniciRolleri
            .AnyAsync(x => x.KullaniciId == kullaniciId && x.RolId == rolId);

        if (rolVarMi)
        {
            return;
        }

        _context.KullaniciRolleri.Add(new KullaniciRol
        {
            KullaniciId = kullaniciId,
            RolId = rolId
        });

        await _context.SaveChangesAsync();
    }

    // Kullanıcının sahip olduğu tüm rollerin isimlerini liste olarak getirir.
    public async Task<List<string>> KullaniciRolleriniGetirAsync(int kullaniciId)
    {
        // Kullanıcının rol adları cookie claim'leri ve yetki kontrolleri için alınır.
        return await _context.KullaniciRolleri
            .AsNoTracking()
            .Include(x => x.Rol)
            .Where(x => x.KullaniciId == kullaniciId)
            .Select(x => x.Rol.RolAdi)
            .ToListAsync();
    }

    // Kullanıcı için gerekli oturum bilgilerini (claim'ler) hazırlar ve sisteme giriş yapmasını (cookie oluşturmasını) sağlar.
    public async Task KullaniciGirisYapAsync(Kullanici kullanici, string aktifRol)
    {
        // Girişte tüm roller ve aktif rol cookie içine claim olarak yazılır.
        var roller = await KullaniciRolleriniGetirAsync(kullanici.KullaniciId);

        var claims = KullaniciClaimleriOlustur(kullanici, roller, aktifRol);

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var principal = new ClaimsPrincipal(identity);

        var httpContext = _httpContextAccessor.HttpContext;

        // Servis HTTP isteği dışında çağrılırsa cookie yazılamaz.
        if (httpContext == null)
        {
            return;
        }

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal
        );
    }

    // Kullanıcının profil bilgileri veya rolleri değiştiğinde mevcut oturum cookie'sini günceller.
    public async Task KullaniciClaimleriniYenileAsync(Kullanici kullanici)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // HTTP context yoksa mevcut oturum claim'leri yenilenemez.
        if (httpContext == null)
        {
            return;
        }

        var roller = await KullaniciRolleriniGetirAsync(kullanici.KullaniciId);

        string? aktifRol = httpContext.User.FindFirst("AktifRol")?.Value;

        // Mevcut aktif rol artık kullanıcının rollerinde yoksa güvenli varsayılan rol seçilir.
        if (string.IsNullOrWhiteSpace(aktifRol) || !roller.Contains(aktifRol))
        {
            aktifRol = VarsayilanAktifRolGetir(roller);
        }

        var claims = KullaniciClaimleriOlustur(kullanici, roller, aktifRol);

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal
        );
    }

    // Kullanıcının oturum içerisindeki aktif rolünü değiştirir (örneğin Eğitmen paneline geçiş yaparken).
    public async Task AktifRolDegistirAsync(string rol)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // Aktif rol değişikliği sadece mevcut web oturumu içinde yapılabilir.
        if (httpContext == null)
        {
            return;
        }

        // Eski AktifRol claim'i çıkarılır ve seçilen rol yeni claim olarak eklenir.
        var claims = httpContext.User.Claims
            .Where(x => x.Type != "AktifRol")
            .ToList();

        claims.Add(new Claim("AktifRol", rol));

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal
        );
    }

    // Oturum cookie'si içine yazılacak temel kullanıcı verilerini ve yetki (claim) listesini hazırlar.
    private static List<Claim> KullaniciClaimleriOlustur(
        Kullanici kullanici,
        List<string> roller,
        string aktifRol)
    {
        // Cookie içinde kullanılacak temel kullanıcı bilgileri ve aktif rol hazırlanır.
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, kullanici.KullaniciId.ToString()),
            new Claim(ClaimTypes.Name, $"{kullanici.Ad} {kullanici.Soyad}"),
            new Claim(ClaimTypes.Email, kullanici.Eposta),
            new Claim("ProfilFotoUrl", kullanici.ProfilFotoUrl ?? ""),
            new Claim("AktifRol", aktifRol)
        };

        // ASP.NET role authorization için her rol ayrı ClaimTypes.Role claim'i olarak eklenir.
        foreach (var rol in roller)
        {
            claims.Add(new Claim(ClaimTypes.Role, rol));
        }

        return claims;
    }

    // Kullanıcının sahip olduğu rollere bakarak sisteme ilk girişte varsayılan olarak hangi rolle başlayacağını belirler.
    private static string VarsayilanAktifRolGetir(List<string> roller)
    {
        // Birden fazla rol varsa en yetkili panel varsayılan aktif rol olur.
        if (roller.Contains("Admin"))
        {
            return "Admin";
        }

        if (roller.Contains("Eğitmen"))
        {
            return "Eğitmen";
        }

        return "Öğrenci";
    }
}
