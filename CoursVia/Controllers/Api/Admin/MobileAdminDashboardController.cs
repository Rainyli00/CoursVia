using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Admin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Admin;

[ApiController]
[Route("api/mobile/admin/dashboard")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Admin"
)]
public class MobileAdminDashboardController : MobileAdminBaseController
{
    public MobileAdminDashboardController(AppDbContext context) : base(context)
    {
    }

    // Admin mobil dashboard özet bilgilerini ve son admin loglarını döndürür.
    // GET /api/mobile/admin/dashboard
    [HttpGet]
    public async Task<ActionResult<MobileAdminDashboardResponse>> Dashboard()
    {
        int adminId = KullaniciIdGetir();

        // Dashboard üst kartları için temel sayaçlar ayrı ayrı hesaplanır.
        int toplamKullaniciSayisi = await _context.Kullanicilar
            .AsNoTracking()
            .CountAsync();

        int onlineKullaniciSayisi = await _context.Kullanicilar
            .AsNoTracking()
            .CountAsync(x => x.OnlineMi);

        int bekleyenEgitmenBasvuruSayisi = await _context.EgitmenProfilleri
            .AsNoTracking()
            .CountAsync(x => x.DurumId == 4);

        int bekleyenKursOnaySayisi = await _context.Kurslar
            .AsNoTracking()
            .CountAsync(x => x.DurumId == 4);

        // Bildirim sayısı sadece giriş yapan admin kullanıcısı için alınır.
        int okunmamisBildirimSayisi = await _context.Bildirimler
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciId == adminId &&
                !x.OkunduMu);

        // Ana ekranda göstermek için en güncel 3 admin logu ham veri olarak çekilir.
        var sonLogHamListe = await _context.AdminLoglari
            .AsNoTracking()
            .OrderByDescending(x => x.IslemTarihi)
            .Take(3)
            .Select(x => new
            {
                x.AdminLogId,

                AdminAd = x.Admin == null ? "" : x.Admin.Ad,
                AdminSoyad = x.Admin == null ? "" : x.Admin.Soyad,

                IslemTipi = x.IslemTipi == null
                    ? ""
                    : x.IslemTipi.IslemTipAdi,

                Aciklama = x.Aciklama ?? string.Empty,
                x.IpAdresi,
                x.IslemTarihi
            })
            .ToListAsync();

        // Null admin veya işlem tipi değerleri mobil ekranda okunabilir metinlere çevrilir.
        var sonLoglar = sonLogHamListe
            .Select(x => new MobileAdminLogItemResponse
            {
                AdminLogId = x.AdminLogId,

                AdminAdSoyad = string.IsNullOrWhiteSpace($"{x.AdminAd} {x.AdminSoyad}".Trim())
                    ? "Bilinmeyen Admin"
                    : $"{x.AdminAd} {x.AdminSoyad}".Trim(),

                IslemTipi = string.IsNullOrWhiteSpace(x.IslemTipi)
                    ? "Bilinmeyen İşlem"
                    : x.IslemTipi,

                Aciklama = x.Aciklama,
                IpAdresi = x.IpAdresi,
                IslemTarihi = x.IslemTarihi
            })
            .ToList();

        return Ok(new MobileAdminDashboardResponse
        {
            Basarili = true,
            Mesaj = "Admin dashboard bilgileri getirildi.",

            ToplamKullaniciSayisi = toplamKullaniciSayisi,
            OnlineKullaniciSayisi = onlineKullaniciSayisi,
            BekleyenEgitmenBasvuruSayisi = bekleyenEgitmenBasvuruSayisi,
            BekleyenKursOnaySayisi = bekleyenKursOnaySayisi,
            OkunmamisBildirimSayisi = okunmamisBildirimSayisi,

            SonLoglar = sonLoglar
        });
    }
}
