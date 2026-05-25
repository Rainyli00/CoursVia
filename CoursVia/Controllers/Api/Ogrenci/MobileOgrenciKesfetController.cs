using CoursVia.Data;
using CoursVia.Models;
using CoursVia.ViewModels.Mobile.Ogrenci;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Ogrenci;

[ApiController]
[Route("api/mobile/ogrenci/kesfet")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Öğrenci"
)]
public class MobileOgrenciKesfetController : MobileOgrenciBaseController
{
    public MobileOgrenciKesfetController(AppDbContext context) : base(context)
    {
    }

    // Öğrencinin keşfedebileceği yayındaki kursları listeler.
    // Kayıtlı olduğu kurslar da listede görünür.
    // Eğitmen kendi kursuna öğrenci olarak kayıt olamaz.
    // Arama, kategori filtresi, sıralama ve sayfalama destekler.
    // Desteklenen sıralama değerleri:
    // guncel, puan-yuksek, populer, degerlendirme-cok, ad-az, ad-za
    // GET /api/mobile/ogrenci/kesfet?arama=react&kategoriId=1&sirala=populer&sayfa=1&sayfaBasinaKayit=10
    [HttpGet]
    public async Task<ActionResult<MobileOgrenciKesfetResponse>> Kesfet(
        [FromQuery] string? arama,
        [FromQuery] int? kategoriId,
        [FromQuery] string? sirala = "guncel",
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBasinaKayit = 10)
    {
        int kullaniciId = KullaniciIdGetir();

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        kategoriId = kategoriId.GetValueOrDefault() > 0
            ? kategoriId
            : null;

        sirala = string.IsNullOrWhiteSpace(sirala)
            ? "guncel"
            : sirala.Trim().ToLower();

        sirala = sirala switch
        {
            "guncel" => "guncel",
            "puan-yuksek" => "puan-yuksek",
            "populer" => "populer",
            "degerlendirme-cok" => "degerlendirme-cok",
            "ad-az" => "ad-az",
            "ad-za" => "ad-za",
            _ => "guncel"
        };

        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        var kategoriler = await KesfetKategorileriGetirAsync();

        var kayitliKursIdleri = await _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                x.AktifMi)
            .Select(x => x.KursId)
            .ToListAsync();

