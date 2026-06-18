using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Egitmen;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Egitmen;

// Mobil uygulamada eğitmenin öğrencilerini listeleyen API controller.
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
    // Aynı öğrenci birden fazla kursa kayıtlıysa tek kayıt olarak listelenir.
    // Arama, kurs filtresi ve sayfalama desteklenir.
    // Liste öğrenci adına göre A-Z sıralanır.
    [HttpGet]
    public async Task<ActionResult<MobileEgitmenOgrencilerimResponse>> Ogrencilerim(
        [FromQuery] string? arama,
        [FromQuery] int? kursId,
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBasinaKayit = 10)
    {
        // JWT token içinden giriş yapan eğitmenin kullanıcı Id değeri alınır.
        int kullaniciId = KullaniciIdGetir();

        // Arama metni boşsa null yapılır, doluysa baştaki ve sondaki boşluklar temizlenir.
        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        // KursId 0 veya negatif gelirse filtre kullanılmasın diye null yapılır.
        kursId = kursId.GetValueOrDefault() > 0
            ? kursId
            : null;

        // Sayfa ve sayfa başına kayıt değerleri güvenli aralığa çekilir.
        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        // Eğer kurs filtresi gönderildiyse, bu kursun gerçekten giriş yapan eğitmene ait olup olmadığı kontrol edilir.
        if (kursId.HasValue)
        {
            bool kursEgitmeneAitMi = await _context.Kurslar
                .AsNoTracking()
                .AnyAsync(x =>
                    x.KursId == kursId.Value &&
                    x.EgitmenId == kullaniciId);

            // Kurs bu eğitmene ait değilse veya yoksa 404 döndürülür.
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

        // Eğitmenin kurslarına ait aktif kurs kayıtları temel sorgu olarak hazırlanır.
        var kayitQuery = _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.AktifMi &&
                x.Kurs.EgitmenId == kullaniciId);

        // Kurs filtresi varsa sadece o kursa ait kayıtlar alınır.
        if (kursId.HasValue)
        {
            kayitQuery = kayitQuery.Where(x => x.KursId == kursId.Value);
        }

        // Arama varsa öğrenci adı, soyadı veya kurs adına göre filtreleme yapılır.
        if (!string.IsNullOrWhiteSpace(arama))
        {
            kayitQuery = kayitQuery.Where(x =>
                x.Kullanici.Ad.Contains(arama) ||
                x.Kullanici.Soyad.Contains(arama) ||
                x.Kurs.KursAdi.Contains(arama));
        }

        // Aynı öğrenci birden fazla kursa kayıtlı olabilir.
        // Bu yüzden KullaniciId üzerinden gruplanarak her öğrenci tek satır yapılır.
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

                // Öğrencinin bu eğitmenin kaç farklı kursuna kayıtlı olduğu hesaplanır.
                KayitliKursSayisi = x
                    .Select(k => k.KursId)
                    .Distinct()
                    .Count()
            })
            .OrderBy(x => x.OgrenciAdSoyad);

        // Filtrelenmiş toplam öğrenci sayısı hesaplanır.
        int toplamKayit = await query.CountAsync();

        // Toplam sayfa sayısı hesaplanır.
        int toplamSayfa = ToplamSayfaHesapla(toplamKayit, sayfaBasinaKayit);

        // İstenen sayfa toplam sayfadan büyükse son sayfaya çekilir.
        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        // Öğrenciler sayfalama uygulanarak alınır.
        var ogrenciler = await query
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .ToListAsync();

        // Öğrenci ad soyadındaki gereksiz boşluklar temizlenir.
        foreach (var ogrenci in ogrenciler)
        {
            ogrenci.OgrenciAdSoyad = ogrenci.OgrenciAdSoyad.Trim();
        }

        // Mobil uygulamaya öğrenci listesi ve sayfalama bilgileri döndürülür.
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