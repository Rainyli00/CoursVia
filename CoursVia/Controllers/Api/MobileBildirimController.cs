using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Ortak;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers.Api;

// Mobil uygulama için ortak bildirim API'si.
// Öğrenci, eğitmen ve admin kullanıcıları aynı bildirim endpointlerini kullanabilir.
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
    // durum parametresi ile tüm, okunmamış veya okunmuş bildirimler filtrelenebilir.
    [HttpGet]
    public async Task<ActionResult<MobileBildirimlerResponse>> Bildirimler(
        [FromQuery] string? durum = "tum",
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBasinaKayit = 10)
    {
        // JWT token içinden giriş yapan kullanıcının Id değeri alınır.
        int kullaniciId = KullaniciIdGetir();

        // Durum parametresi boş gelirse varsayılan olarak tüm bildirimler listelenir.
        durum = string.IsNullOrWhiteSpace(durum)
            ? "tum"
            : durum.Trim().ToLower();

        // Sadece belirlenen filtre değerlerine izin verilir.
        if (durum != "tum" && durum != "okunmamis" && durum != "okunmus")
        {
            return BadRequest(new MobileBildirimlerResponse
            {
                Basarili = false,
                Mesaj = "Geçersiz durum filtresi. Durum değeri tum, okunmamis veya okunmus olabilir.",
                Durum = durum
            });
        }

        // Sayfa ve sayfa başına kayıt değerleri güvenli aralığa çekilir.
        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        // Sadece giriş yapan kullanıcıya ait bildirimler alınır.
        var query = _context.Bildirimler
            .AsNoTracking()
            .Where(x => x.KullaniciId == kullaniciId);

        // Okunmamış bildirim filtresi uygulanır.
        if (durum == "okunmamis")
        {
            query = query.Where(x => !x.OkunduMu);
        }
        // Okunmuş bildirim filtresi uygulanır.
        else if (durum == "okunmus")
        {
            query = query.Where(x => x.OkunduMu);
        }

        // Filtrelenmiş toplam bildirim sayısı hesaplanır.
        int toplamKayit = await query.CountAsync();

        // Toplam sayfa sayısı hesaplanır.
        int toplamSayfa = ToplamSayfaHesapla(
            toplamKayit,
            sayfaBasinaKayit
        );

        // İstenen sayfa toplam sayfadan büyükse son sayfaya çekilir.
        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        // Header badge gibi alanlarda gösterilecek okunmamış bildirim sayısı hesaplanır.
        int okunmamisBildirimSayisi = await _context.Bildirimler
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciId == kullaniciId &&
                !x.OkunduMu);

        // Bildirimler tarihe göre yeniden eskiye sıralanır ve sayfalama uygulanır.
        var bildirimler = await query
            .OrderByDescending(x => x.OlusturmaTarihi)
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new MobileBildirimItemResponse
            {
                BildirimId = x.BildirimId,

                // Bildirim tipinin Id değeri mobil tarafa gönderilir.
                BildirimTipId = x.BildirimTipId,

                // Bildirim tipinin adı mobil tarafta gösterim için gönderilir.
                BildirimTipAdi = x.BildirimTipi.BildirimTipAdi,

                Baslik = x.Baslik,
                Mesaj = x.Mesaj,

                OkunduMu = x.OkunduMu,
                OlusturmaTarihi = x.OlusturmaTarihi
            })
            .ToListAsync();

        // Mobil uygulamaya bildirim listesi ve sayfalama bilgileri döndürülür.
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

    // Mobil uygulamada hızlı bildirim özeti almak için kullanılır.
    // Genelde header badge veya bildirim ikonundaki sayı için kullanılır.
    [HttpGet("ozet")]
    public async Task<ActionResult<MobileBildirimOzetResponse>> Ozet()
    {
        int kullaniciId = KullaniciIdGetir();

        // Kullanıcının toplam bildirim sayısı alınır.
        int toplamBildirimSayisi = await _context.Bildirimler
            .AsNoTracking()
            .CountAsync(x => x.KullaniciId == kullaniciId);

        // Kullanıcının okunmamış bildirim sayısı alınır.
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

    // Kullanıcının seçtiği tek bir bildirimi okundu olarak işaretler.
    [HttpPost("{bildirimId:int}/okundu")]
    public async Task<ActionResult<MobileBildirimIslemResponse>> OkunduYap(
        int bildirimId)
    {
        int kullaniciId = KullaniciIdGetir();

        // Bildirimin hem var olduğu hem de giriş yapan kullanıcıya ait olduğu kontrol edilir.
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

        // Bildirim okunmamışsa okundu yapılır.
        // Zaten okunduysa gereksiz güncelleme yapılmaz.
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

    // Kullanıcının seçtiği tek bir bildirimi okunmamış olarak işaretler.
    [HttpPost("{bildirimId:int}/okunmadi")]
    public async Task<ActionResult<MobileBildirimIslemResponse>> OkunmadiYap(
        int bildirimId)
    {
        int kullaniciId = KullaniciIdGetir();

        // Bildirimin giriş yapan kullanıcıya ait olup olmadığı kontrol edilir.
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

        // Bildirim okunduysa okunmamış hale getirilir.
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

        // Kullanıcıya ait okunmamış bildirimler alınır.
        var okunmamisBildirimler = await _context.Bildirimler
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                !x.OkunduMu)
            .ToListAsync();

        // Her bildirim okundu olarak işaretlenir.
        foreach (var bildirim in okunmamisBildirimler)
        {
            bildirim.OkunduMu = true;
        }


        // Değişiklikler veritabanına kaydedilir.
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
    // Başka kullanıcıya ait bildirim silinemez.
    [HttpDelete("{bildirimId:int}")]
    public async Task<ActionResult<MobileBildirimIslemResponse>> Sil(
        int bildirimId)
    {
        int kullaniciId = KullaniciIdGetir();

        // Silinmek istenen bildirimin kullanıcıya ait olup olmadığı kontrol edilir.
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

        // Bildirim veritabanından kalıcı olarak silinir.
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
    // 1'den küçük değer gelirse 1 yapılır.
    private static int SayfaNormalizeEt(int sayfa)
    {
        return sayfa < 1 ? 1 : sayfa;
    }

    // Sayfa başına kayıt değerini normalize eder.
    // Çok düşük değerler 10'a, çok yüksek değerler 50'ye çekilir.
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

    // Toplam kayıt ve sayfa başına kayıt sayısına göre toplam sayfa sayısını hesaplar.
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