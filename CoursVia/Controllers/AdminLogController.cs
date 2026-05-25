using CoursVia.Data;
using CoursVia.Services;
using CoursVia.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers;

[Authorize(Roles = "Admin")]
public class AdminLogController : Controller
{
    private readonly AppDbContext _context;

    public AdminLogController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Loglar(
     string? arama,
     string kategori = "tum",
     string siralama = "yeni",
     DateTime? baslangicTarihi = null,
     DateTime? bitisTarihi = null,
     int sayfa = 1)
    {
        const int sayfaBasinaKayit = 12;

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        kategori = string.IsNullOrWhiteSpace(kategori)
            ? "tum"
            : kategori.Trim().ToLower();

        siralama = string.IsNullOrWhiteSpace(siralama)
            ? "yeni"
            : siralama.Trim().ToLower();

        if (kategori != "tum" &&
            kategori != "kullanici" &&
            kategori != "egitmen-basvuru" &&
            kategori != "kurs-onay" &&
            kategori != "kurs" &&
            kategori != "yorum" &&
            kategori != "kategori" &&
            kategori != "sistem")
        {
            kategori = "tum";
        }

        if (siralama != "yeni" && siralama != "eski")
        {
            siralama = "yeni";
        }

        if (sayfa < 1)
        {
            sayfa = 1;
        }

        var query = _context.AdminLoglari
            .AsNoTracking()
            .Include(x => x.Admin)
            .Include(x => x.IslemTipi)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                (x.Aciklama != null && x.Aciklama.Contains(arama)) ||
                (x.IpAdresi != null && x.IpAdresi.Contains(arama)) ||
                (x.Admin != null && (
                    x.Admin.Ad.Contains(arama) ||
                    x.Admin.Soyad.Contains(arama) ||
                    x.Admin.Eposta.Contains(arama))) ||
                x.IslemTipi.IslemTipAdi.Contains(arama));
        }

        query = kategori switch
        {
            "kullanici" => query.Where(x =>
                x.IslemTipi.IslemTipAdi == AdminLogService.KullaniciIslemleri),

            "egitmen-basvuru" => query.Where(x =>
                x.IslemTipi.IslemTipAdi == AdminLogService.EgitmenBasvurulari),

            "kurs-onay" => query.Where(x =>
                x.IslemTipi.IslemTipAdi == AdminLogService.KursOnaylari),

            "kurs" => query.Where(x =>
                x.IslemTipi.IslemTipAdi == AdminLogService.KursIslemleri),

            "yorum" => query.Where(x =>
                x.IslemTipi.IslemTipAdi == AdminLogService.YorumIslemleri),

            "kategori" => query.Where(x =>
                x.IslemTipi.IslemTipAdi == AdminLogService.KategoriIslemleri),

            "sistem" => query.Where(x =>
                x.IslemTipi.IslemTipAdi == AdminLogService.SistemKullanici),

            _ => query
        };

        if (baslangicTarihi.HasValue)
        {
            DateTime baslangic = baslangicTarihi.Value.Date;
            query = query.Where(x => x.IslemTarihi >= baslangic);
        }

        if (bitisTarihi.HasValue)
        {
            DateTime bitis = bitisTarihi.Value.Date.AddDays(1);
            query = query.Where(x => x.IslemTarihi < bitis);
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

        var siraliQuery = siralama == "eski"
            ? query
                .OrderBy(x => x.IslemTarihi)
                .ThenBy(x => x.AdminLogId)
            : query
                .OrderByDescending(x => x.IslemTarihi)
                .ThenByDescending(x => x.AdminLogId);

        var loglar = await siraliQuery
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new AdminLogListeItemViewModel
            {
                AdminLogId = x.AdminLogId,
                AdminAdSoyad = x.Admin != null
                    ? x.Admin.Ad + " " + x.Admin.Soyad
                    : "Sistem / Kullanıcı",
                AdminEposta = x.Admin != null
                    ? x.Admin.Eposta
                    : "-",
                IslemTipiAdi = x.IslemTipi.IslemTipAdi,
                Aciklama = x.Aciklama,
                IpAdresi = x.IpAdresi,
                IslemTarihi = x.IslemTarihi
            })
            .ToListAsync();

        DateTime bugun = DateTime.Today;
        DateTime yarin = bugun.AddDays(1);

        var model = new AdminLogViewModel
        {
            Arama = arama,
            Kategori = kategori,
            Siralama = siralama,
            BaslangicTarihi = baslangicTarihi,
            BitisTarihi = bitisTarihi,

            Loglar = loglar,

            ToplamLogSayisi = await _context.AdminLoglari
                .AsNoTracking()
                .CountAsync(),

            BugunkuLogSayisi = await _context.AdminLoglari
                .AsNoTracking()
                .CountAsync(x => x.IslemTarihi >= bugun && x.IslemTarihi < yarin),

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            ToplamSayfa = toplamSayfa,
            SayfaBasinaKayit = sayfaBasinaKayit
        };

        return View(model);
    }
}

