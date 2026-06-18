using CoursVia.Data;
using CoursVia.Models;
using CoursVia.Services;
using CoursVia.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers;

// Admin panelinde kategori ekleme, düzenleme, silme ve listeleme işlemlerini yönetir.
[Authorize(Roles = "Admin")]
public class AdminKategoriController : Controller
{
    private readonly AppDbContext _context;
    private readonly AdminLogService _adminLogService;

    public AdminKategoriController(AppDbContext context, AdminLogService adminLogService)
    {
        _context = context;
        _adminLogService = adminLogService;
    }

    [HttpGet]
    // Kategorileri arama, filtreleme ve sayfalama ile listeler.
    public async Task<IActionResult> Kategoriler(string? arama, string? durumFiltresi, int sayfa = 1)
    {
        const int sayfaBasinaKayit = 10;

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        if (sayfa < 1)
        {
            sayfa = 1;
        }

        var query = _context.Kategoriler
            .AsNoTracking()
            .Include(x => x.KursKategorileri)
            .Include(x => x.EgitmenBranslari)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x => x.KategoriAdi.Contains(arama));
        }

        // Kategori kullanılıyor mu veya boş mu bilgisine göre filtre uygulanır.
        if (!string.IsNullOrWhiteSpace(durumFiltresi))
        {
            if (durumFiltresi == "kullanilan")
            {
                query = query.Where(x => x.KursKategorileri.Any() || x.EgitmenBranslari.Any());
            }
            else if (durumFiltresi == "bos")
            {
                query = query.Where(x => !x.KursKategorileri.Any() && !x.EgitmenBranslari.Any());
            }
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

        var kategoriler = await query
            .OrderBy(x => x.KategoriAdi)
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new KategoriListeItemViewModel
            {
                KategoriId = x.KategoriId,
                KategoriAdi = x.KategoriAdi,
                KursSayisi = x.KursKategorileri.Count,
                EgitmenSayisi = x.EgitmenBranslari.Count
            })
            .ToListAsync();

        int toplamKategoriSayisi = await _context.Kategoriler
            .AsNoTracking()
            .CountAsync();

        int kullanilanKategoriSayisi = await _context.Kategoriler
            .AsNoTracking()
            .CountAsync(x =>
                x.KursKategorileri.Any() ||
                x.EgitmenBranslari.Any());

        var model = new KategoriYonetimiViewModel
        {
            Arama = arama,
            DurumFiltresi = durumFiltresi,
            Kategoriler = kategoriler,

            ToplamKategoriSayisi = toplamKategoriSayisi,
            KullanilanKategoriSayisi = kullanilanKategoriSayisi,
            BosKategoriSayisi = toplamKategoriSayisi - kullanilanKategoriSayisi,

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            ToplamSayfa = toplamSayfa,
            SayfaBasinaKayit = sayfaBasinaKayit
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Yeni kategori ekler.
    public async Task<IActionResult> Ekle(KategoriKaydetViewModel model)
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        model.KategoriAdi = model.KategoriAdi?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(model.KategoriAdi))
        {
            TempData["AdminHata"] = "Kategori adı zorunludur.";
            return RedirectToAction(nameof(Kategoriler));
        }

        bool kategoriVar = await _context.Kategoriler
            .AsNoTracking()
            .AnyAsync(x => x.KategoriAdi.ToLower() == model.KategoriAdi.ToLower());

        if (kategoriVar)
        {
            TempData["AdminHata"] = "Bu kategori zaten mevcut.";
            return RedirectToAction(nameof(Kategoriler));
        }

        var kategori = new Kategori
        {
            KategoriAdi = model.KategoriAdi
        };

        _context.Kategoriler.Add(kategori);

        // Kategori ekleme işlemi admin loglarına kaydedilir.
        await _adminLogService.KaydetAsync(
            adminId,
            AdminLogService.KategoriIslemleri,
            $"{model.KategoriAdi} adlı kategori eklendi.");

        await _context.SaveChangesAsync();

        TempData["AdminBasari"] = "Kategori başarıyla eklendi.";

        return RedirectToAction(nameof(Kategoriler));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Mevcut kategori adını günceller.
    public async Task<IActionResult> Duzenle(KategoriKaydetViewModel model)
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (!model.KategoriId.HasValue)
        {
            TempData["AdminHata"] = "Kategori bulunamadı.";
            return RedirectToAction(nameof(Kategoriler));
        }

        model.KategoriAdi = model.KategoriAdi?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(model.KategoriAdi))
        {
            TempData["AdminHata"] = "Kategori adı zorunludur.";
            return RedirectToAction(nameof(Kategoriler));
        }

        var kategori = await _context.Kategoriler
            .FirstOrDefaultAsync(x => x.KategoriId == model.KategoriId.Value);

        if (kategori == null)
        {
            TempData["AdminHata"] = "Kategori bulunamadı.";
            return RedirectToAction(nameof(Kategoriler));
        }

        // "Diğer" kategorisi sistem tarafından kullanıldığı için adı değiştirilemez.
        if (kategori.KategoriAdi.Trim().ToLower() == "diğer" &&
            model.KategoriAdi.Trim().ToLower() != "diğer")
        {
            TempData["AdminHata"] = "\"Diğer\" sistem kategorisinin adı değiştirilemez.";
            return RedirectToAction(nameof(Kategoriler));
        }

        bool kategoriVar = await _context.Kategoriler
            .AsNoTracking()
            .AnyAsync(x =>
                x.KategoriId != kategori.KategoriId &&
                x.KategoriAdi.ToLower() == model.KategoriAdi.ToLower());

        if (kategoriVar)
        {
            TempData["AdminHata"] = "Bu kategori adı başka bir kayıt tarafından kullanılıyor.";
            return RedirectToAction(nameof(Kategoriler));
        }

        string eskiKategoriAdi = kategori.KategoriAdi;

        kategori.KategoriAdi = model.KategoriAdi;

        await _adminLogService.KaydetAsync(
            adminId,
            AdminLogService.KategoriIslemleri,
            $"{eskiKategoriAdi} kategorisi {model.KategoriAdi} olarak güncellendi.");

        await _context.SaveChangesAsync();

        TempData["AdminBasari"] = "Kategori başarıyla güncellendi.";

        return RedirectToAction(nameof(Kategoriler));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // Kategoriyi siler; kullanılıyorsa bağlı kayıtları "Diğer" kategorisine taşır.
    public async Task<IActionResult> Sil(int id)
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var kategori = await _context.Kategoriler
            .Include(x => x.KursKategorileri)
            .Include(x => x.EgitmenBranslari)
            .FirstOrDefaultAsync(x => x.KategoriId == id);

        if (kategori == null)
        {
            TempData["AdminHata"] = "Kategori bulunamadı.";
            return RedirectToAction(nameof(Kategoriler));
        }

        if (kategori.KategoriAdi.Trim().ToLower() == "diğer")
        {
            TempData["AdminHata"] = "\"Diğer\" kategorisi sistem kategorisi olduğu için silinemez.";
            return RedirectToAction(nameof(Kategoriler));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            string silinenKategoriAdi = kategori.KategoriAdi;

            bool kullaniliyorMu =
                kategori.KursKategorileri.Any() ||
                kategori.EgitmenBranslari.Any();

            // Kullanılan kategori silinirse kurslar ve eğitmen branşları boşa düşmesin diye "Diğer" kategorisine aktarılır.
            if (kullaniliyorMu)
            {
                var digerKategori = await DigerKategorisiniGetirVeyaOlusturAsync();

                await KursKategorileriniDigereTasiAsync(
                    kategori.KategoriId,
                    digerKategori.KategoriId);

                await EgitmenBranslariniDigereTasiAsync(
                    kategori.KategoriId,
                    digerKategori.KategoriId);
            }

            _context.Kategoriler.Remove(kategori);

            await _adminLogService.KaydetAsync(
                adminId,
                AdminLogService.KategoriIslemleri,
                kullaniliyorMu
                    ? $"{silinenKategoriAdi} kategorisi silindi. Bağlı kurs ve eğitmen branşları Diğer kategorisine taşındı."
                    : $"{silinenKategoriAdi} adlı kategori silindi.");

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["AdminBasari"] = kullaniliyorMu
                ? "Kategori silindi. Bağlı kayıtlar Diğer kategorisine taşındı."
                : "Kategori başarıyla silindi.";

            return RedirectToAction(nameof(Kategoriler));
        }
        catch
        {
            await transaction.RollbackAsync();

            TempData["AdminHata"] = "Kategori silinirken beklenmeyen bir hata oluştu.";
            return RedirectToAction(nameof(Kategoriler));
        }
    }

    // "Diğer" kategorisini getirir; yoksa yeni oluşturur.
    private async Task<Kategori> DigerKategorisiniGetirVeyaOlusturAsync()
    {
        var digerKategori = await _context.Kategoriler
            .FirstOrDefaultAsync(x => x.KategoriAdi == "Diğer");

        if (digerKategori != null)
        {
            return digerKategori;
        }

        digerKategori = new Kategori
        {
            KategoriAdi = "Diğer"
        };

        _context.Kategoriler.Add(digerKategori);

        await _context.SaveChangesAsync();

        return digerKategori;
    }

    // Silinen kategoriye bağlı kurs kategori ilişkilerini "Diğer" kategorisine taşır.
    private async Task KursKategorileriniDigereTasiAsync(int silinecekKategoriId, int digerKategoriId)
    {
        var kursKategorileri = await _context.KursKategorileri
            .Where(x => x.KategoriId == silinecekKategoriId)
            .ToListAsync();

        if (!kursKategorileri.Any())
        {
            return;
        }

        var digerKategoriKursIdleri = await _context.KursKategorileri
            .Where(x => x.KategoriId == digerKategoriId)
            .Select(x => x.KursId)
            .ToListAsync();

        var tasinanKursIdleri = new HashSet<int>(digerKategoriKursIdleri);

        //Kurs zaten “Diğer” kategorisinde varsa sistem eski kategori bağlantısını siliyor. 
        foreach (var kursKategori in kursKategorileri)
        {
            if (tasinanKursIdleri.Contains(kursKategori.KursId))
            {
                _context.KursKategorileri.Remove(kursKategori);
            }
            else
            {// Kursu "Diğer" kategorisine taşı.
                kursKategori.KategoriId = digerKategoriId;
                // Taşınan kurs ID'sini HashSet'e ekle.
                tasinanKursIdleri.Add(kursKategori.KursId);
            }
        }
    }

    // Silinen kategoriye bağlı eğitmen branşlarını "Diğer" kategorisine taşır.
    private async Task EgitmenBranslariniDigereTasiAsync(int silinecekKategoriId, int digerKategoriId)
    {
        var egitmenBranslari = await _context.EgitmenBranslari
            .Where(x => x.KategoriId == silinecekKategoriId)
            .ToListAsync();

        if (!egitmenBranslari.Any())
        {
            return;
        }

        var digerKategoriEgitmenProfilIdleri = await _context.EgitmenBranslari
            .Where(x => x.KategoriId == digerKategoriId)
            .Select(x => x.EgitmenProfilId)
            .ToListAsync();

        var tasinanEgitmenProfilIdleri = new HashSet<int>(digerKategoriEgitmenProfilIdleri);
        // Eğitmen zaten “Diğer” kategorisinde varsa sistem eski kategori bağlantısını siliyor.
        foreach (var egitmenBransi in egitmenBranslari)
        {
            if (tasinanEgitmenProfilIdleri.Contains(egitmenBransi.EgitmenProfilId))
            {
                _context.EgitmenBranslari.Remove(egitmenBransi);
            }
            else
            {
                egitmenBransi.KategoriId = digerKategoriId;
                tasinanEgitmenProfilIdleri.Add(egitmenBransi.EgitmenProfilId);
            }
        }
    }
}