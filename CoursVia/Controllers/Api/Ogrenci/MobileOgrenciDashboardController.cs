using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Ogrenci;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Ogrenci;

[ApiController]
[Route("api/mobile/ogrenci")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Öğrenci"
)]
public class MobileOgrenciDashboardController : MobileOgrenciBaseController
{
    public MobileOgrenciDashboardController(AppDbContext context) : base(context)
    {
    }

    // Öğrenci mobil ana ekranı.
    // Kayıtlı kurs, devam eden kurs, tamamlanan kurs, sertifika ve son kurs özetlerini döndürür.
    [HttpGet("dashboard")]
    public async Task<ActionResult<MobileOgrenciDashboardResponse>> Dashboard()
    {
        int kullaniciId = KullaniciIdGetir();

        var kursOzetleri = await _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                x.AktifMi)
            .Select(x => new
            {
                x.TamamlandiMi,

                ToplamDersSayisi = x.Kurs.Dersler
                    .Count(d =>
                        d.AktifMi &&
                        !d.SistemDersiMi),

                TamamlananDersSayisi = x.DersIlerlemeleri
                    .Count(i =>
                        i.TamamlandiMi &&
                        i.Ders.AktifMi &&
                        !i.Ders.SistemDersiMi)
            })
            .ToListAsync();

        int kayitliKursSayisi = kursOzetleri.Count;
        int devamEdenKursSayisi = kursOzetleri.Count(x => !x.TamamlandiMi);
        int tamamlananKursSayisi = kursOzetleri.Count(x => x.TamamlandiMi);

        int ortalamaIlerlemeYuzdesi = 0;

        if (kursOzetleri.Any())
        {
            var ilerlemeler = kursOzetleri.Select(x =>
            {
                if (x.ToplamDersSayisi == 0)
                {
                    return 0;
                }

                return (int)Math.Round(x.TamamlananDersSayisi * 100.0 / x.ToplamDersSayisi);
            });

            ortalamaIlerlemeYuzdesi = (int)Math.Round(ilerlemeler.Average());
        }

        int sertifikaSayisi = await _context.Sertifikalar
            .AsNoTracking()
            .CountAsync(x => x.KullaniciId == kullaniciId);

        int okunmamisBildirimSayisi = await _context.Bildirimler
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciId == kullaniciId &&
                !x.OkunduMu);

        var sonKurslar = await _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                x.AktifMi)
            .OrderByDescending(x => x.KayitTarihi)
            .Take(5)
            .Select(x => new MobileOgrenciDashboardKursResponse
            {
                KursKayitId = x.KursKayitId,
                KursId = x.KursId,

                KursAdi = x.Kurs.KursAdi,
                KapakGorselUrl = x.Kurs.KapakGorselUrl,

                EgitmenAdSoyad = x.Kurs.Egitmen.Ad + " " + x.Kurs.Egitmen.Soyad,

                DurumId = x.Kurs.DurumId,
                DurumAdi = x.Kurs.Durum.DurumAdi,
                GuncelleniyorMu = x.Kurs.DurumId == 7,
                DevamEdilebilirMi = x.Kurs.DurumId == 5,

                ToplamDersSayisi = x.Kurs.Dersler
                    .Count(d =>
                        d.AktifMi &&
                        !d.SistemDersiMi),

                TamamlananDersSayisi = x.DersIlerlemeleri
                    .Count(i =>
                        i.TamamlandiMi &&
                        i.Ders.AktifMi &&
                        !i.Ders.SistemDersiMi),

                KursTamamlandiMi = x.TamamlandiMi
            })
            .ToListAsync();

        foreach (var kurs in sonKurslar)
        {
            kurs.EgitmenAdSoyad = kurs.EgitmenAdSoyad.Trim();

            kurs.IlerlemeYuzdesi = kurs.ToplamDersSayisi == 0
                ? 0
                : (int)Math.Round(kurs.TamamlananDersSayisi * 100.0 / kurs.ToplamDersSayisi);
        }

        return Ok(new MobileOgrenciDashboardResponse
        {
            Basarili = true,
            Mesaj = "Öğrenci dashboard bilgileri getirildi.",

            KayitliKursSayisi = kayitliKursSayisi,
            DevamEdenKursSayisi = devamEdenKursSayisi,
            TamamlananKursSayisi = tamamlananKursSayisi,
            OrtalamaIlerlemeYuzdesi = ortalamaIlerlemeYuzdesi,
            SertifikaSayisi = sertifikaSayisi,
            OkunmamisBildirimSayisi = okunmamisBildirimSayisi,

            SonKurslar = sonKurslar
        });
    }
}
