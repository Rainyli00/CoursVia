using CoursVia.Data;
using CoursVia.Services;
using CoursVia.ViewModels.Bildirim;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers;

[Authorize]
public class BildirimController : Controller
{
    private readonly AppDbContext _context;
    private readonly BildirimService _bildirimService;

    public BildirimController(AppDbContext context, BildirimService bildirimService)
    {
        _context = context;
        _bildirimService = bildirimService;
    }

    [HttpGet]
    public async Task<IActionResult> Bildirimler(string durum = "tum", int sayfa = 1)
    {
        const int sayfaBasinaKayit = 10;

        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        durum = string.IsNullOrWhiteSpace(durum)
            ? "tum"
            : durum.Trim().ToLower();

        if (durum != "tum" &&
            durum != "okunmamis" &&
            durum != "okunmus")
        {
            durum = "tum";
        }

        if (sayfa < 1)
        {
            sayfa = 1;
        }

        var query = _context.Bildirimler
            .AsNoTracking()
            .Include(x => x.BildirimTipi)
            .Where(x => x.KullaniciId == kullaniciId)
            .AsQueryable();

        query = durum switch
        {
            "okunmamis" => query.Where(x => !x.OkunduMu),
            "okunmus" => query.Where(x => x.OkunduMu),
            _ => query
        };

        int toplamKayit = await query.CountAsync();

        int toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);

        if (toplamSayfa < 1)
        {
            toplamSayfa = 1;
        }

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        var bildirimler = await query
            .OrderBy(x => x.OkunduMu)
            .ThenByDescending(x => x.OlusturmaTarihi)
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new BildirimListeItemViewModel
            {
                BildirimId = x.BildirimId,
                BildirimTipiAdi = x.BildirimTipi.BildirimTipAdi,
                Baslik = x.Baslik,
                Mesaj = x.Mesaj,
                OlusturmaTarihi = x.OlusturmaTarihi,
                OkunduMu = x.OkunduMu
            })
            .ToListAsync();

        var model = new BildirimlerViewModel
        {
            Durum = durum,
            Bildirimler = bildirimler,

            ToplamBildirimSayisi = await _context.Bildirimler
                .AsNoTracking()
                .CountAsync(x => x.KullaniciId == kullaniciId),

            OkunmamisBildirimSayisi = await _context.Bildirimler
                .AsNoTracking()
                .CountAsync(x => x.KullaniciId == kullaniciId && !x.OkunduMu),

            OkunmusBildirimSayisi = await _context.Bildirimler
                .AsNoTracking()
                .CountAsync(x => x.KullaniciId == kullaniciId && x.OkunduMu),

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            ToplamSayfa = toplamSayfa,
            SayfaBasinaKayit = sayfaBasinaKayit
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetSonBildirimler()
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var querySonuclar = await _context.Bildirimler
            .AsNoTracking()
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                !x.OkunduMu)
            .OrderByDescending(x => x.OlusturmaTarihi)
            .Take(5)
            .Select(x => new
            {
                x.BildirimId,
                x.Baslik,
                x.Mesaj,
                x.OkunduMu,
                x.OlusturmaTarihi
            })
            .ToListAsync();

        var bildirimler = querySonuclar.Select(x => new 
            {
                bildirimId = x.BildirimId,
                baslik = x.Baslik,
                mesaj = x.Mesaj,
                okunduMu = x.OkunduMu,
                olusturmaTarihi = x.OlusturmaTarihi.ToString("dd.MM.yyyy HH:mm")
            }).ToList();

        var okunmamisSayisi = await _context.Bildirimler
            .AsNoTracking()
            .CountAsync(x => x.KullaniciId == kullaniciId && !x.OkunduMu);

        return Json(new { bildirimler, okunmamisSayisi });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OkunduYap(int id, string durum = "tum", int sayfa = 1)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        bool sonuc = await _bildirimService.OkunduYapAsync(kullaniciId, id);

        if (sonuc)
        {
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Bildirimler), new
        {
            durum,
            sayfa
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OkunmadiYap(int id, string durum = "tum", int sayfa = 1)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        bool sonuc = await _bildirimService.OkunmadiYapAsync(kullaniciId, id);

        if (sonuc)
        {
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Bildirimler), new
        {
            durum,
            sayfa
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TumunuOkunduYap(string durum = "tum")
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await _bildirimService.TumunuOkunduYapAsync(kullaniciId);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Bildirimler), new
        {
            durum
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id, string durum = "tum", int sayfa = 1)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var bildirim = await _context.Bildirimler
            .FirstOrDefaultAsync(x =>
                x.BildirimId == id &&
                x.KullaniciId == kullaniciId);

        if (bildirim != null)
        {
            _context.Bildirimler.Remove(bildirim);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Bildirimler), new
        {
            durum,
            sayfa
        });
    }
}
