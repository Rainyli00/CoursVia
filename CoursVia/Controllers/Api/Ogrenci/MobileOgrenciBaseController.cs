using CoursVia.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoursVia.Controllers.Api.Ogrenci;

// Öğrenci mobil controllerlarının ortak base class'ı.
// Kullanıcı id alma ve sayfalama yardımcıları burada tutulur.
public abstract class MobileOgrenciBaseController : ControllerBase
{
    protected readonly AppDbContext _context;

    protected MobileOgrenciBaseController(AppDbContext context)
    {
        _context = context;
    }

    // JWT token içinden giriş yapan öğrencinin KullaniciId değerini alır.
    protected int KullaniciIdGetir()
    {
        string? kullaniciIdDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(kullaniciIdDegeri, out int kullaniciId))
        {
            throw new UnauthorizedAccessException("Geçersiz kullanıcı bilgisi.");
        }

        return kullaniciId;
    }

    // Sayfa değerini normalize eder.
    protected static int SayfaNormalizeEt(int sayfa)
    {
        return sayfa < 1
            ? 1
            : sayfa;
    }

    // Sayfa başına kayıt değerini normalize eder.
    protected static int SayfaBasinaKayitNormalizeEt(int sayfaBasinaKayit)
    {
        if (sayfaBasinaKayit < 1)
        {
            return 10;
        }

        return Math.Min(sayfaBasinaKayit, 50);
    }

    // Toplam sayfa sayısını hesaplar.
    protected static int ToplamSayfaHesapla(int toplamKayit, int sayfaBasinaKayit)
    {
        if (toplamKayit <= 0)
        {
            return 1;
        }

        return (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);
    }
}