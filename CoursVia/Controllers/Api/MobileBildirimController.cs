using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Ortak;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers.Api;

// Mobil ortak bildirim API'si.
// Öğrenci, eğitmen ve admin tarafı aynı endpointleri kullanır.
[ApiController]
[Route("api/mobile/bildirimler")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MobileBildirimController : ControllerBase
{
    private readonly AppDbContext _context;

    public MobileBildirimController(AppDbContext context)
    {
        _context = context;
    }

    // Giriş yapan kullanıcının bildirimlerini listeler.
    // Durum değerleri:
    // tum
    // okunmamis
    // okunmus
    [HttpGet]
    public async Task<ActionResult<MobileBildirimlerResponse>> Bildirimler(
        [FromQuery] string? durum = "tum",
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBasinaKayit = 10)
    {
        int kullaniciId = KullaniciIdGetir();

        durum = string.IsNullOrWhiteSpace(durum)
            ? "tum"
            : durum.Trim().ToLower();

        if (durum != "tum" && durum != "okunmamis" && durum != "okunmus")
        {
            return BadRequest(new MobileBildirimlerResponse
            {
                Basarili = false,
                Mesaj = "Geçersiz durum filtresi. Durum değeri tum, okunmamis veya okunmus olabilir.",
                Durum = durum
            });
        }

        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        var query = _context.Bildirimler
            .AsNoTracking()
            .Where(x => x.KullaniciId == kullaniciId);

        if (durum == "okunmamis")
        {
            query = query.Where(x => !x.OkunduMu);
        }
        else if (durum == "okunmus")
        {
            query = query.Where(x => x.OkunduMu);
        }

        int toplamKayit = await query.CountAsync();

        int toplamSayfa = ToplamSayfaHesapla(
            toplamKayit,
            sayfaBasinaKayit
        );

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        int okunmamisBildirimSayisi = await _context.Bildirimler
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciId == kullaniciId &&
                !x.OkunduMu);

        var bildirimler = await query
            .OrderByDescending(x => x.OlusturmaTarihi)
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new MobileBildirimItemResponse
            {
                BildirimId = x.BildirimId,

                // Modelindeki doğru property adı:
                // BildirimTipId
                BildirimTipId = x.BildirimTipId,

                // Modelindeki doğru property adı:
                // BildirimTipAdi
                BildirimTipAdi = x.BildirimTipi.BildirimTipAdi,

                Baslik = x.Baslik,
                Mesaj = x.Mesaj,

                OkunduMu = x.OkunduMu,
                OlusturmaTarihi = x.OlusturmaTarihi
            })
            .ToListAsync();

        return Ok(new MobileBildirimlerResponse
        {
            Basarili = true,
            Mesaj = "Bildirimler getirildi.",
            Durum = durum,
            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            SayfaBasinaKayit = sayfaBasinaKayit,
            ToplamSayfa = toplamSayfa,
            OkunmamisBildirimSayisi = okunmamisBildirimSayisi,
            Bildirimler = bildirimler
        });
    }

    // Header badge gibi yerlerde hızlı bildirim sayısı almak için kullanılır.
    [HttpGet("ozet")]
    public async Task<ActionResult<MobileBildirimOzetResponse>> Ozet()
    {
        int kullaniciId = KullaniciIdGetir();

        int toplamBildirimSayisi = await _context.Bildirimler
            .AsNoTracking()
            .CountAsync(x => x.KullaniciId == kullaniciId);

        int okunmamisBildirimSayisi = await _context.Bildirimler
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciId == kullaniciId &&
                !x.OkunduMu);

        return Ok(new MobileBildirimOzetResponse
        {
            Basarili = true,
            Mesaj = "Bildirim özeti getirildi.",
            ToplamBildirimSayisi = toplamBildirimSayisi,
            OkunmamisBildirimSayisi = okunmamisBildirimSayisi
        });
    }

    // Tek bir bildirimi okundu yapar.
    [HttpPost("{bildirimId:int}/okundu")]
    public async Task<ActionResult<MobileBildirimIslemResponse>> OkunduYap(
        int bildirimId)
    {
        int kullaniciId = KullaniciIdGetir();

        var bildirim = await _context.Bildirimler
            .FirstOrDefaultAsync(x =>
                x.BildirimId == bildirimId &&
                x.KullaniciId == kullaniciId);

        if (bildirim == null)
        {
            return NotFound(new MobileBildirimIslemResponse
            {
                Basarili = false,
                Mesaj = "Bildirim bulunamadı.",
                OkunmamisBildirimSayisi = await OkunmamisBildirimSayisiGetir(kullaniciId)
            });
        }

        if (!bildirim.OkunduMu)
        {
            bildirim.OkunduMu = true;
            await _context.SaveChangesAsync();
        }

        return Ok(new MobileBildirimIslemResponse
        {
            Basarili = true,
            Mesaj = "Bildirim okundu olarak işaretlendi.",
            OkunmamisBildirimSayisi = await OkunmamisBildirimSayisiGetir(kullaniciId)
        });
    }

    // Tek bir bildirimi okunmamış yapar.
    [HttpPost("{bildirimId:int}/okunmadi")]
    public async Task<ActionResult<MobileBildirimIslemResponse>> OkunmadiYap(
        int bildirimId)
    {
        int kullaniciId = KullaniciIdGetir();

        var bildirim = await _context.Bildirimler
            .FirstOrDefaultAsync(x =>
                x.BildirimId == bildirimId &&
                x.KullaniciId == kullaniciId);

        if (bildirim == null)
        {
            return NotFound(new MobileBildirimIslemResponse
            {
                Basarili = false,
                Mesaj = "Bildirim bulunamadı.",
                OkunmamisBildirimSayisi = await OkunmamisBildirimSayisiGetir(kullaniciId)
            });
        }

        if (bildirim.OkunduMu)
        {
            bildirim.OkunduMu = false;
            await _context.SaveChangesAsync();
        }

        return Ok(new MobileBildirimIslemResponse
        {
            Basarili = true,
            Mesaj = "Bildirim okunmamış olarak işaretlendi.",
            OkunmamisBildirimSayisi = await OkunmamisBildirimSayisiGetir(kullaniciId)
        });
    }

    // Kullanıcının tüm okunmamış bildirimlerini okundu yapar.
    [HttpPost("tumunu-okundu")]
    public async Task<ActionResult<MobileBildirimIslemResponse>> TumunuOkunduYap()
    {
        int kullaniciId = KullaniciIdGetir();

        var okunmamisBildirimler = await _context.Bildirimler
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                !x.OkunduMu)
            .ToListAsync();

        foreach (var bildirim in okunmamisBildirimler)
        {
            bildirim.OkunduMu = true;
        }

        if (okunmamisBildirimler.Any())
        {
            await _context.SaveChangesAsync();
        }

        return Ok(new MobileBildirimIslemResponse
        {
            Basarili = true,
            Mesaj = "Tüm bildirimler okundu olarak işaretlendi.",
            OkunmamisBildirimSayisi = 0
        });
    }

    // Kullanıcının kendi bildirimini siler.
    // Başka kullanıcının bildirimi silinemez.
    [HttpDelete("{bildirimId:int}")]
    public async Task<ActionResult<MobileBildirimIslemResponse>> Sil(
        int bildirimId)
    {
        int kullaniciId = KullaniciIdGetir();

        var bildirim = await _context.Bildirimler
            .FirstOrDefaultAsync(x =>
                x.BildirimId == bildirimId &&
                x.KullaniciId == kullaniciId);

        if (bildirim == null)
        {
            return NotFound(new MobileBildirimIslemResponse
            {
                Basarili = false,
                Mesaj = "Bildirim bulunamadı.",
                OkunmamisBildirimSayisi = await OkunmamisBildirimSayisiGetir(kullaniciId)
            });
        }

        _context.Bildirimler.Remove(bildirim);
        await _context.SaveChangesAsync();

        return Ok(new MobileBildirimIslemResponse
        {
            Basarili = true,
            Mesaj = "Bildirim silindi.",
            OkunmamisBildirimSayisi = await OkunmamisBildirimSayisiGetir(kullaniciId)
        });
    }

    // JWT token içinden giriş yapan kullanıcının KullaniciId değerini alır.
    private int KullaniciIdGetir()
    {
        string? kullaniciIdDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(kullaniciIdDegeri, out int kullaniciId))
        {
            throw new UnauthorizedAccessException("Geçersiz kullanıcı bilgisi.");
        }

        return kullaniciId;
    }

    // Giriş yapan kullanıcının okunmamış bildirim sayısını getirir.
    private async Task<int> OkunmamisBildirimSayisiGetir(int kullaniciId)
    {
        return await _context.Bildirimler
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciId == kullaniciId &&
                !x.OkunduMu);
    }

    // Sayfa değerini normalize eder.
    private static int SayfaNormalizeEt(int sayfa)
    {
        return sayfa < 1 ? 1 : sayfa;
    }

    // Sayfa başına kayıt değerini normalize eder.
    private static int SayfaBasinaKayitNormalizeEt(int sayfaBasinaKayit)
    {
        if (sayfaBasinaKayit < 1)
        {
            return 10;
        }

        if (sayfaBasinaKayit > 50)
        {
            return 50;
        }

        return sayfaBasinaKayit;
    }

    // Toplam sayfa sayısını hesaplar.
    private static int ToplamSayfaHesapla(
        int toplamKayit,
        int sayfaBasinaKayit)
    {
        if (toplamKayit <= 0)
        {
            return 1;
        }

        return (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);
    }
}
