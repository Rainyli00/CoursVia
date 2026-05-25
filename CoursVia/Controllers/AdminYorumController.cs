using CoursVia.Data;
using CoursVia.Models;
using CoursVia.Services;
using CoursVia.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers;

[Authorize(Roles = "Admin")]
public class AdminYorumController : Controller
{
    private readonly AppDbContext _context;
    private readonly AdminLogService _adminLogService;

    public AdminYorumController(AppDbContext context, AdminLogService adminLogService)
    {
        _context = context;
        _adminLogService = adminLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Yorumlar(
        string? arama,
        int? puan = null,
        string siralama = "yeni",
        int sayfa = 1)
    {
        const int sayfaBasinaKayit = 12;

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        siralama = string.IsNullOrWhiteSpace(siralama)
            ? "yeni"
            : siralama.Trim().ToLower();

        if (siralama != "yeni" && siralama != "eski")
        {
            siralama = "yeni";
        }

        if (puan.HasValue && (puan.Value < 1 || puan.Value > 5))
        {
            puan = null;
        }

        if (sayfa < 1)
        {
            sayfa = 1;
        }

        var query = _context.KursDegerlendirmeleri
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.YorumMetni != null && x.YorumMetni.Contains(arama) ||
                x.Kurs.KursAdi.Contains(arama) ||
                x.Kullanici.Ad.Contains(arama) ||
                x.Kullanici.Soyad.Contains(arama) ||
                x.Kullanici.Eposta.Contains(arama) ||
                x.Kurs.Egitmen.Ad.Contains(arama) ||
                x.Kurs.Egitmen.Soyad.Contains(arama) ||
                x.Kurs.Egitmen.Eposta.Contains(arama));
        }

        if (puan.HasValue)
        {
            query = query.Where(x => x.Puan == puan.Value);
        }

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

        query = siralama == "eski"
            ? query.OrderBy(x => x.DegerlendirmeTarihi)
            : query.OrderByDescending(x => x.DegerlendirmeTarihi);

        var yorumlar = await query
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new AdminYorumListeItemViewModel
            {
                DegerlendirmeId = x.DegerlendirmeId,

                KursId = x.KursId,
                KursAdi = x.Kurs.KursAdi,

                EgitmenAdSoyad = x.Kurs.Egitmen.Ad + " " + x.Kurs.Egitmen.Soyad,
                EgitmenEposta = x.Kurs.Egitmen.Eposta,

                OgrenciAdSoyad = x.Kullanici.Ad + " " + x.Kullanici.Soyad,
                OgrenciEposta = x.Kullanici.Eposta,

                Puan = x.Puan,
                YorumMetni = x.YorumMetni,
                DegerlendirmeTarihi = x.DegerlendirmeTarihi
            })
            .ToListAsync();

        double ortalamaPuan = await _context.KursDegerlendirmeleri
            .AsNoTracking()
            .AverageAsync(x => (double?)x.Puan) ?? 0;

        var bugun = DateTime.Today;

        var model = new AdminYorumYonetimiViewModel
        {
            Arama = arama,
            Puan = puan,
            Siralama = siralama,

            Yorumlar = yorumlar,

            ToplamYorumSayisi = await _context.KursDegerlendirmeleri
                .AsNoTracking()
                .CountAsync(),

            BugunkuYorumSayisi = await _context.KursDegerlendirmeleri
                .AsNoTracking()
                .CountAsync(x => x.DegerlendirmeTarihi >= bugun),

            OrtalamaPuan = Math.Round(ortalamaPuan, 1),

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            ToplamSayfa = toplamSayfa,
            SayfaBasinaKayit = sayfaBasinaKayit
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var degerlendirme = await _context.KursDegerlendirmeleri
            .Include(x => x.Kurs)
            .Include(x => x.Kullanici)
            .FirstOrDefaultAsync(x => x.DegerlendirmeId == id);

        if (degerlendirme == null)
        {
            TempData["AdminHata"] = "Yorum bulunamadı.";
            return RedirectToAction(nameof(Yorumlar));
        }

        string kursAdi = degerlendirme.Kurs.KursAdi;
        string ogrenciAdSoyad = $"{degerlendirme.Kullanici.Ad} {degerlendirme.Kullanici.Soyad}".Trim();

        _context.KursDegerlendirmeleri.Remove(degerlendirme);

        await _adminLogService.KaydetAsync(
            adminId,
            AdminLogService.YorumIslemleri,
            $"{ogrenciAdSoyad} adlı öğrencinin {kursAdi} kursundaki yorumu silindi.");

        await _context.SaveChangesAsync();

        TempData["AdminBasari"] = "Yorum başarıyla silindi.";

        return RedirectToAction(nameof(Yorumlar));
    }

}
