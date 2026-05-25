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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public KullaniciHesapService(
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RolEkleAsync(int kullaniciId, int rolId)
    {
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

    public async Task<List<string>> KullaniciRolleriniGetirAsync(int kullaniciId)
    {
        return await _context.KullaniciRolleri
            .AsNoTracking()
            .Include(x => x.Rol)
            .Where(x => x.KullaniciId == kullaniciId)
            .Select(x => x.Rol.RolAdi)
            .ToListAsync();
    }

    public async Task KullaniciGirisYapAsync(Kullanici kullanici, string aktifRol)
    {
        var roller = await KullaniciRolleriniGetirAsync(kullanici.KullaniciId);

        var claims = KullaniciClaimleriOlustur(kullanici, roller, aktifRol);

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var principal = new ClaimsPrincipal(identity);

        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return;
        }

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal
        );
    }

    public async Task KullaniciClaimleriniYenileAsync(Kullanici kullanici)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return;
        }

        var roller = await KullaniciRolleriniGetirAsync(kullanici.KullaniciId);

        string? aktifRol = httpContext.User.FindFirst("AktifRol")?.Value;

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

    public async Task AktifRolDegistirAsync(string rol)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return;
        }

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

    private static List<Claim> KullaniciClaimleriOlustur(
        Kullanici kullanici,
        List<string> roller,
        string aktifRol)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, kullanici.KullaniciId.ToString()),
            new Claim(ClaimTypes.Name, $"{kullanici.Ad} {kullanici.Soyad}"),
            new Claim(ClaimTypes.Email, kullanici.Eposta),
            new Claim("ProfilFotoUrl", kullanici.ProfilFotoUrl ?? ""),
            new Claim("AktifRol", aktifRol)
        };

        foreach (var rol in roller)
        {
            claims.Add(new Claim(ClaimTypes.Role, rol));
        }

        return claims;
    }

    private static string VarsayilanAktifRolGetir(List<string> roller)
    {
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