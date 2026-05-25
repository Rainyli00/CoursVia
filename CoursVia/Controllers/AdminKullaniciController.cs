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
public class AdminKullaniciController : Controller
{
    private readonly AppDbContext _context;
    private readonly AdminLogService _adminLogService;

    public AdminKullaniciController(AppDbContext context, AdminLogService adminLogService)
    {
        _context = context;
        _adminLogService = adminLogService;
    }

    [HttpGet]
public async Task<IActionResult> Kullanicilar(string? arama, string rol = "tum", string durum = "tum", int sayfa = 1)
{
    const int sayfaBasinaKayit = 10;

    arama = string.IsNullOrWhiteSpace(arama)
        ? null
        : arama.Trim();

    rol = string.IsNullOrWhiteSpace(rol)
        ? "tum"
        : rol.Trim().ToLower();

    durum = string.IsNullOrWhiteSpace(durum)
        ? "tum"
        : durum.Trim().ToLower();

    if (sayfa < 1)
    {
        sayfa = 1;
    }

    var query = _context.Kullanicilar
        .AsNoTracking()
        .AsSplitQuery()
        .Include(x => x.Durum)
        .Include(x => x.KullaniciRolleri)
            .ThenInclude(x => x.Rol)
        .Include(x => x.EgitmenProfili)
            .ThenInclude(x => x!.EgitmenBranslari)
                .ThenInclude(x => x.Kategori)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(arama))
    {
        query = query.Where(x =>
            x.Ad.Contains(arama) ||
            x.Soyad.Contains(arama) ||
            x.Eposta.Contains(arama) ||
            (x.Telefon != null && x.Telefon.Contains(arama)) ||
            (x.SonIpAdresi != null && x.SonIpAdresi.Contains(arama)));
    }

    query = rol switch
    {
        "admin" => query.Where(x => x.KullaniciRolleri.Any(r => r.RolId == 1)),
        "egitmen" => query.Where(x =>
            x.KullaniciRolleri.Any(r => r.RolId == 2) ||
            x.EgitmenProfili != null),
        "ogrenci" => query.Where(x => x.KullaniciRolleri.Any(r => r.RolId == 3)),
        _ => query
    };

    query = durum switch
    {
        "aktif" => query.Where(x => x.DurumId == 1),
        "pasif" => query.Where(x => x.DurumId == 2),
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

    var kullaniciKayitlari = await query
        .OrderByDescending(x => x.KayitTarihi)
        .Skip((sayfa - 1) * sayfaBasinaKayit)
        .Take(sayfaBasinaKayit)
        .ToListAsync();

    var kullanicilar = kullaniciKayitlari
        .Select(x => new KullaniciListeItemViewModel
        {
            KullaniciId = x.KullaniciId,
            AdSoyad = $"{x.Ad} {x.Soyad}".Trim(),
            Eposta = x.Eposta,
            ProfilFotoUrl = x.ProfilFotoUrl,

            DurumId = x.DurumId,
            DurumAdi = x.Durum.DurumAdi,
            OnlineMi = x.OnlineMi,

            SonIpAdresi = x.SonIpAdresi,
            KayitTarihi = x.KayitTarihi,
            SonGirisTarihi = x.SonGirisTarihi,

            Roller = x.KullaniciRolleri
                .Select(r => r.Rol.RolAdi)
                .OrderBy(r => r)
                .ToList(),

            EgitmenMi =
                x.KullaniciRolleri.Any(r => r.RolId == 2) ||
                x.EgitmenProfili != null,

            UzmanlikAlani = x.EgitmenProfili?.UzmanlikAlani,

            Branslar = x.EgitmenProfili == null
                ? new List<string>()
                : x.EgitmenProfili.EgitmenBranslari
                    .Select(b => b.Kategori.KategoriAdi)
                    .OrderBy(b => b)
                    .ToList()
        })
        .ToList();

    var model = new KullaniciYonetimiViewModel
    {
        Arama = arama,
        Rol = rol,
        Durum = durum,

        Kullanicilar = kullanicilar,

        ToplamKullaniciSayisi = await _context.Kullanicilar
            .AsNoTracking()
            .CountAsync(),

        OnlineKullaniciSayisi = await _context.Kullanicilar
            .AsNoTracking()
            .CountAsync(x => x.OnlineMi),

        AktifKullaniciSayisi = await _context.Kullanicilar
            .AsNoTracking()
            .CountAsync(x => x.DurumId == 1),

        PasifKullaniciSayisi = await _context.Kullanicilar
            .AsNoTracking()
            .CountAsync(x => x.DurumId == 2),

        AdminSayisi = await _context.Kullanicilar
            .AsNoTracking()
            .CountAsync(x => x.KullaniciRolleri.Any(r => r.RolId == 1)),

        EgitmenSayisi = await _context.Kullanicilar
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciRolleri.Any(r => r.RolId == 2) ||
                x.EgitmenProfili != null),

        OgrenciSayisi = await _context.Kullanicilar
            .AsNoTracking()
            .CountAsync(x => x.KullaniciRolleri.Any(r => r.RolId == 3)),

        ToplamKayit = toplamKayit,
        Sayfa = sayfa,
        ToplamSayfa = toplamSayfa,
        SayfaBasinaKayit = sayfaBasinaKayit
    };

