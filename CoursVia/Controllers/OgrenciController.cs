using CoursVia.Data;
using CoursVia.ViewModels.Ogrenci;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers;

// Öğrenci panelinde kayıtlı kursları, ilerleme durumunu ve sınav özetlerini yönetir.
[Authorize(Roles = "Öğrenci")]
public class OgrenciController : Controller
{
    private readonly AppDbContext _context;

    public OgrenciController(AppDbContext context)
    {
        _context = context;
    }

    // Öğrencinin ana panelini hazırlar.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var ogrenci = await _context.Kullanicilar
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId);

        if (ogrenci == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Öğrencinin aktif olarak kayıtlı olduğu kurslar alınır.
        var kurslar = await _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                x.AktifMi)
            .OrderByDescending(x => x.KayitTarihi)
            .Select(x => new OgrenciKursOzetViewModel
            {
                KursKayitId = x.KursKayitId,
                KursId = x.KursId,

                KursAdi = x.Kurs.KursAdi,
                KapakGorselUrl = x.Kurs.KapakGorselUrl,

                EgitmenAdSoyad = x.Kurs.Egitmen.Ad + " " + x.Kurs.Egitmen.Soyad,

                DurumId = x.Kurs.DurumId,
                DurumAdi = x.Kurs.Durum.DurumAdi,

                KayitTarihi = x.KayitTarihi,

                KursTamamlandiMi = x.TamamlandiMi,

                // Kursun aktif ve normal ders sayısı hesaplanır.
                ToplamDersSayisi = x.Kurs.Dersler
                    .Count(d =>
                        d.AktifMi &&
                        !d.SistemDersiMi),

                // Öğrencinin tamamladığı aktif ders sayısı hesaplanır.
                TamamlananDersSayisi = x.DersIlerlemeleri
                    .Count(i =>
                        i.TamamlandiMi &&
                        i.Ders.AktifMi &&
                        !i.Ders.SistemDersiMi),

                // Kursun öğrencinin favorilerinde olup olmadığı kontrol edilir.
                FavorideMi = _context.Favoriler.Any(f =>
                    f.KullaniciId == kullaniciId &&
                    f.KursId == x.KursId)
            })
            .ToListAsync();

        var kursKayitIdleri = kurslar
            .Select(x => x.KursKayitId)
            .ToList();

        // Öğrencinin kayıtlı kurslarındaki sınav katılımları tek sorguda alınır.
        var sinavKatilimlari = await _context.SinavKatilimlari
            .AsNoTracking()
            .Where(x => kursKayitIdleri.Contains(x.KursKayitId))
            .OrderByDescending(x => x.BaslamaTarihi)
            .Select(x => new
            {
                x.KursKayitId,
                x.BitisTarihi,
                x.AlinanPuan,
                x.GectiMi
            })
            .ToListAsync();

        // Her kurs kaydı için en son sınav sonucu bulunur.
        var sonSinavlar = sinavKatilimlari
            .GroupBy(x => x.KursKayitId)
            .ToDictionary(
                x => x.Key,
                x => x.First()
            );

        // Her kurs için ilerleme yüzdesi ve sınav durumu hesaplanır.
        foreach (var kurs in kurslar)
        {
            // İlerleme yüzdesi, tamamlanan ders sayısının toplam ders sayısına oranı olarak hesaplanır.
            kurs.IlerlemeYuzdesi = kurs.ToplamDersSayisi == 0
                ? 0
                : (int)Math.Round((kurs.TamamlananDersSayisi * 100.0) / kurs.ToplamDersSayisi);
           
            if (sonSinavlar.TryGetValue(kurs.KursKayitId, out var sonSinav))
            { // Son sınav bilgileri kurs özetine eklenir.
                kurs.SonSinavPuani = sonSinav.AlinanPuan;
                kurs.SinavdanGectiMi = sonSinav.GectiMi;

                kurs.SinavDurumu = sonSinav.BitisTarihi == null
                    ? "Devam ediyor"
                    : sonSinav.GectiMi == true
                        ? "Geçti"
                        : "Kaldı";
            }
            else
            {
                kurs.SinavDurumu = "Henüz girmedi";
            }
        }

        // Öğrenci panelinde gösterilecek kurs listesi ve özet istatistikler hazırlanır.
        var model = new OgrenciPanelViewModel
        {
            OgrenciAdSoyad = $"{ogrenci.Ad} {ogrenci.Soyad}".Trim(),

            Kurslar = kurslar,

            ToplamKayitliKursSayisi = kurslar.Count,

            TamamlananKursSayisi = kurslar.Count(x => x.KursTamamlandiMi),

            DevamEdenKursSayisi = kurslar.Count(x => !x.KursTamamlandiMi && !x.GuncelleniyorMu),

            OrtalamaIlerlemeYuzdesi = kurslar.Any()
                ? (int)Math.Round(kurslar.Average(x => x.IlerlemeYuzdesi))
                : 0
        };

        return View(model);
    }
}