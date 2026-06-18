using CoursVia.Data;
using CoursVia.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers;

// Bu controller sadece Admin rolüne sahip kullanıcılar tarafından erişilebilir.
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _context;

    
    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    // Admin panelinin ana dashboard ekranını hazırlar.
    public async Task<IActionResult> Index()
    {
        // Giriş yapan admin kullanıcının Id bilgisi claim üzerinden alınır.
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Dashboard üzerinde adminin ad soyad bilgisini göstermek için kullanıcı bilgisi çekilir.
        string adminAdSoyad = await _context.Kullanicilar
            .AsNoTracking()
            .Where(x => x.KullaniciId == adminId)
            .Select(x => x.Ad + " " + x.Soyad)
            .FirstOrDefaultAsync() ?? string.Empty;

        // Admin dashboard ekranında gösterilecek özet istatistikler ViewModel içerisine doldurulur.
        var model = new AdminDashboardViewModel
        {
            AdminAdSoyad = adminAdSoyad.Trim(),

          
            ToplamKullaniciSayisi = await _context.Kullanicilar
                .AsNoTracking()
                .CountAsync(),

            
            OnlineKullaniciSayisi = await _context.Kullanicilar
                .AsNoTracking()
                .CountAsync(x => x.OnlineMi),

            // Admin onayı bekleyen eğitmen başvuruları.
            // DurumId = 4: Onay Bekliyor
            OnayBekleyenEgitmenSayisi = await _context.EgitmenProfilleri
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 4),

            // Admin tarafından incelenmeyi bekleyen kurslar.
            // DurumId = 4: Onay Bekliyor
            OnayBekleyenKursSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 4),

          
            YayindakiKursSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 5),

            ReddedilenKursSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 6),

            // Admin dashboard üzerinde gösterilecek son 5 sistem/admin işlemi.
            SonIslemler = await _context.AdminLoglari
                .AsNoTracking()
                .Include(x => x.Admin)
                .Include(x => x.IslemTipi)
                .OrderByDescending(x => x.IslemTarihi)
                .Take(5)
                .Select(x => new AdminSonIslemViewModel
                {
                    // İşlem admin tarafından yapılmışsa admin adı, sistem işlemiyse varsayılan metin gösterilir.
                    AdminAdSoyad = x.Admin != null
                        ? x.Admin.Ad + " " + x.Admin.Soyad
                        : "Sistem / Kullanıcı",

                    // Log kaydının işlem tipi ve açıklaması dashboard için hazırlanır.
                    IslemTipi = x.IslemTipi.IslemTipAdi,
                    Aciklama = x.Aciklama,
                    IslemTarihi = x.IslemTarihi
                })
                .ToListAsync()
        };

       
        return View(model);
    }
}