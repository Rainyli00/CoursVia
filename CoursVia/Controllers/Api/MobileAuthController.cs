using CoursVia.Data;
using CoursVia.Models;
using CoursVia.Services;
using CoursVia.ViewModels.Mobile.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


    

namespace CoursVia.Controllers.Api;

[ApiController]
[Route("api/mobile/auth")]
public class MobileAuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PasswordService _passwordService;
    private readonly IpAdresService _ipAdresService;
    private readonly IConfiguration _configuration;
    private readonly EmailService _emailService;

    public MobileAuthController(
        AppDbContext context,
        PasswordService passwordService,
        IpAdresService ipAdresService,
        IConfiguration configuration,
        EmailService emailService)
    {
        _context = context;
        _passwordService = passwordService;
        _ipAdresService = ipAdresService;
        _configuration = configuration;
        _emailService = emailService;
    }

    // Mobil uygulama giriş endpointi.
    // Kullanıcı e-posta ve şifre gönderir.
    // Bilgiler doğruysa kısa süreli access token ve uzun süreli refresh token döner.
    // Refresh token düz metin olarak DB'ye yazılmaz, hash olarak MobilOturumlari tablosunda saklanır.
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<MobileLoginResponse>> Login([FromBody] MobileLoginRequest? request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.Eposta) ||
            string.IsNullOrWhiteSpace(request.Sifre))
        {
            return BadRequest(new MobileLoginResponse
            {
                Basarili = false,
                Mesaj = "E-posta ve şifre zorunludur."
            });
        }

        string eposta = request.Eposta.Trim().ToLower();

        var kullanici = await _context.Kullanicilar
            .Include(x => x.KullaniciRolleri)
                .ThenInclude(x => x.Rol)
            .FirstOrDefaultAsync(x => x.Eposta.ToLower() == eposta);

        if (kullanici == null)
        {
            return Unauthorized(new MobileLoginResponse
            {
                Basarili = false,
                Mesaj = "E-posta veya şifre hatalı."
            });
        }

        if (kullanici.DurumId != 1)
        {
            return Unauthorized(new MobileLoginResponse
            {
                Basarili = false,
                Mesaj = "Hesabınız aktif değil."
            });
        }

        bool sifreDogruMu = _passwordService.VerifyPassword(request.Sifre, kullanici.SifreHash);

        if (!sifreDogruMu)
        {
            return Unauthorized(new MobileLoginResponse
            {
                Basarili = false,
                Mesaj = "E-posta veya şifre hatalı."
            });
        }

        var roller = kullanici.KullaniciRolleri
            .Select(x => x.Rol.RolAdi)
            .Distinct()
            .ToList();

        if (!roller.Any())
        {
            return Unauthorized(new MobileLoginResponse
            {
                Basarili = false,
                Mesaj = "Bu kullanıcıya ait rol bulunamadı."
            });
        }

        DateTime simdi = DateTime.UtcNow;

        int accessTokenDakika = AccessTokenDakikaGetir();
        int refreshTokenGun = RefreshTokenGunGetir();

        DateTime accessTokenBitisTarihi = simdi.AddMinutes(accessTokenDakika);
        DateTime refreshTokenBitisTarihi = simdi.AddDays(refreshTokenGun);

        string accessToken = AccessTokenOlustur(
            kullanici.KullaniciId,
            kullanici.Ad,
            kullanici.Soyad,
            kullanici.Eposta,
            kullanici.ProfilFotoUrl,
            roller,
            accessTokenBitisTarihi
        );

        string refreshToken = RefreshTokenOlustur();
        string refreshTokenHash = TokenHashOlustur(refreshToken);

        _context.MobilOturumlari.Add(new MobilOturum
        {
            KullaniciId = kullanici.KullaniciId,
            RefreshTokenHash = refreshTokenHash,
            RefreshTokenBitisTarihi = refreshTokenBitisTarihi,
            OlusturmaTarihi = simdi,
            AktifMi = true
        });

        kullanici.SonGirisTarihi = DateTime.Now;
        kullanici.SonIpAdresi = _ipAdresService.IpAdresiGetir();
        kullanici.OnlineMi = true;

        await _context.SaveChangesAsync();

        return Ok(new MobileLoginResponse
        {
            Basarili = true,
            Mesaj = "Giriş başarılı.",

            AccessToken = accessToken,
            AccessTokenBitisTarihi = accessTokenBitisTarihi,

            RefreshToken = refreshToken,
            RefreshTokenBitisTarihi = refreshTokenBitisTarihi,

            Kullanici = KullaniciResponseOlustur(kullanici, roller)
        });
    }

    // Access token süresi dolduğunda yeni access token üretir.
    // Refresh token DB'deki aktif MobilOturum kaydıyla kontrol edilir.
    // Güvenlik için refresh token her yenilemede yeniden üretilir".
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<MobileRefreshTokenResponse>> Refresh([FromBody] MobileRefreshTokenRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new MobileRefreshTokenResponse
            {
                Basarili = false,
                Mesaj = "Refresh token zorunludur."
            });
        }

        string refreshTokenHash = TokenHashOlustur(request.RefreshToken.Trim());
        DateTime simdi = DateTime.UtcNow;

        var oturum = await _context.MobilOturumlari
            .Include(x => x.Kullanici)
                .ThenInclude(x => x.KullaniciRolleri)
                    .ThenInclude(x => x.Rol)
            .FirstOrDefaultAsync(x =>
                x.RefreshTokenHash == refreshTokenHash &&
                x.AktifMi);

        if (oturum == null)
        {
            return Unauthorized(new MobileRefreshTokenResponse
            {
                Basarili = false,
                Mesaj = "Refresh token geçersiz."
            });
        }

        if (oturum.RefreshTokenBitisTarihi <= simdi)
        {
            oturum.AktifMi = false;

            await _context.SaveChangesAsync();

            return Unauthorized(new MobileRefreshTokenResponse
            {
                Basarili = false,
                Mesaj = "Refresh token süresi dolmuş."
            });
        }

        if (oturum.Kullanici.DurumId != 1)
        {
            oturum.AktifMi = false;

            await _context.SaveChangesAsync();

            return Unauthorized(new MobileRefreshTokenResponse
            {
                Basarili = false,
                Mesaj = "Kullanıcı bulunamadı veya aktif değil."
            });
        }

        var roller = oturum.Kullanici.KullaniciRolleri
            .Select(x => x.Rol.RolAdi)
            .Distinct()
            .ToList();

        // Normal şartlarda böyle bir durum olmamalı ama yine de kontrol ediyoruz. sonuçta roller olmadan token üretmek anlamsız olurdu.
        if (!roller.Any())
        {
            oturum.AktifMi = false;

            await _context.SaveChangesAsync();

            return Unauthorized(new MobileRefreshTokenResponse
            {
                Basarili = false,
                Mesaj = "Bu kullanıcıya ait rol bulunamadı."
            });
        }

        int accessTokenDakika = AccessTokenDakikaGetir();
        int refreshTokenGun = RefreshTokenGunGetir();

        DateTime yeniAccessTokenBitisTarihi = simdi.AddMinutes(accessTokenDakika);
        DateTime yeniRefreshTokenBitisTarihi = simdi.AddDays(refreshTokenGun);

        string yeniAccessToken = AccessTokenOlustur(
            oturum.Kullanici.KullaniciId,
            oturum.Kullanici.Ad,
            oturum.Kullanici.Soyad,
            oturum.Kullanici.Eposta,
            oturum.Kullanici.ProfilFotoUrl,
            roller,
            yeniAccessTokenBitisTarihi
        );

        string yeniRefreshToken = RefreshTokenOlustur();
        string yeniRefreshTokenHash = TokenHashOlustur(yeniRefreshToken);

        oturum.RefreshTokenHash = yeniRefreshTokenHash;
        oturum.RefreshTokenBitisTarihi = yeniRefreshTokenBitisTarihi;
        oturum.AktifMi = true;

        oturum.Kullanici.OnlineMi = true;
        oturum.Kullanici.SonIpAdresi = _ipAdresService.IpAdresiGetir();

        await _context.SaveChangesAsync();

        return Ok(new MobileRefreshTokenResponse
        {
            Basarili = true,
            Mesaj = "Token yenilendi.",

            AccessToken = yeniAccessToken,
            AccessTokenBitisTarihi = yeniAccessTokenBitisTarihi,

            RefreshToken = yeniRefreshToken,
            RefreshTokenBitisTarihi = yeniRefreshTokenBitisTarihi
        });
    }

    // Mobil uygulama açıldığında access token hâlâ geçerli mi diye kontrol etmek için kullanılır.
    // Access token geçerliyse kullanıcının güncel bilgilerini döndürür.
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("me")]
    public async Task<ActionResult<MobileLoginResponse>> Me()
    {
        string? kullaniciIdDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(kullaniciIdDegeri, out int kullaniciId))
        {
            return Unauthorized(new MobileLoginResponse
            {
                Basarili = false,
                Mesaj = "Geçersiz token."
            });
        }

        var kullanici = await _context.Kullanicilar
            .AsNoTracking()
            .Include(x => x.KullaniciRolleri)
                .ThenInclude(x => x.Rol)
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (kullanici == null || kullanici.DurumId != 1)
        {
            return Unauthorized(new MobileLoginResponse
            {
                Basarili = false,
                Mesaj = "Kullanıcı bulunamadı veya aktif değil."
            });
        }

        var roller = kullanici.KullaniciRolleri
            .Select(x => x.Rol.RolAdi)
            .Distinct()
            .ToList();

        return Ok(new MobileLoginResponse
        {
            Basarili = true,
            Mesaj = "Kullanıcı bilgileri getirildi.",
            Kullanici = KullaniciResponseOlustur(kullanici, roller)
        });
    }

    [AllowAnonymous]
    [HttpPost("sifremi-unuttum")]
    public async Task<ActionResult<MobileSifremiUnuttumResponseDto>> SifremiUnuttum(
    [FromBody] MobileSifremiUnuttumRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Eposta))
        {
            return BadRequest(new MobileSifremiUnuttumResponseDto
            {
                Basarili = false,
                Mesaj = "E-posta adresi zorunludur.",
                KodGonderildiMi = false
            });
        }

        var eposta = request.Eposta.Trim().ToLower();

        var kullanici = await _context.Kullanicilar
            .FirstOrDefaultAsync(x => x.Eposta.ToLower() == eposta);

        if (kullanici == null)
        {
            return NotFound(new MobileSifremiUnuttumResponseDto
            {
                Basarili = false,
                Mesaj = "Bu e-posta adresi ile kayıtlı kullanıcı bulunamadı.",
                Eposta = eposta,
                KodGonderildiMi = false
            });
        }

        if (kullanici.DurumId != 1)
        {
            return BadRequest(new MobileSifremiUnuttumResponseDto
            {
                Basarili = false,
                Mesaj = "Bu hesap aktif değil.",
                Eposta = kullanici.Eposta,
                KodGonderildiMi = false
            });
        }

        var eskiKodlar = await _context.SifreSifirlamalari
            .Where(x => x.KullaniciId == kullanici.KullaniciId && !x.KullanildiMi)
            .ToListAsync();

        foreach (var eskiKod in eskiKodlar)
        {
            eskiKod.KullanildiMi = true;
        }

        var kod = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

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

            <p>Şifrenizi sıfırlamak için aşağıdaki doğrulama kodunu mobil uygulamada kullanabilirsiniz:</p>

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
            return StatusCode(500, new MobileSifremiUnuttumResponseDto
            {
                Basarili = false,
                Mesaj = "Kod oluşturuldu fakat e-posta gönderilirken hata oluştu.",
                Eposta = kullanici.Eposta,
                KodGonderildiMi = false
            });
        }

        return Ok(new MobileSifremiUnuttumResponseDto
        {
            Basarili = true,
            Mesaj = "Şifre sıfırlama kodu e-posta adresinize gönderildi.",
            Eposta = kullanici.Eposta,
            KodGonderildiMi = true
        });
    }

    [AllowAnonymous]
    [HttpPost("sifre-sifirla")]
    public async Task<ActionResult<MobileSifreSifirlaResponseDto>> SifreSifirla(
        [FromBody] MobileSifreSifirlaRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Eposta) ||
            string.IsNullOrWhiteSpace(request.Kod) ||
            string.IsNullOrWhiteSpace(request.YeniSifre))
        {
            return BadRequest(new MobileSifreSifirlaResponseDto
            {
                Basarili = false,
                Mesaj = "Lütfen e-posta, kod ve yeni şifre alanlarını doldurun."
            });
        }

        if (request.YeniSifre.Length < 6)
        {
            return BadRequest(new MobileSifreSifirlaResponseDto
            {
                Basarili = false,
                Mesaj = "Yeni şifre en az 6 karakter olmalıdır."
            });
        }

        var eposta = request.Eposta.Trim().ToLower();
        var kod = request.Kod.Trim();

        var kullanici = await _context.Kullanicilar
            .FirstOrDefaultAsync(x => x.Eposta.ToLower() == eposta);

        if (kullanici == null)
        {
            return NotFound(new MobileSifreSifirlaResponseDto
            {
                Basarili = false,
                Mesaj = "Kullanıcı bulunamadı."
            });
        }

        if (kullanici.DurumId != 1)
        {
            return BadRequest(new MobileSifreSifirlaResponseDto
            {
                Basarili = false,
                Mesaj = "Bu hesap aktif değil."
            });
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
            return BadRequest(new MobileSifreSifirlaResponseDto
            {
                Basarili = false,
                Mesaj = "Kod hatalı, kullanılmış veya süresi dolmuş."
            });
        }

        kullanici.SifreHash = _passwordService.HashPassword(request.YeniSifre);
        sifreSifirlama.KullanildiMi = true;

        await _context.SaveChangesAsync();

        return Ok(new MobileSifreSifirlaResponseDto
        {
            Basarili = true,
            Mesaj = "Şifreniz başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz."
        });
    }

    // Mobil uygulamadan çıkış işlemi.
    // Sadece refresh token ile çalışır.
    // Access token süresi dolmuş olsa bile logout yapılabilir.
    // Refresh token ile MobilOturum kaydı bulunur ve pasife çekilir.
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<ActionResult<MobileAuthIslemResponse>> Logout([FromBody] MobileLogoutRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new MobileAuthIslemResponse
            {
                Basarili = false,
                Mesaj = "Refresh token zorunludur."
            });
        }

        string refreshTokenHash = TokenHashOlustur(request.RefreshToken.Trim());
        DateTime simdi = DateTime.UtcNow;

        var oturum = await _context.MobilOturumlari
            .FirstOrDefaultAsync(x =>
                x.RefreshTokenHash == refreshTokenHash &&
                x.AktifMi);

        if (oturum == null)
        {
            return Ok(new MobileAuthIslemResponse
            {
                Basarili = true,
                Mesaj = "Çıkış başarılı."
            });
        }

        int kullaniciId = oturum.KullaniciId;

        oturum.AktifMi = false;

        var kullanici = await _context.Kullanicilar
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (kullanici != null)
        {
            bool baskaAktifMobilOturumVarMi = await _context.MobilOturumlari
                .AnyAsync(x =>
                    x.KullaniciId == kullaniciId &&
                    x.MobilOturumId != oturum.MobilOturumId &&
                    x.AktifMi &&
                    x.RefreshTokenBitisTarihi > simdi);

            if (!baskaAktifMobilOturumVarMi)
            {
                kullanici.OnlineMi = false;
            }

            kullanici.SonIpAdresi = _ipAdresService.IpAdresiGetir();
        }

        await _context.SaveChangesAsync();

        return Ok(new MobileAuthIslemResponse
        {
            Basarili = true,
            Mesaj = "Çıkış başarılı."
        });
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("online-durum")]
    public async Task<ActionResult<MobileAuthIslemResponse>> OnlineDurumGuncelle([FromBody] MobileOnlineDurumRequest? request)
    {
        if (request == null)
        {
            return BadRequest(new MobileAuthIslemResponse
            {
                Basarili = false,
                Mesaj = "Online durum bilgisi zorunludur."
            });
        }

        int kullaniciId = KullaniciIdGetir();

        var kullanici = await _context.Kullanicilar
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (kullanici == null)
        {
            return NotFound(new MobileAuthIslemResponse
            {
                Basarili = false,
                Mesaj = "Kullanıcı bulunamadı."
            });
        }

        kullanici.OnlineMi = request.OnlineMi;
        kullanici.SonIpAdresi = _ipAdresService.IpAdresiGetir();

        await _context.SaveChangesAsync();

        return Ok(new MobileAuthIslemResponse
        {
            Basarili = true,
            Mesaj = request.OnlineMi
                ? "Kullanıcı online yapıldı."
                : "Kullanıcı offline yapıldı."
        });
    }

    private string AccessTokenOlustur(
        int kullaniciId,
        string ad,
        string soyad,
        string eposta,
        string? profilFotoUrl,
        List<string> roller,
        DateTime accessTokenBitisTarihi)
    {
        string jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key appsettings.json içinde bulunamadı.");

        string issuer = _configuration["Jwt:Issuer"] ?? "CoursVia";
        string audience = _configuration["Jwt:Audience"] ?? "CoursViaMobile";

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, kullaniciId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, kullaniciId.ToString()),
            new Claim(ClaimTypes.Name, $"{ad} {soyad}".Trim()),
            new Claim(ClaimTypes.Email, eposta),
            new Claim("ProfilFotoUrl", profilFotoUrl ?? "")
        };

        foreach (string rol in roller)
        {
            claims.Add(new Claim(ClaimTypes.Role, rol));
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: accessTokenBitisTarihi,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string RefreshTokenOlustur()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(bytes);
    }

    // Refresh token düz metin olarak DB'ye yazılmaz, hash olarak saklanır. Bu fonksiyon refresh token'ın hash'ini oluşturur.
    private static string TokenHashOlustur(string token)
    {
        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
        byte[] hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }

    private int AccessTokenDakikaGetir()
    {
        return int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out int dakika)
            ? dakika
            : 15;
    }

    private int RefreshTokenGunGetir()
    {
        return int.TryParse(_configuration["Jwt:RefreshTokenDays"], out int gun)
            ? gun
            : 7;
    }

    private static MobileKullaniciResponse KullaniciResponseOlustur(
        Kullanici kullanici,
        List<string> roller)
    {
        return new MobileKullaniciResponse
        {
            KullaniciId = kullanici.KullaniciId,
            AdSoyad = $"{kullanici.Ad} {kullanici.Soyad}".Trim(),
            Eposta = kullanici.Eposta,
            ProfilFotoUrl = kullanici.ProfilFotoUrl,
            Roller = roller
        };
    }

    private int KullaniciIdGetir()
    {
        string? kullaniciIdDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(kullaniciIdDegeri, out int kullaniciId))
        {
            throw new UnauthorizedAccessException("Geçersiz kullanıcı bilgisi.");
        }

        return kullaniciId;
    }
}