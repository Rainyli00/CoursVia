using CoursVia.Data;
using CoursVia.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        string adminAdSoyad = await _context.Kullanicilar
            .AsNoTracking()
            .Where(x => x.KullaniciId == adminId)
            .Select(x => x.Ad + " " + x.Soyad)
            .FirstOrDefaultAsync() ?? string.Empty;

        var model = new AdminDashboardViewModel
        {
            AdminAdSoyad = adminAdSoyad.Trim(),

            ToplamKullaniciSayisi = await _context.Kullanicilar
                .AsNoTracking()
                .CountAsync(),

            OnlineKullaniciSayisi = await _context.Kullanicilar
                .AsNoTracking()
                .CountAsync(x => x.OnlineMi),

            OnayBekleyenEgitmenSayisi = await _context.EgitmenProfilleri
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 4),

            OnayBekleyenKursSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 4),

            YayindakiKursSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 5),

            ReddedilenKursSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 6),

            SonIslemler = await _context.AdminLoglari
                .AsNoTracking()
                .Include(x => x.Admin)
                .Include(x => x.IslemTipi)
                .OrderByDescending(x => x.IslemTarihi)
                .Take(5)
                .Select(x => new AdminSonIslemViewModel
                {
                    AdminAdSoyad = x.Admin != null
                        ? x.Admin.Ad + " " + x.Admin.Soyad
                        : "Sistem / Kullanıcı",
                    IslemTipi = x.IslemTipi.IslemTipAdi,
                    Aciklama = x.Aciklama,
                    IslemTarihi = x.IslemTarihi
                })
                .ToListAsync()
        };

        return View(model);
    }
}
