using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Ogrenci;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Ogrenci;

[ApiController]
[Route("api/mobile/ogrenci/sinavlarim")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Öğrenci"
)]
public class MobileOgrenciSinavlarimController : MobileOgrenciBaseController
{
    private const int MaksimumSinavHakki = 3;

    public MobileOgrenciSinavlarimController(AppDbContext context) : base(context)
    {
    }

    // Öğrencinin kayıtlı kurslarındaki sınav durumlarını listeler.
    // GET /api/mobile/ogrenci/sinavlarim
    [HttpGet]
    public async Task<ActionResult<MobileOgrenciSinavlarimResponse>> Sinavlarim(
        [FromQuery] string? arama,
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBasinaKayit = 10)
    {
        int kullaniciId = KullaniciIdGetir();

        // Arama ve sayfalama değerleri sorguya girmeden normalize edilir.
        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        // Öğrencinin yalnızca aktif kurs kayıtları sınav listesine dahil edilir.
        var query = _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                x.AktifMi);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            // Arama kurs, eğitmen ve varsa sınav adı üzerinden yapılır.
            query = query.Where(x =>
                x.Kurs.KursAdi.Contains(arama) ||
                x.Kurs.Egitmen.Ad.Contains(arama) ||
                x.Kurs.Egitmen.Soyad.Contains(arama) ||
                x.Kurs.Sinav != null && x.Kurs.Sinav.SinavAdi.Contains(arama));
        }

        int toplamKayit = await query.CountAsync();
        int toplamSayfa = ToplamSayfaHesapla(toplamKayit, sayfaBasinaKayit);

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        // Kurs ve sınav özetleri alınır; sınav hakkı hesabı aşağıda katılımlarla yapılır.
        var kursKayitlari = await query
            .OrderByDescending(x => x.KayitTarihi)
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new
            {
                x.KursKayitId,
                x.KursId,

                x.Kurs.KursAdi,
                x.Kurs.KapakGorselUrl,
                x.Kurs.DurumId,
                DurumAdi = x.Kurs.Durum.DurumAdi,

                SinavId = x.Kurs.Sinav == null ? (int?)null : x.Kurs.Sinav.SinavId,
                SinavAdi = x.Kurs.Sinav == null ? null : x.Kurs.Sinav.SinavAdi,
                GecmeNotu = x.Kurs.Sinav == null ? (int?)null : x.Kurs.Sinav.GecmeNotu,

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

        var kursKayitIdleri = kursKayitlari
            .Select(x => x.KursKayitId)
            .ToList();

        // Sayfadaki kurs kayıtlarına ait tüm sınav girişleri tek sorguyla çekilir.
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

        var sinavlar = new List<MobileOgrenciSinavItemResponse>();

        foreach (var kayit in kursKayitlari)
        {
            // Her kurs için öğrencinin sınav geçmişi ve son giriş bilgisi ayrıştırılır.
            var buKursaAitKatilimlar = sinavKatilimlari
                .Where(x => x.KursKayitId == kayit.KursKayitId)
                .OrderByDescending(x => x.BaslamaTarihi)
                .ToList();

            var sonSinav = buKursaAitKatilimlar.FirstOrDefault();

            bool derslerTamamlandiMi =
                kayit.ToplamDersSayisi > 0 &&
                kayit.TamamlananDersSayisi >= kayit.ToplamDersSayisi;

            // Başarısız ve tamamlanmış denemeler maksimum sınav hakkından düşülür.
            int basarisizDenemeSayisi = buKursaAitKatilimlar
                .Count(x =>
                    x.BitisTarihi != null &&
                    x.GectiMi == false);

            bool basariliMi = buKursaAitKatilimlar.Any(x => x.GectiMi == true);

            bool devamEdenSinavVar = buKursaAitKatilimlar.Any(x =>
                x.BitisTarihi == null);

            // Başarılı olan öğrencinin yeniden sınava girmesine gerek olmadığı için hak 0 gösterilir.
            int kalanHak = basariliMi
                ? 0
                : Math.Max(0, MaksimumSinavHakki - basarisizDenemeSayisi);

            string durumMetni;

            // Mobil ekranda tek bir durum etiketi gösterilebilmesi için öncelikli karar sırası uygulanır.
            if (kayit.DurumId == 7 && !basariliMi)
            {
                durumMetni = "Güncelleniyor";
            }
            else if (kayit.SinavId == null)
            {
                durumMetni = "Sınav bulunmuyor";
            }
            else if (!derslerTamamlandiMi)
            {
                durumMetni = "Dersleri tamamla";
            }
            else if (devamEdenSinavVar)
            {
                durumMetni = "Devam ediyor";
            }
            else if (basariliMi)
            {
                durumMetni = "Başarılı";
            }
            else if (kalanHak <= 0)
            {
                durumMetni = "Hak doldu";
            }
            else if (!buKursaAitKatilimlar.Any())
            {
                durumMetni = "Sınava hazır";
            }
            else
            {
                durumMetni = "Tekrar girilebilir";
            }

            sinavlar.Add(new MobileOgrenciSinavItemResponse
            {
                KursKayitId = kayit.KursKayitId,
                KursId = kayit.KursId,

                KursAdi = kayit.KursAdi,
                KapakGorselUrl = kayit.KapakGorselUrl,

                DurumId = kayit.DurumId,
                DurumAdi = kayit.DurumAdi,
                GuncelleniyorMu = kayit.DurumId == 7,
                DevamEdilebilirMi = kayit.DurumId == 5,

                SinavId = kayit.SinavId,
                SinavAdi = kayit.SinavAdi,
                GecmeNotu = kayit.GecmeNotu,

                DerslerTamamlandiMi = derslerTamamlandiMi,
                ToplamDersSayisi = kayit.ToplamDersSayisi,
                TamamlananDersSayisi = kayit.TamamlananDersSayisi,

                GirisSayisi = buKursaAitKatilimlar.Count,
                KalanHak = kalanHak,

                SonPuan = sonSinav?.AlinanPuan,
                SonucGectiMi = sonSinav?.GectiMi,
                SonSinavTarihi = sonSinav?.BaslamaTarihi,

                DurumMetni = durumMetni
            });
        }

        return Ok(new MobileOgrenciSinavlarimResponse
        {
            Basarili = true,
            Mesaj = "Öğrencinin sınav durumları getirildi.",
            Arama = arama,
            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            SayfaBasinaKayit = sayfaBasinaKayit,
            ToplamSayfa = toplamSayfa,
            Sinavlar = sinavlar
        });
    }
}
