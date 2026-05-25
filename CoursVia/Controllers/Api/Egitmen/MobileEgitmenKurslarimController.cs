using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Egitmen;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Egitmen;

[ApiController]
[Route("api/mobile/egitmen/kurslarim")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Eğitmen"
)]
public class MobileEgitmenKurslarimController : MobileEgitmenBaseController
{
    public MobileEgitmenKurslarimController(AppDbContext context) : base(context)
    {
    }

    // Eğitmenin tüm kurslarını mobil liste ekranı için döndürür.
    // Arama, durum filtresi, kategori filtresi, sıralama ve sayfalama destekler.
    // GET /api/mobile/egitmen/kurslarim?arama=react&durumId=5&kategoriId=2&sirala=guncel&sayfa=1&sayfaBasinaKayit=10
    [HttpGet]
    public async Task<ActionResult<MobileEgitmenKurslarimResponse>> Kurslarim(
        [FromQuery] string? arama,
        [FromQuery] int? durumId,
        [FromQuery] int? kategoriId,
        [FromQuery] string? sirala = "guncel",
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBasinaKayit = 10)
    {
        int kullaniciId = KullaniciIdGetir();

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        durumId = durumId.GetValueOrDefault() > 0
            ? durumId
            : null;

        kategoriId = kategoriId.GetValueOrDefault() > 0
            ? kategoriId
            : null;

        sirala = string.IsNullOrWhiteSpace(sirala)
            ? "guncel"
            : sirala.Trim().ToLower();

        sirala = sirala switch
        {
            "guncel" => "guncel",
            "eski" => "eski",
            "ad-az" => "ad-az",
            "ad-za" => "ad-za",
            "puan-yuksek" => "puan-yuksek",
            "puan-dusuk" => "puan-dusuk",
            "ogrenci-cok" => "ogrenci-cok",
            "ogrenci-az" => "ogrenci-az",
            _ => "guncel"
        };

        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        var kategoriler = await EgitmenKategorileriGetirAsync(kullaniciId);

        var query = _context.Kurslar
            .AsNoTracking()
            .Where(x => x.EgitmenId == kullaniciId);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.KursAdi.Contains(arama) ||
                x.Durum.DurumAdi.Contains(arama) ||
                x.KursKategorileri.Any(k => k.Kategori.KategoriAdi.Contains(arama)));
        }

        if (durumId.HasValue)
        {
            query = query.Where(x => x.DurumId == durumId.Value);
        }

        if (kategoriId.HasValue)
        {
            query = query.Where(x =>
                x.KursKategorileri.Any(k => k.KategoriId == kategoriId.Value));
        }

        int toplamKayit = await query.CountAsync();
        int toplamSayfa = ToplamSayfaHesapla(toplamKayit, sayfaBasinaKayit);

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        var kursQuery = query
            .Select(x => new MobileEgitmenKursItemResponse
            {
                KursId = x.KursId,

                KursAdi = x.KursAdi,
                KapakGorselUrl = x.KapakGorselUrl,

                Kategoriler = x.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList(),

                DurumId = x.DurumId,
                DurumAdi = x.Durum.DurumAdi,

                OgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi),

                TamamlayanOgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi &&
                        k.TamamlandiMi),

                DersSayisi = _context.Dersler
                    .Count(d =>
                        d.KursId == x.KursId &&
                        d.AktifMi &&
                        !d.SistemDersiMi),

                DegerlendirmeSayisi = _context.KursDegerlendirmeleri
                    .Count(d => d.KursId == x.KursId),

                OrtalamaPuan = _context.KursDegerlendirmeleri
                    .Where(d => d.KursId == x.KursId)
                    .Select(d => (double?)d.Puan)
                    .Average() ?? 0,

                OlusturmaTarihi = x.OlusturmaTarihi,
                GuncellemeTarihi = x.GuncellemeTarihi
            });

        kursQuery = sirala switch
        {
            "eski" => kursQuery.OrderBy(x => x.OlusturmaTarihi),

            "ad-az" => kursQuery.OrderBy(x => x.KursAdi),
            "ad-za" => kursQuery.OrderByDescending(x => x.KursAdi),

            "puan-yuksek" => kursQuery
                .OrderByDescending(x => x.OrtalamaPuan)
                .ThenByDescending(x => x.DegerlendirmeSayisi),

            "puan-dusuk" => kursQuery
                .OrderBy(x => x.OrtalamaPuan)
                .ThenBy(x => x.DegerlendirmeSayisi),

            "ogrenci-cok" => kursQuery
                .OrderByDescending(x => x.OgrenciSayisi)
                .ThenBy(x => x.KursAdi),

            "ogrenci-az" => kursQuery
                .OrderBy(x => x.OgrenciSayisi)
                .ThenBy(x => x.KursAdi),

            _ => kursQuery.OrderByDescending(x => x.GuncellemeTarihi ?? x.OlusturmaTarihi)
        };

        var kurslar = await kursQuery
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .ToListAsync();

        foreach (var kurs in kurslar)
        {
            kurs.OrtalamaPuan = Math.Round(kurs.OrtalamaPuan, 1);
        }

        return Ok(new MobileEgitmenKurslarimResponse
        {
            Basarili = true,
            Mesaj = "Eğitmen kursları getirildi.",

            Arama = arama,
            DurumId = durumId,
            KategoriId = kategoriId,
            Sirala = sirala,

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            SayfaBasinaKayit = sayfaBasinaKayit,
            ToplamSayfa = toplamSayfa,

            Kategoriler = kategoriler,
            Kurslar = kurslar
        });
    }

    // Eğitmenin seçtiği kursun mobil detay bilgisini döndürür.
    // Son öğrenciler dönülmez.
    // Son 5 yorum döndürülür.
    // Ders altında materyal adı ve tipi döner. Materyal URL dönülmez.
    // GET /api/mobile/egitmen/kurslarim/{kursId}
    [HttpGet("{kursId:int}")]
    public async Task<ActionResult<MobileEgitmenKursDetayResponse>> KursDetay(int kursId)
    {
        int kullaniciId = KullaniciIdGetir();

        var kursDetay = await _context.Kurslar
            .AsNoTracking()
            .Where(x =>
                x.KursId == kursId &&
                x.EgitmenId == kullaniciId)
            .Select(x => new MobileEgitmenKursDetayResponse
            {
                Basarili = true,
                Mesaj = "Kurs detayı getirildi.",

                KursId = x.KursId,
                KursAdi = x.KursAdi,
                Aciklama = x.Aciklama,
                KapakGorselUrl = x.KapakGorselUrl,

                Kategoriler = x.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList(),

                DurumId = x.DurumId,
                DurumAdi = x.Durum.DurumAdi,

                OgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi),

                TamamlayanOgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi &&
                        k.TamamlandiMi),

                BolumSayisi = _context.Bolumler
                    .Count(b => b.KursId == x.KursId),

                DersSayisi = _context.Dersler
                    .Count(d =>
                        d.KursId == x.KursId &&
                        d.AktifMi &&
                        !d.SistemDersiMi),

                DegerlendirmeSayisi = _context.KursDegerlendirmeleri
                    .Count(d => d.KursId == x.KursId),

                OrtalamaPuan = _context.KursDegerlendirmeleri
                    .Where(d => d.KursId == x.KursId)
                    .Select(d => (double?)d.Puan)
                    .Average() ?? 0,

                SinavVarMi = x.Sinav != null,
                SinavAdi = x.Sinav == null ? null : x.Sinav.SinavAdi,
                SinavSoruSayisi = x.Sinav == null ? (int?)null : x.Sinav.SoruSayisi,
                SinavSureDakika = x.Sinav == null ? (int?)null : x.Sinav.SureDakika,
                SinavGecmeNotu = x.Sinav == null ? (int?)null : x.Sinav.GecmeNotu,

                OlusturmaTarihi = x.OlusturmaTarihi,
                GuncellemeTarihi = x.GuncellemeTarihi
            })
            .FirstOrDefaultAsync();

        if (kursDetay == null)
        {
            return NotFound(new MobileEgitmenKursDetayResponse
            {
                Basarili = false,
                Mesaj = "Kurs bulunamadı."
            });
        }

        kursDetay.OrtalamaPuan = Math.Round(kursDetay.OrtalamaPuan, 1);

        kursDetay.Bolumler = await _context.Bolumler
            .AsNoTracking()
            .Where(x => x.KursId == kursId)
            .OrderBy(x => x.SiraNo)
            .Select(x => new MobileEgitmenBolumItemResponse
            {
                BolumId = x.BolumId,
                BolumAdi = x.BolumAdi,
                SiraNo = x.SiraNo,

                Dersler = x.Dersler
                    .Where(d =>
                        d.AktifMi &&
                        !d.SistemDersiMi)
                    .OrderBy(d => d.SiraNo)
                    .Select(d => new MobileEgitmenDersItemResponse
                    {
                        DersId = d.DersId,
                        DersAdi = d.DersAdi,
                        SiraNo = d.SiraNo,
                        AktifMi = d.AktifMi,

                        MateryalVarMi = d.DersMateryalleri.Any(),
                        MateryalSayisi = d.DersMateryalleri.Count(),

                        Materyaller = d.DersMateryalleri
                            .OrderBy(m => m.YuklenmeTarihi)
                            .Select(m => new MobileEgitmenDersMateryalItemResponse
                            {
                                MateryalId = m.MateryalId,
                                Baslik = m.Baslik,
                                MateryalTipAdi = m.MateryalTipi.MateryalTipAdi
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToListAsync();

        foreach (var bolum in kursDetay.Bolumler)
        {
            bolum.DersSayisi = bolum.Dersler.Count;
        }

        kursDetay.SonYorumlar = await _context.KursDegerlendirmeleri
            .AsNoTracking()
            .Where(x =>
                x.KursId == kursId &&
                x.YorumMetni != null &&
                x.YorumMetni != "")
            .OrderByDescending(x => x.DegerlendirmeTarihi)
            .Take(5)
            .Select(x => new MobileEgitmenKursYorumItemResponse
            {
                DegerlendirmeId = x.DegerlendirmeId,
                KullaniciId = x.KullaniciId,

                OgrenciAdSoyad = x.Kullanici.Ad + " " + x.Kullanici.Soyad,

                Puan = x.Puan,
                YorumMetni = x.YorumMetni ?? string.Empty,
                DegerlendirmeTarihi = x.DegerlendirmeTarihi
            })
            .ToListAsync();

        foreach (var yorum in kursDetay.SonYorumlar)
        {
            yorum.OgrenciAdSoyad = yorum.OgrenciAdSoyad.Trim();
        }

        return Ok(kursDetay);
    }

    // Eğitmenin kendi kursunu taslak durumuna alır.
    // POST /api/mobile/egitmen/kurslarim/{kursId}/taslaga-al
    [HttpPost("{kursId:int}/taslaga-al")]
    public async Task<ActionResult<MobileEgitmenIslemResponse>> TaslagaAl(int kursId)
    {
        int kullaniciId = KullaniciIdGetir();

        var kurs = await _context.Kurslar
            .FirstOrDefaultAsync(x =>
                x.KursId == kursId &&
                x.EgitmenId == kullaniciId);

        if (kurs == null)
        {
            return NotFound(new MobileEgitmenIslemResponse
            {
                Basarili = false,
                Mesaj = "Kurs bulunamadı."
            });
        }

        if (kurs.DurumId == 3)
        {
            return BadRequest(new MobileEgitmenIslemResponse
            {
                Basarili = false,
                Mesaj = "Bu kurs zaten taslak durumunda."
            });
        }

        kurs.DurumId = 3;
        kurs.GuncellemeTarihi = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new MobileEgitmenIslemResponse
        {
            Basarili = true,
            Mesaj = "Kurs taslağa alındı."
        });
    }

    // Eğitmen kurslarım ekranındaki kategori filtresi için seçenekleri getirir.
    // Sadece eğitmenin kendi kurslarında kullanılan kategoriler döner.
    private async Task<List<MobileEgitmenKategoriSecenekResponse>> EgitmenKategorileriGetirAsync(int kullaniciId)
    {
        var kategoriler = await _context.KursKategorileri
            .AsNoTracking()
            .Where(x => x.Kurs.EgitmenId == kullaniciId)
            .Select(x => new
            {
                x.KategoriId,
                x.Kategori.KategoriAdi,
                x.KursId
            })
            .ToListAsync();

        return kategoriler
            .GroupBy(x => new { x.KategoriId, x.KategoriAdi })
            .Select(x => new MobileEgitmenKategoriSecenekResponse
            {
                KategoriId = x.Key.KategoriId,
                KategoriAdi = x.Key.KategoriAdi,
                KayitSayisi = x
                    .Select(k => k.KursId)
                    .Distinct()
                    .Count()
            })
            .OrderBy(x => x.KategoriAdi)
            .ToList();
    }
}