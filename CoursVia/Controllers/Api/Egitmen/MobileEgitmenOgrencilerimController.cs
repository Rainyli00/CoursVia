using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Egitmen;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Egitmen;

[ApiController]
[Route("api/mobile/egitmen/ogrencilerim")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Eğitmen"
)]
public class MobileEgitmenOgrencilerimController : MobileEgitmenBaseController
{
    public MobileEgitmenOgrencilerimController(AppDbContext context) : base(context)
    {
    }

    // Eğitmenin kurslarına kayıtlı benzersiz öğrencileri döndürür.
    // Aynı öğrenci birden fazla kursa kayıtlıysa tek kayıt olarak döner.
    // Arama, kurs filtresi ve sayfalama destekler.
    // Sıralama filtresi kaldırıldı. Liste öğrenci adına göre A-Z döner.
    // GET /api/mobile/egitmen/ogrencilerim?arama=ali&kursId=5&sayfa=1&sayfaBasinaKayit=10
    [HttpGet]
    public async Task<ActionResult<MobileEgitmenOgrencilerimResponse>> Ogrencilerim(
        [FromQuery] string? arama,
        [FromQuery] int? kursId,
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBasinaKayit = 10)
    {
        int kullaniciId = KullaniciIdGetir();

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        kursId = kursId.GetValueOrDefault() > 0
            ? kursId
            : null;

        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        if (kursId.HasValue)
        {
            bool kursEgitmeneAitMi = await _context.Kurslar
                .AsNoTracking()
                .AnyAsync(x =>
                    x.KursId == kursId.Value &&
                    x.EgitmenId == kullaniciId);

            if (!kursEgitmeneAitMi)
            {
                return NotFound(new MobileEgitmenOgrencilerimResponse
                {
                    Basarili = false,
                    Mesaj = "Kurs bulunamadı.",
                    Arama = arama,
                    KursId = kursId,
                    Sayfa = sayfa,
                    SayfaBasinaKayit = sayfaBasinaKayit,
                    ToplamSayfa = 1
                });
            }
        }

        var kayitQuery = _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.AktifMi &&
                x.Kurs.EgitmenId == kullaniciId);

        if (kursId.HasValue)
        {
            kayitQuery = kayitQuery.Where(x => x.KursId == kursId.Value);
        }

        if (!string.IsNullOrWhiteSpace(arama))
        {
            kayitQuery = kayitQuery.Where(x =>
                x.Kullanici.Ad.Contains(arama) ||
                x.Kullanici.Soyad.Contains(arama) ||
                x.Kurs.KursAdi.Contains(arama));
        }

        var query = kayitQuery
            .GroupBy(x => new
            {
                x.KullaniciId,
                x.Kullanici.Ad,
                x.Kullanici.Soyad,
                x.Kullanici.ProfilFotoUrl
            })
            .Select(x => new MobileEgitmenOgrenciItemResponse
            {
                KullaniciId = x.Key.KullaniciId,
                OgrenciAdSoyad = x.Key.Ad + " " + x.Key.Soyad,
                ProfilFotoUrl = x.Key.ProfilFotoUrl,

                KayitliKursSayisi = x
                    .Select(k => k.KursId)
                    .Distinct()
                    .Count()
            })
            .OrderBy(x => x.OgrenciAdSoyad);

        int toplamKayit = await query.CountAsync();
        int toplamSayfa = ToplamSayfaHesapla(toplamKayit, sayfaBasinaKayit);

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        var ogrenciler = await query
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .ToListAsync();

        foreach (var ogrenci in ogrenciler)
        {
            ogrenci.OgrenciAdSoyad = ogrenci.OgrenciAdSoyad.Trim();
        }

        return Ok(new MobileEgitmenOgrencilerimResponse
        {
            Basarili = true,
            Mesaj = "Eğitmenin öğrencileri getirildi.",

            Arama = arama,
            KursId = kursId,

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            SayfaBasinaKayit = sayfaBasinaKayit,
            ToplamSayfa = toplamSayfa,

            Ogrenciler = ogrenciler
        });
    }
}