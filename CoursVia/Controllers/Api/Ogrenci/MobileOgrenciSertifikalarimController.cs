using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Ogrenci;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Ogrenci;

[ApiController]
[Route("api/mobile/ogrenci/sertifikalarim")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Öğrenci"
)]
public class MobileOgrenciSertifikalarimController : MobileOgrenciBaseController
{
    public MobileOgrenciSertifikalarimController(AppDbContext context) : base(context)
    {
    }

    // Öğrencinin kazandığı sertifikaları listeler.
    // GET /api/mobile/ogrenci/sertifikalarim
    [HttpGet]
    public async Task<ActionResult<MobileOgrenciSertifikalarimResponse>> Sertifikalarim(
        [FromQuery] string? arama,
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBasinaKayit = 10)
    {
        int kullaniciId = KullaniciIdGetir();

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        var query = _context.Sertifikalar
            .AsNoTracking()
            .Where(x => x.KullaniciId == kullaniciId);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.Kurs.KursAdi.Contains(arama) ||
                x.Kurs.Egitmen.Ad.Contains(arama) ||
                x.Kurs.Egitmen.Soyad.Contains(arama) ||
                x.SertifikaKodu.Contains(arama));
        }

        int toplamKayit = await query.CountAsync();
        int toplamSayfa = ToplamSayfaHesapla(toplamKayit, sayfaBasinaKayit);

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        var sertifikalar = await query
            .OrderByDescending(x => x.VerilmeTarihi)
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new MobileOgrenciSertifikaItemResponse
            {
                SertifikaId = x.SertifikaId,

                KursId = x.KursId,
                KursAdi = x.Kurs.KursAdi,
                KapakGorselUrl = x.Kurs.KapakGorselUrl,

                EgitmenAdSoyad = x.Kurs.Egitmen.Ad + " " + x.Kurs.Egitmen.Soyad,

                SertifikaKodu = x.SertifikaKodu,
                VerilmeTarihi = x.VerilmeTarihi
            })
            .ToListAsync();

        foreach (var sertifika in sertifikalar)
        {
            sertifika.EgitmenAdSoyad = sertifika.EgitmenAdSoyad.Trim();
        }

        return Ok(new MobileOgrenciSertifikalarimResponse
        {
            Basarili = true,
            Mesaj = "Öğrencinin sertifikaları getirildi.",
            Arama = arama,
            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            SayfaBasinaKayit = sayfaBasinaKayit,
            ToplamSayfa = toplamSayfa,
            Sertifikalar = sertifikalar
        });
    }
}