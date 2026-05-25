using CoursVia.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoursVia.Controllers.Api.Admin;

public abstract class MobileAdminBaseController : ControllerBase
{
    protected readonly AppDbContext _context;

    protected MobileAdminBaseController(AppDbContext context)
    {
        _context = context;
    }

    protected int KullaniciIdGetir()
    {
        string? kullaniciIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (int.TryParse(kullaniciIdText, out int kullaniciId))
        {
            return kullaniciId;
        }

        return 0;
    }

    protected int SayfaNormalizeEt(int sayfa)
    {
        return sayfa < 1 ? 1 : sayfa;
    }

    protected int SayfaBasinaKayitNormalizeEt(int sayfaBasinaKayit)
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

    protected int ToplamSayfaHesapla(int toplamKayit, int sayfaBasinaKayit)
    {
        if (toplamKayit <= 0)
        {
            return 1;
        }

        return (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);
    }
}