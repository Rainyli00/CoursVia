using CoursVia.Data;
using CoursVia.Models;
using CoursVia.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers;

[Authorize(Roles = "Öğrenci,Admin")]
public class OgrenciEgitmenBasvuruController : Controller
{
    private readonly AppDbContext _context;
    private readonly AdminLogService _adminLogService;

    public OgrenciEgitmenBasvuruController(AppDbContext context, AdminLogService adminLogService)
    {
        _context = context;
        _adminLogService = adminLogService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Basvuru(
        string uzmanlikAlani,
        string biyografi,
        int? deneyimYili,
        string? websiteUrl,
        List<int> seciliBransIdleri)
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        uzmanlikAlani = string.IsNullOrWhiteSpace(uzmanlikAlani)
            ? string.Empty
            : uzmanlikAlani.Trim();

        biyografi = string.IsNullOrWhiteSpace(biyografi)
            ? string.Empty
            : biyografi.Trim();

        websiteUrl = string.IsNullOrWhiteSpace(websiteUrl)
            ? null
            : websiteUrl.Trim();

        seciliBransIdleri = seciliBransIdleri
            .Distinct()
            .Where(x => x > 0)
            .ToList();

        if (string.IsNullOrWhiteSpace(uzmanlikAlani))
        {
            TempData["OgrenciHata"] = "Uzmanlık alanı zorunludur.";
            return RedirectToAction("Index", "ProfilAyarlari");
        }

        if (uzmanlikAlani.Length > 150)
        {
            TempData["OgrenciHata"] = "Uzmanlık alanı en fazla 150 karakter olabilir.";
            return RedirectToAction("Index", "ProfilAyarlari");
        }

        if (string.IsNullOrWhiteSpace(biyografi))
        {
            TempData["OgrenciHata"] = "Biyografi zorunludur.";
            return RedirectToAction("Index", "ProfilAyarlari");
        }

     

        if (deneyimYili.HasValue &&
            (deneyimYili.Value < 0 || deneyimYili.Value > 60))
        {
            TempData["OgrenciHata"] = "Deneyim yılı 0 ile 60 arasında olmalıdır.";
            return RedirectToAction("Index", "ProfilAyarlari");
        }

        if (!string.IsNullOrWhiteSpace(websiteUrl) && websiteUrl.Length > 250)
        {
            TempData["OgrenciHata"] = "Web site adresi en fazla 250 karakter olabilir.";
            return RedirectToAction("Index", "ProfilAyarlari");
        }

        if (!seciliBransIdleri.Any())
        {
            TempData["OgrenciHata"] = "En az bir branş seçmelisiniz.";
            return RedirectToAction("Index", "ProfilAyarlari");
        }

        var kategoriIdleri = await _context.Kategoriler
            .AsNoTracking()
            .Select(x => x.KategoriId)
            .ToListAsync();

        bool gecersizKategoriVar = seciliBransIdleri
            .Any(x => !kategoriIdleri.Contains(x));

        if (gecersizKategoriVar)
        {
            TempData["OgrenciHata"] = "Geçersiz branş seçimi yapıldı.";
            return RedirectToAction("Index", "ProfilAyarlari");
        }

        var mevcutProfil = await _context.EgitmenProfilleri
            .Include(x => x.EgitmenBranslari)
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        var kullanici = await _context.Kullanicilar
            .AsNoTracking()
            .Where(x => x.KullaniciId == kullaniciId)
            .Select(x => new
            {
                x.Ad,
                x.Soyad,
                x.Eposta
            })
            .FirstOrDefaultAsync();

        string kullaniciAdSoyad = kullanici == null
            ? $"Kullanıcı #{kullaniciId}"
            : $"{kullanici.Ad} {kullanici.Soyad}".Trim();

        string kullaniciEposta = kullanici?.Eposta ?? "-";
        bool yeniBasvuruMu = mevcutProfil == null;

        if (mevcutProfil != null && mevcutProfil.DurumId == 4)
        {
            TempData["OgrenciHata"] = "Zaten onay bekleyen bir eğitmen başvurunuz var.";
            return RedirectToAction("Index", "ProfilAyarlari");
        }

        if (mevcutProfil != null && mevcutProfil.DurumId == 8)
        {
            TempData["OgrenciHata"] = "Eğitmen başvurunuz zaten onaylanmış.";
            return RedirectToAction("Index", "ProfilAyarlari");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (mevcutProfil == null)
            {
                mevcutProfil = new EgitmenProfili
                {
                    KullaniciId = kullaniciId,
                    DurumId = 4,
                    UzmanlikAlani = uzmanlikAlani,
                    Biyografi = biyografi,
                    DeneyimYili = deneyimYili,
                    WebsiteUrl = websiteUrl
                };

                _context.EgitmenProfilleri.Add(mevcutProfil);

                await _context.SaveChangesAsync();
            }
            else
            {
                mevcutProfil.DurumId = 4;
                mevcutProfil.UzmanlikAlani = uzmanlikAlani;
                mevcutProfil.Biyografi = biyografi;
                mevcutProfil.DeneyimYili = deneyimYili;
                mevcutProfil.WebsiteUrl = websiteUrl;

                _context.EgitmenBranslari.RemoveRange(mevcutProfil.EgitmenBranslari);
            }

            foreach (int kategoriId in seciliBransIdleri)
            {
                _context.EgitmenBranslari.Add(new EgitmenBransi
                {
                    EgitmenProfilId = mevcutProfil.EgitmenProfilId,
                    KategoriId = kategoriId
                });
            }

            await _adminLogService.KaydetAsync(
                null,
                AdminLogService.EgitmenBasvurulari,
                yeniBasvuruMu
                    ? $"{kullaniciAdSoyad} eğitmen başvurusu oluşturdu. E-posta: {kullaniciEposta}"
                    : $"{kullaniciAdSoyad} eğitmen başvurusunu güncelledi. E-posta: {kullaniciEposta}");

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["OgrenciBasari"] = "Eğitmen başvurunuz başarıyla alındı. Admin onayından sonra eğitmen paneline erişebilirsiniz.";

            return RedirectToAction("Index", "ProfilAyarlari");
        }
        catch
        {
            await transaction.RollbackAsync();

            TempData["OgrenciHata"] = "Başvuru kaydedilirken beklenmeyen bir hata oluştu.";
            return RedirectToAction("Index", "ProfilAyarlari");
        }
    }
}