        var query = _context.Kurslar
            .AsNoTracking()
            .Where(x => x.DurumId == 5);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.KursAdi.Contains(arama) ||
                (x.Aciklama != null && x.Aciklama.Contains(arama)) ||
                x.Egitmen.Ad.Contains(arama) ||
                x.Egitmen.Soyad.Contains(arama));
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

        query = sirala switch
        {
            "puan-yuksek" => query
                .OrderByDescending(x =>
                    x.KursDegerlendirmeleri
                        .Select(d => (double?)d.Puan)
                        .Average() ?? 0)
                .ThenByDescending(x => x.KursDegerlendirmeleri.Count())
                .ThenByDescending(x => x.OlusturmaTarihi),

            "populer" => query
                .OrderByDescending(x =>
                    _context.KursKayitlari.Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi))
                .ThenByDescending(x =>
                    x.KursDegerlendirmeleri
                        .Select(d => (double?)d.Puan)
                        .Average() ?? 0)
                .ThenByDescending(x => x.OlusturmaTarihi),

            "degerlendirme-cok" => query
                .OrderByDescending(x => x.KursDegerlendirmeleri.Count())
                .ThenByDescending(x =>
                    x.KursDegerlendirmeleri
                        .Select(d => (double?)d.Puan)
                        .Average() ?? 0)
                .ThenByDescending(x => x.OlusturmaTarihi),

            "ad-az" => query
                .OrderBy(x => x.KursAdi)
                .ThenByDescending(x => x.OlusturmaTarihi),

            "ad-za" => query
                .OrderByDescending(x => x.KursAdi)
                .ThenByDescending(x => x.OlusturmaTarihi),

            _ => query.OrderByDescending(x => x.OlusturmaTarihi)
        };

        var kurslar = await query
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new MobileOgrenciKesfetKursItemResponse
            {
                KursId = x.KursId,

                KursAdi = x.KursAdi,
                Aciklama = x.Aciklama,
                KapakGorselUrl = x.KapakGorselUrl,

                EgitmenAdSoyad = x.Egitmen.Ad + " " + x.Egitmen.Soyad,

                DurumId = x.DurumId,
                DurumAdi = x.Durum.DurumAdi,
                GuncelleniyorMu = x.DurumId == 7,
                DevamEdilebilirMi = x.DurumId == 5,

                Kategoriler = x.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList(),

                ToplamBolumSayisi = x.Bolumler.Count(),

                ToplamDersSayisi = x.Dersler
                    .Count(d =>
                        d.AktifMi &&
                        !d.SistemDersiMi),

                KayitliOgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi),

                DegerlendirmeSayisi = x.KursDegerlendirmeleri.Count(),

                OrtalamaPuan = x.KursDegerlendirmeleri.Any()
                    ? Math.Round(x.KursDegerlendirmeleri.Average(d => d.Puan), 1)
                    : 0,

                KayitliMi = kayitliKursIdleri.Contains(x.KursId),

                KendiKursuMu = x.EgitmenId == kullaniciId,

                KayitOlabilirMi =
                    x.DurumId == 5 &&
                    !kayitliKursIdleri.Contains(x.KursId) &&
                    x.EgitmenId != kullaniciId
            })
            .ToListAsync();

        foreach (var kurs in kurslar)
        {
            kurs.EgitmenAdSoyad = kurs.EgitmenAdSoyad.Trim();
        }

        return Ok(new MobileOgrenciKesfetResponse
        {
            Basarili = true,
            Mesaj = "Keşfet kursları getirildi.",

            Arama = arama,
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

    // Keşfet kurs detayını getirir.
    // Öğrenci kayıtlıysa kayıtlı bilgisi döner.
    // Eğitmen kendi kursuna öğrenci olarak kayıt olamaz.
    // GET /api/mobile/ogrenci/kesfet/{kursId}
    [HttpGet("{kursId:int}")]
    public async Task<ActionResult<MobileOgrenciKesfetDetayResponse>> Detay(int kursId)
    {
        int kullaniciId = KullaniciIdGetir();

        var kursDurumu = await _context.Kurslar
            .AsNoTracking()
            .Where(x => x.KursId == kursId)
            .Select(x => new
            {
                x.KursId,
                x.KursAdi,
                x.KapakGorselUrl,
                x.DurumId,
                DurumAdi = x.Durum.DurumAdi
            })
            .FirstOrDefaultAsync();

        if (kursDurumu == null)
        {
            return NotFound(new MobileOgrenciKesfetDetayResponse
            {
                Basarili = false,
                Mesaj = "Kurs bulunamadı."
            });
        }

        if (kursDurumu.DurumId == 7)
        {
            return BadRequest(new MobileOgrenciKesfetDetayResponse
            {
                Basarili = false,
                Mesaj = "Bu kurs şu anda güncelleniyor. Güncelleme tamamlandığında tekrar devam edebilirsiniz.",
                KursId = kursDurumu.KursId,
                KursAdi = kursDurumu.KursAdi,
                KapakGorselUrl = kursDurumu.KapakGorselUrl,
                DurumId = kursDurumu.DurumId,
                DurumAdi = kursDurumu.DurumAdi,
                GuncelleniyorMu = true,
                DevamEdilebilirMi = false,
                KayitOlabilirMi = false
            });
        }

        if (kursDurumu.DurumId != 5)
        {
            return NotFound(new MobileOgrenciKesfetDetayResponse
            {
                Basarili = false,
                Mesaj = "Kurs bulunamadı."
            });
        }

        bool kayitliMi = await _context.KursKayitlari
            .AsNoTracking()
            .AnyAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == kursId &&
                x.AktifMi);

        var kurs = await _context.Kurslar
            .AsNoTracking()
            .Where(x =>
                x.KursId == kursId &&
                x.DurumId == 5)
            .Select(x => new MobileOgrenciKesfetDetayResponse
            {
                Basarili = true,
                Mesaj = "Kurs detayı getirildi.",

                KursId = x.KursId,
                KursAdi = x.KursAdi,
                Aciklama = x.Aciklama,
                KapakGorselUrl = x.KapakGorselUrl,

                EgitmenAdSoyad = x.Egitmen.Ad + " " + x.Egitmen.Soyad,

                DurumId = x.DurumId,
                DurumAdi = x.Durum.DurumAdi,
                GuncelleniyorMu = x.DurumId == 7,
                DevamEdilebilirMi = x.DurumId == 5,

                Kategoriler = x.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList(),

                ToplamBolumSayisi = x.Bolumler.Count(),

                ToplamDersSayisi = x.Dersler
                    .Count(d =>
                        d.AktifMi &&
                        !d.SistemDersiMi),

                KayitliOgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi),

                DegerlendirmeSayisi = x.KursDegerlendirmeleri.Count(),

                OrtalamaPuan = x.KursDegerlendirmeleri.Any()
                    ? Math.Round(x.KursDegerlendirmeleri.Average(d => d.Puan), 1)
                    : 0,

                SinavVarMi = x.Sinav != null,

                GecmeNotu = x.Sinav == null
                    ? null
                    : x.Sinav.GecmeNotu,

                KayitliMi = kayitliMi,

                KendiKursuMu = x.EgitmenId == kullaniciId,

                KayitOlabilirMi =
                    x.DurumId == 5 &&
                    !kayitliMi &&
                    x.EgitmenId != kullaniciId,

                Bolumler = x.Bolumler
                    .OrderBy(b => b.SiraNo)
                    .Select(b => new MobileOgrenciKesfetBolumResponse
                    {
                        BolumId = b.BolumId,
                        BolumAdi = b.BolumAdi,
                        SiraNo = b.SiraNo,

                        Dersler = b.Dersler
                            .Where(d =>
                                d.AktifMi &&
                                !d.SistemDersiMi)
                            .OrderBy(d => d.SiraNo)
                            .Select(d => new MobileOgrenciKesfetDersResponse
                            {
                                DersId = d.DersId,
                                DersAdi = d.DersAdi,
                                SiraNo = d.SiraNo,

                                MateryalVarMi = d.DersMateryalleri.Any(),
                                MateryalSayisi = d.DersMateryalleri.Count()
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (kurs == null)
        {
            return NotFound(new MobileOgrenciKesfetDetayResponse
            {
                Basarili = false,
                Mesaj = "Kurs bulunamadı."
            });
        }

        kurs.EgitmenAdSoyad = kurs.EgitmenAdSoyad.Trim();

        foreach (var bolum in kurs.Bolumler)
        {
            bolum.DersSayisi = bolum.Dersler.Count;
        }

        return Ok(kurs);
    }

    // Öğrenciyi keşfet üzerinden kursa kaydeder.
    // Eğitmen kendi kursuna öğrenci olarak kayıt olamaz.
    // POST /api/mobile/ogrenci/kesfet/{kursId}/kayit-ol
    [HttpPost("{kursId:int}/kayit-ol")]
    public async Task<ActionResult<MobileOgrenciIslemResponse>> KayitOl(int kursId)
    {
        int kullaniciId = KullaniciIdGetir();

        var kurs = await _context.Kurslar
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.KursId == kursId &&
                x.DurumId == 5);

        if (kurs == null)
        {
            return NotFound(new MobileOgrenciIslemResponse
            {
                Basarili = false,
                Mesaj = "Kurs bulunamadı veya yayında değil."
            });
        }

        if (kurs.EgitmenId == kullaniciId)
        {
            return BadRequest(new MobileOgrenciIslemResponse
            {
                Basarili = false,
                Mesaj = "Kendi kursunuza öğrenci olarak kayıt olamazsınız."
            });
        }

        var mevcutKayit = await _context.KursKayitlari
            .FirstOrDefaultAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == kursId);

        if (mevcutKayit != null && mevcutKayit.AktifMi)
        {
            return BadRequest(new MobileOgrenciIslemResponse
            {
                Basarili = false,
                Mesaj = "Bu kursa zaten kayıtlısınız."
            });
        }

        if (mevcutKayit != null && !mevcutKayit.AktifMi)
        {
            mevcutKayit.AktifMi = true;
            mevcutKayit.TamamlandiMi = false;
            mevcutKayit.TamamlanmaTarihi = null;
            mevcutKayit.KayitTarihi = DateTime.Now;
        }
        else
        {
            _context.KursKayitlari.Add(new KursKaydi
            {
                KullaniciId = kullaniciId,
                KursId = kursId,
                KayitTarihi = DateTime.Now,
                AktifMi = true,
                TamamlandiMi = false,
                TamamlanmaTarihi = null
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new MobileOgrenciIslemResponse
        {
            Basarili = true,
            Mesaj = "Kursa başarıyla kayıt olundu."
        });
    }

    // Keşfet filtreleri için yayındaki kurs kategorilerini getirir.
    private async Task<List<MobileOgrenciKategoriSecenekResponse>> KesfetKategorileriGetirAsync()
    {
        var kategoriler = await _context.KursKategorileri
            .AsNoTracking()
            .Where(x => x.Kurs.DurumId == 5)
            .Select(x => new
            {
                x.KategoriId,
                x.Kategori.KategoriAdi,
                x.KursId
            })
            .ToListAsync();

        return kategoriler
            .GroupBy(x => new { x.KategoriId, x.KategoriAdi })
            .Select(x => new MobileOgrenciKategoriSecenekResponse
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