    return View(model);
    
  }

    [HttpGet]
    public async Task<IActionResult> Detay(int id)
    {
        var kullanici = await _context.Kullanicilar
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Durum)
            .Include(x => x.KullaniciRolleri)
                .ThenInclude(x => x.Rol)
            .Include(x => x.EgitmenProfili)
                .ThenInclude(x => x!.Durum)
            .Include(x => x.EgitmenProfili)
                .ThenInclude(x => x!.EgitmenBranslari)
                    .ThenInclude(x => x.Kategori)
            .FirstOrDefaultAsync(x => x.KullaniciId == id);

        if (kullanici == null)
        {
            TempData["AdminHata"] = "Kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Kullanicilar));
        }

        var kayitliKurslarQuery = await _context.KursKayitlari
            .AsNoTracking()
            .Where(x => x.KullaniciId == id)
            .OrderByDescending(x => x.KayitTarihi)
            .Select(x => new
            {
                KursKayitId = x.KursKayitId,
                KursId = x.KursId,
                KursAdi = x.Kurs.KursAdi,
                KayitTarihi = x.KayitTarihi,
                AktifMi = x.AktifMi,
                TamamlandiMi = x.TamamlandiMi,
                TamamlananDersSayisi = x.DersIlerlemeleri
                    .Count(i =>
                        i.TamamlandiMi &&
                        i.Ders.AktifMi &&
                        !i.Ders.SistemDersiMi),
                ToplamDersSayisi = x.Kurs.Dersler.Count(d => d.AktifMi && !d.SistemDersiMi),
                SinavVarMi = x.Kurs.Sinav != null
            })
            .ToListAsync();

        var kursKayitIdleri = kayitliKurslarQuery.Select(x => x.KursKayitId).ToList();

        var sinavKatilimlari = await _context.SinavKatilimlari
            .AsNoTracking()
            .Where(x => kursKayitIdleri.Contains(x.KursKayitId))
            .OrderByDescending(x => x.BaslamaTarihi)
            .Select(x => new
            {
                x.KursKayitId,
                x.BaslamaTarihi,
                x.BitisTarihi,
                x.AlinanPuan,
                x.GectiMi
            })
            .ToListAsync();

        var sonSinavlar = sinavKatilimlari
            .GroupBy(x => x.KursKayitId)
            .ToDictionary(x => x.Key, x => x.First());

        var sinavGirisSayilari = sinavKatilimlari
            .GroupBy(x => x.KursKayitId)
            .ToDictionary(x => x.Key, x => x.Count());

        var kayitliKurslar = new List<KullaniciKayitliKursViewModel>();

        foreach (var kayit in kayitliKurslarQuery)
        {
            var kursModel = new KullaniciKayitliKursViewModel
            {
                KursKayitId = kayit.KursKayitId,
                KursId = kayit.KursId,
                KursAdi = kayit.KursAdi,
                KayitTarihi = kayit.KayitTarihi,
                AktifMi = kayit.AktifMi,
                TamamlandiMi = kayit.TamamlandiMi,
                TamamlananDersSayisi = kayit.TamamlananDersSayisi,
                ToplamDersSayisi = kayit.ToplamDersSayisi,
                IlerlemeYuzdesi = kayit.ToplamDersSayisi == 0
                    ? 0
                    : (int)Math.Round(kayit.TamamlananDersSayisi * 100.0 / kayit.ToplamDersSayisi),
                SinavGirisSayisi = sinavGirisSayilari.TryGetValue(kayit.KursKayitId, out int girisSayisi)
                    ? girisSayisi
                    : 0
            };

            if (sonSinavlar.TryGetValue(kayit.KursKayitId, out var sonSinav))
            {
                kursModel.SonSinavPuani = sonSinav.AlinanPuan;
                kursModel.SinavdanGectiMi = sonSinav.GectiMi;
                kursModel.SonSinavTarihi = sonSinav.BitisTarihi;

                kursModel.SinavDurumu = sonSinav.BitisTarihi == null
                    ? "Devam ediyor"
                    : sonSinav.GectiMi == true
                        ? "Geçti"
                        : "Kaldı";
            }
            else
            {
                kursModel.SinavDurumu = kayit.SinavVarMi
                    ? "Henüz girmedi"
                    : "Sınav yok";
            }

            kayitliKurslar.Add(kursModel);
        }

        var verdigiKurslar = await _context.Kurslar
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(x => x.OlusturmaTarihi)
            .Where(x => x.EgitmenId == id)
            .Select(x => new KullaniciVerdigiKursViewModel
            {
                KursId = x.KursId,
                KursAdi = x.KursAdi,
                KapakGorselUrl = x.KapakGorselUrl,
                DurumId = x.DurumId,
                DurumAdi = x.Durum.DurumAdi,
                OlusturmaTarihi = x.OlusturmaTarihi,
                GuncellemeTarihi = x.GuncellemeTarihi,
                DersSayisi = x.Dersler.Count,
                OgrenciSayisi = _context.KursKayitlari.Count(k => k.KursId == x.KursId),
                Kategoriler = x.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList()
            })
            .ToListAsync();

        var model = new KullaniciDetayViewModel
        {
            KullaniciId = kullanici.KullaniciId,
            Ad = kullanici.Ad,
            Soyad = kullanici.Soyad,
            Eposta = kullanici.Eposta,
            Telefon = kullanici.Telefon,
            ProfilFotoUrl = kullanici.ProfilFotoUrl,

            DurumId = kullanici.DurumId,
            DurumAdi = kullanici.Durum.DurumAdi,
            OnlineMi = kullanici.OnlineMi,

            SonIpAdresi = kullanici.SonIpAdresi,
            KayitTarihi = kullanici.KayitTarihi,
            SonGirisTarihi = kullanici.SonGirisTarihi,

            Roller = kullanici.KullaniciRolleri
                .Select(x => x.Rol.RolAdi)
                .OrderBy(x => x)
                .ToList(),

            EgitmenDetay = kullanici.EgitmenProfili == null
                ? null
                : new KullaniciEgitmenDetayViewModel
                {
                    EgitmenProfilId = kullanici.EgitmenProfili.EgitmenProfilId,
                    DurumId = kullanici.EgitmenProfili.DurumId,
                    DurumAdi = kullanici.EgitmenProfili.Durum.DurumAdi,
                    UzmanlikAlani = kullanici.EgitmenProfili.UzmanlikAlani,
                    Biyografi = kullanici.EgitmenProfili.Biyografi,
                    DeneyimYili = kullanici.EgitmenProfili.DeneyimYili,
                    WebsiteUrl = kullanici.EgitmenProfili.WebsiteUrl,

                    Branslar = kullanici.EgitmenProfili.EgitmenBranslari
                        .Select(x => x.Kategori.KategoriAdi)
                        .OrderBy(x => x)
                        .ToList()
                },

            KayitliKurslar = kayitliKurslar,
            VerdigiKurslar = verdigiKurslar
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Duzenle(int id)
    {
        var kullanici = await _context.Kullanicilar
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.KullaniciRolleri)
                .ThenInclude(x => x.Rol)
            .Include(x => x.EgitmenProfili)
                .ThenInclude(x => x!.EgitmenBranslari)
            .FirstOrDefaultAsync(x => x.KullaniciId == id);

        if (kullanici == null)
        {
            TempData["AdminHata"] = "Kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Kullanicilar));
        }

        var model = new KullaniciDuzenleViewModel
        {
            KullaniciId = kullanici.KullaniciId,
            Ad = kullanici.Ad,
            Soyad = kullanici.Soyad,
            Eposta = kullanici.Eposta,
            Telefon = kullanici.Telefon,
            DurumId = kullanici.DurumId,

            Roller = kullanici.KullaniciRolleri
                .Select(x => x.Rol.RolAdi)
                .OrderBy(x => x)
                .ToList(),

            AdminYetkisiVarMi = kullanici.KullaniciRolleri
                .Any(x => x.RolId == 1),

            EgitmenMi =
                kullanici.KullaniciRolleri.Any(x => x.RolId == 2) ||
                kullanici.EgitmenProfili != null,

            EgitmenRoluVarMi = kullanici.KullaniciRolleri.Any(x => x.RolId == 2),

            OgrenciRoluVarMi = kullanici.KullaniciRolleri.Any(x => x.RolId == 3),

            EgitmenProfilId = kullanici.EgitmenProfili?.EgitmenProfilId,
            EgitmenDurumId = kullanici.EgitmenProfili?.DurumId,
            UzmanlikAlani = kullanici.EgitmenProfili?.UzmanlikAlani,
            Biyografi = kullanici.EgitmenProfili?.Biyografi,
            DeneyimYili = kullanici.EgitmenProfili?.DeneyimYili,
            WebsiteUrl = kullanici.EgitmenProfili?.WebsiteUrl,

            SeciliBransIdleri = kullanici.EgitmenProfili == null
                ? new List<int>()
                : kullanici.EgitmenProfili.EgitmenBranslari
                    .Select(x => x.KategoriId)
                    .ToList()
        };

        await DuzenleSecenekleriniDoldurAsync(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(KullaniciDuzenleViewModel model)
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        bool istenenAdminYetkisiVarMi = model.AdminYetkisiVarMi;
        bool istenenEgitmenRoluVarMi = model.EgitmenRoluVarMi;
        bool istenenOgrenciRoluVarMi = model.OgrenciRoluVarMi;

        model.Ad = model.Ad?.Trim() ?? string.Empty;
        model.Soyad = model.Soyad?.Trim() ?? string.Empty;
        model.Eposta = model.Eposta?.Trim() ?? string.Empty;
        model.Telefon = string.IsNullOrWhiteSpace(model.Telefon) ? null : model.Telefon.Trim();

        model.UzmanlikAlani = string.IsNullOrWhiteSpace(model.UzmanlikAlani) ? null : model.UzmanlikAlani.Trim();
        model.Biyografi = string.IsNullOrWhiteSpace(model.Biyografi) ? null : model.Biyografi.Trim();
        model.WebsiteUrl = string.IsNullOrWhiteSpace(model.WebsiteUrl) ? null : model.WebsiteUrl.Trim();
        model.SeciliBransIdleri ??= new List<int>();

        if (string.IsNullOrWhiteSpace(model.Ad))
        {
            ModelState.AddModelError(nameof(model.Ad), "Ad alanı zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(model.Soyad))
        {
            ModelState.AddModelError(nameof(model.Soyad), "Soyad alanı zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(model.Eposta))
        {
            ModelState.AddModelError(nameof(model.Eposta), "E-posta alanı zorunludur.");
        }

        if (model.DurumId != 1 && model.DurumId != 2)
        {
            ModelState.AddModelError(nameof(model.DurumId), "Geçersiz kullanıcı durumu.");
        }

        if (model.DeneyimYili.HasValue && model.DeneyimYili.Value < 0)
        {
            ModelState.AddModelError(nameof(model.DeneyimYili), "Deneyim yılı negatif olamaz.");
        }

        bool epostaKullaniliyor = await _context.Kullanicilar
            .AsNoTracking()
            .AnyAsync(x =>
                x.KullaniciId != model.KullaniciId &&
                x.Eposta.ToLower() == model.Eposta.ToLower());

        if (epostaKullaniliyor)
        {
            ModelState.AddModelError(nameof(model.Eposta), "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");
        }

        var kullanici = await _context.Kullanicilar
            .Include(x => x.KullaniciRolleri)
            .Include(x => x.EgitmenProfili)
                .ThenInclude(x => x!.EgitmenBranslari)
            .FirstOrDefaultAsync(x => x.KullaniciId == model.KullaniciId);

        if (kullanici == null)
        {
            TempData["AdminHata"] = "Kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Kullanicilar));
        }

        bool mevcutAdminYetkisiVarMi = kullanici.KullaniciRolleri
            .Any(x => x.RolId == 1);

        model.Roller = new List<string>();

        if (istenenAdminYetkisiVarMi)
        {
            model.Roller.Add("Admin");
        }

        if (istenenEgitmenRoluVarMi)
        {
            model.Roller.Add("Eğitmen");
        }

        if (istenenOgrenciRoluVarMi)
        {
            model.Roller.Add("Öğrenci");
        }

        model.EgitmenMi =
            istenenEgitmenRoluVarMi ||
            kullanici.EgitmenProfili != null;

        model.AdminYetkisiVarMi = istenenAdminYetkisiVarMi;
        model.EgitmenRoluVarMi = istenenEgitmenRoluVarMi;
        model.OgrenciRoluVarMi = istenenOgrenciRoluVarMi;

        model.EgitmenProfilId = kullanici.EgitmenProfili?.EgitmenProfilId;
        model.EgitmenDurumId = kullanici.EgitmenProfili?.DurumId;

        if (model.KullaniciId == adminId && model.DurumId == 2)
        {
            ModelState.AddModelError(nameof(model.DurumId), "Kendi hesabınızı pasife alamazsınız.");
        }

        if (model.DurumId == 2 && await SonAktifAdminMiAsync(model.KullaniciId))
        {
            ModelState.AddModelError(nameof(model.DurumId), "Sistemde en az bir aktif yönetici kalmalıdır.");
        }

        if (model.KullaniciId == adminId && !model.AdminYetkisiVarMi)
        {
            ModelState.AddModelError(nameof(model.AdminYetkisiVarMi), "Kendi yönetici yetkinizi kaldıramazsınız.");
        }

        if (mevcutAdminYetkisiVarMi &&
            !model.AdminYetkisiVarMi &&
            await SonAktifAdminMiAsync(model.KullaniciId))
        {
            ModelState.AddModelError(nameof(model.AdminYetkisiVarMi), "Sistemde en az bir aktif yönetici kalmalıdır.");
        }

        if (!ModelState.IsValid)
        {
            await DuzenleSecenekleriniDoldurAsync(model);
            return View(model);
        }

        kullanici.Ad = model.Ad;
        kullanici.Soyad = model.Soyad;
        kullanici.Eposta = model.Eposta;
        kullanici.Telefon = model.Telefon;
        kullanici.DurumId = model.DurumId;

        var adminRolu = kullanici.KullaniciRolleri
            .FirstOrDefault(x => x.RolId == 1);

        if (model.AdminYetkisiVarMi && adminRolu == null)
        {
            _context.KullaniciRolleri.Add(new KullaniciRol
            {
                KullaniciId = kullanici.KullaniciId,
                RolId = 1
            });
        }
        else if (!model.AdminYetkisiVarMi && adminRolu != null)
        {
            _context.KullaniciRolleri.Remove(adminRolu);
        }

        // Eğitmen rolü
        var egitmenRolu = kullanici.KullaniciRolleri
            .FirstOrDefault(x => x.RolId == 2);

        if (model.EgitmenRoluVarMi && egitmenRolu == null)
        {
            _context.KullaniciRolleri.Add(new KullaniciRol
            {
                KullaniciId = kullanici.KullaniciId,
                RolId = 2
            });
        }
        else if (!model.EgitmenRoluVarMi && egitmenRolu != null)
        {
            _context.KullaniciRolleri.Remove(egitmenRolu);
        }

        // Öğrenci rolü
        var ogrenciRolu = kullanici.KullaniciRolleri
            .FirstOrDefault(x => x.RolId == 3);

        if (model.OgrenciRoluVarMi && ogrenciRolu == null)
        {
            _context.KullaniciRolleri.Add(new KullaniciRol
            {
                KullaniciId = kullanici.KullaniciId,
                RolId = 3
            });
        }
        else if (!model.OgrenciRoluVarMi && ogrenciRolu != null)
        {
            _context.KullaniciRolleri.Remove(ogrenciRolu);
        }

        if (model.EgitmenMi && kullanici.EgitmenProfili != null)
        {
            kullanici.EgitmenProfili.UzmanlikAlani = model.UzmanlikAlani;
            kullanici.EgitmenProfili.Biyografi = model.Biyografi;
            kullanici.EgitmenProfili.DeneyimYili = model.DeneyimYili;
            kullanici.EgitmenProfili.WebsiteUrl = model.WebsiteUrl;

            var mevcutBranslar = kullanici.EgitmenProfili.EgitmenBranslari.ToList();

            _context.EgitmenBranslari.RemoveRange(mevcutBranslar);

            var gecerliBransIdleri = await _context.Kategoriler
                .AsNoTracking()
                .Where(x => model.SeciliBransIdleri.Contains(x.KategoriId))
                .Select(x => x.KategoriId)
                .ToListAsync();

            foreach (int kategoriId in gecerliBransIdleri.Distinct())
            {
                _context.EgitmenBranslari.Add(new EgitmenBransi
                {
                    EgitmenProfilId = kullanici.EgitmenProfili.EgitmenProfilId,
                    KategoriId = kategoriId
                });
            }
        }

        await _adminLogService.KaydetAsync(
            adminId,
            AdminLogService.KullaniciIslemleri,
            $"{kullanici.Ad} {kullanici.Soyad} adlı kullanıcının bilgileri güncellendi.");

        await _context.SaveChangesAsync();

        TempData["AdminBasari"] = "Kullanıcı bilgileri güncellendi.";

        return RedirectToAction(nameof(Kullanicilar));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        if (id == adminId)
        {
            TempData["AdminHata"] = "Kendi hesabınızı silemezsiniz.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var kullanici = await _context.Kullanicilar
            .Include(x => x.KullaniciRolleri)
            .Include(x => x.EgitmenProfili)
                .ThenInclude(x => x!.EgitmenBranslari)
            .FirstOrDefaultAsync(x => x.KullaniciId == id);

        if (kullanici == null)
        {
            TempData["AdminHata"] = "Kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Kullanicilar));
        }

        bool adminMi = kullanici.KullaniciRolleri.Any(x => x.RolId == 1);

        bool egitmenMi =
            kullanici.KullaniciRolleri.Any(x => x.RolId == 2) ||
            kullanici.EgitmenProfili != null;

        bool sadeceOgrenciMi =
            kullanici.KullaniciRolleri.Any(x => x.RolId == 3) &&
            !adminMi &&
            !egitmenMi;

        bool yonetimGecmisiVar = await YonetimGecmisiVarMiAsync(kullanici.KullaniciId);

        if (sadeceOgrenciMi && !yonetimGecmisiVar)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                string silinenKullaniciAdSoyad = $"{kullanici.Ad} {kullanici.Soyad}".Trim();

                await OgrenciVerileriniSilAsync(kullanici.KullaniciId);

                _context.KullaniciRolleri.RemoveRange(kullanici.KullaniciRolleri);
                _context.Kullanicilar.Remove(kullanici);

                await _adminLogService.KaydetAsync(
                    adminId,
                    AdminLogService.KullaniciIslemleri,
                    $"{silinenKullaniciAdSoyad} adlı öğrenci ve bağlı tüm öğrenci kayıtları kalıcı olarak silindi.");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["AdminBasari"] = "Öğrenci ve bağlı tüm kayıtları kalıcı olarak silindi.";

                return RedirectToAction(nameof(Kullanicilar));
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["AdminHata"] = "Öğrenci silinirken beklenmeyen bir hata oluştu.";
                return RedirectToAction(nameof(Detay), new { id = kullanici.KullaniciId });
            }
        }

        if (adminMi && await SonAktifAdminMiAsync(kullanici.KullaniciId))
        {
            TempData["AdminHata"] = "Sistemde en az bir aktif yönetici kalmalıdır. Bu kullanıcı silinemez veya pasife alınamaz.";
            return RedirectToAction(nameof(Detay), new { id = kullanici.KullaniciId });
        }

        bool kritikBagliKayitVar = await KritikBagliKayitVarMiAsync(kullanici.KullaniciId);

        if (!kritikBagliKayitVar)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                string silinenKullaniciAdSoyad = $"{kullanici.Ad} {kullanici.Soyad}".Trim();

                await KullaniciYanBagliVerileriniSilAsync(kullanici.KullaniciId);

                if (kullanici.EgitmenProfili != null)
                {
                    _context.EgitmenBranslari.RemoveRange(kullanici.EgitmenProfili.EgitmenBranslari);
                    _context.EgitmenProfilleri.Remove(kullanici.EgitmenProfili);
                }

                _context.KullaniciRolleri.RemoveRange(kullanici.KullaniciRolleri);
                _context.Kullanicilar.Remove(kullanici);

                await _adminLogService.KaydetAsync(
                    adminId,
                    AdminLogService.KullaniciIslemleri,
                    $"{silinenKullaniciAdSoyad} adlı kullanıcı kritik bağlı kaydı olmadığı için kalıcı olarak silindi.");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["AdminBasari"] = "Kullanıcı kalıcı olarak silindi.";

                return RedirectToAction(nameof(Kullanicilar));
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["AdminHata"] = "Kullanıcı silinirken beklenmeyen bir hata oluştu.";
                return RedirectToAction(nameof(Detay), new { id = kullanici.KullaniciId });
            }
        }

        kullanici.DurumId = 2;

        await _adminLogService.KaydetAsync(
            adminId,
            AdminLogService.KullaniciIslemleri,
            $"{kullanici.Ad} {kullanici.Soyad} adlı kullanıcı kritik bağlı kaydı bulunduğu için pasife alındı.");

        await _context.SaveChangesAsync();

        TempData["AdminBasari"] = "Kullanıcının kritik bağlı kayıtları olduğu için kalıcı silme yapılmadı. Kullanıcı pasife alındı.";

        return RedirectToAction(nameof(Detay), new { id = kullanici.KullaniciId });
    }

    private async Task DuzenleSecenekleriniDoldurAsync(KullaniciDuzenleViewModel model)
    {
        model.DurumSecenekleri = await _context.Durumlar
            .AsNoTracking()
            .Where(x => x.DurumId == 1 || x.DurumId == 2)
            .OrderBy(x => x.DurumId)
            .Select(x => new KullaniciDurumSecimViewModel
            {
                DurumId = x.DurumId,
                DurumAdi = x.DurumAdi
            })
            .ToListAsync();

        model.BransSecenekleri = await _context.Kategoriler
            .AsNoTracking()
            .OrderBy(x => x.KategoriAdi)
            .Select(x => new KullaniciBransSecimViewModel
            {
                KategoriId = x.KategoriId,
                KategoriAdi = x.KategoriAdi,
                SeciliMi = model.SeciliBransIdleri.Contains(x.KategoriId)
            })
            .ToListAsync();
    }

    private async Task<bool> SonAktifAdminMiAsync(int kullaniciId)
    {
        bool hedefAktifAdminMi = await _context.Kullanicilar
            .AsNoTracking()
            .AnyAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.DurumId == 1 &&
                x.KullaniciRolleri.Any(r => r.RolId == 1));

        if (!hedefAktifAdminMi)
        {
            return false;
        }

        int digerAktifAdminSayisi = await _context.Kullanicilar
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciId != kullaniciId &&
                x.DurumId == 1 &&
                x.KullaniciRolleri.Any(r => r.RolId == 1));

        return digerAktifAdminSayisi == 0;
    }

    private async Task<bool> KritikBagliKayitVarMiAsync(int kullaniciId)
    {
        if (await _context.Kurslar.AsNoTracking().AnyAsync(x => x.EgitmenId == kullaniciId))
        {
            return true;
        }

        if (await _context.KursKayitlari.AsNoTracking().AnyAsync(x => x.KullaniciId == kullaniciId))
        {
            return true;
        }

        if (await _context.Sertifikalar.AsNoTracking().AnyAsync(x => x.KullaniciId == kullaniciId))
        {
            return true;
        }

        if (await YonetimGecmisiVarMiAsync(kullaniciId))
        {
            return true;
        }

        return false;
    }

    private async Task<bool> YonetimGecmisiVarMiAsync(int kullaniciId)
    {
        if (await _context.AdminLoglari.AsNoTracking().AnyAsync(x => x.AdminId == kullaniciId))
        {
            return true;
        }

        if (await _context.EgitmenOnaylari.AsNoTracking().AnyAsync(x => x.AdminId == kullaniciId))
        {
            return true;
        }

        if (await _context.KursOnaylari.AsNoTracking().AnyAsync(x => x.AdminId == kullaniciId))
        {
            return true;
        }

        return false;
    }

    private async Task OgrenciVerileriniSilAsync(int kullaniciId)
    {
        var kursKayitIdleri = await _context.KursKayitlari
            .Where(x => x.KullaniciId == kullaniciId)
            .Select(x => x.KursKayitId)
            .ToListAsync();

        var sinavKatilimIdleri = await _context.SinavKatilimlari
            .Where(x => kursKayitIdleri.Contains(x.KursKayitId))
            .Select(x => x.SinavKatilimId)
            .ToListAsync();

        var ogrenciCevaplari = await _context.OgrenciCevaplari
            .Where(x => sinavKatilimIdleri.Contains(x.SinavKatilimId))
            .ToListAsync();

        var sinavKatilimlari = await _context.SinavKatilimlari
            .Where(x => sinavKatilimIdleri.Contains(x.SinavKatilimId))
            .ToListAsync();

        var dersIlerlemeleri = await _context.DersIlerlemeleri
            .Where(x => kursKayitIdleri.Contains(x.KursKayitId))
            .ToListAsync();

        var kursKayitlari = await _context.KursKayitlari
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var sertifikalar = await _context.Sertifikalar
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        _context.OgrenciCevaplari.RemoveRange(ogrenciCevaplari);
        _context.SinavKatilimlari.RemoveRange(sinavKatilimlari);
        _context.DersIlerlemeleri.RemoveRange(dersIlerlemeleri);
        _context.KursKayitlari.RemoveRange(kursKayitlari);
        _context.Sertifikalar.RemoveRange(sertifikalar);

        await KullaniciYanBagliVerileriniSilAsync(kullaniciId);
    }

    private async Task KullaniciYanBagliVerileriniSilAsync(int kullaniciId)
    {
        var bildirimler = await _context.Bildirimler
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var oneriler = await _context.Oneriler
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var sifreSifirlamalari = await _context.SifreSifirlamalari
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var favoriler = await _context.Favoriler
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var degerlendirmeler = await _context.KursDegerlendirmeleri
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        var egitmenOnaylari = await _context.EgitmenOnaylari
            .Where(x => x.KullaniciId == kullaniciId)
            .ToListAsync();

        _context.Bildirimler.RemoveRange(bildirimler);
        _context.Oneriler.RemoveRange(oneriler);
        _context.SifreSifirlamalari.RemoveRange(sifreSifirlamalari);
        _context.Favoriler.RemoveRange(favoriler);
        _context.KursDegerlendirmeleri.RemoveRange(degerlendirmeler);
        _context.EgitmenOnaylari.RemoveRange(egitmenOnaylari);
    }

}
