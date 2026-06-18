using CoursVia.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoursVia.Controllers.Api.Admin;

// Mobil admin controllerlarının ortak yardımcılarını tutar.
public abstract class MobileAdminBaseController : ControllerBase
{
    protected readonly AppDbContext _context;

    protected MobileAdminBaseController(AppDbContext context)
    {
        _context = context;
    }

    protected int KullaniciIdGetir()
    {
        // Admin id JWT içindeki NameIdentifier claim'inden okunur.
        string? kullaniciIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(kullaniciIdText, out int kullaniciId))
        {
            return kullaniciId;
        }

        return 0;
    }

    protected int SayfaNormalizeEt(int sayfa)
    {
        // Mobil istekten 0 veya negatif sayfa gelirse ilk sayfa kullanılır.
        return sayfa < 1 ? 1 : sayfa;
    }

    protected int SayfaBasinaKayitNormalizeEt(int sayfaBasinaKayit)
    {
        // Geçersiz değerler varsayılana çekilir, büyük listeler 50 kayıtla sınırlandırılır.
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

    protected int ToplamSayfaHesapla(int toplamKayit, int sayfaBasinaKayit)
    {
        // Kayıt yokken de sayfa değeri 1 tutulur; böylece Skip hesabı negatif olmaz.
        if (toplamKayit <= 0)
        {
            return 1;
        }

        return (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);
    }
}
