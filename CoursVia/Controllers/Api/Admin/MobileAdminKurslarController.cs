using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Admin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Admin;

[ApiController]
[Route("api/mobile/admin/kurslar")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Admin"
)]
public class MobileAdminKurslarController : MobileAdminBaseController
{
    public MobileAdminKurslarController(AppDbContext context) : base(context)
    {
    }

    // Admin mobil kurs listesini döndürür.
    // Mobilde kurs onay/red işlemi yoktur, sadece görüntüleme vardır.
    // Arama, durum filtresi, kategori filtresi, sıralama ve sayfalama destekler.
    // Kurs durum filtresinde Aktif ve Onaylandı yoktur.
    // Desteklenen kurs durumları:
    // 2 = Pasif
    // 3 = Taslak
    // 4 = Onay Bekliyor
    // 5 = Yayında
    // 6 = Reddedildi
    // 7 = Düzeltme İsteniyor
    [HttpGet]
    public async Task<ActionResult<MobileAdminKurslarResponse>> Kurslar(
        [FromQuery] string? arama,
        [FromQuery] int? durumId,
        [FromQuery] int? kategoriId,
        [FromQuery] string? sirala = "guncel",
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBasinaKayit = 10)
    {
        // Arama, durum, kategori, sıralama ve sayfalama değerleri normalize edilir.
        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        // Kurslarda sadece 2-7 arası durumlar filtrelenebilir.
        // 1 = Aktif kullanıcılar için, 8 = Eğitmen başvurusu Onaylandı için kullanılır.
        durumId = durumId.HasValue &&
                  (
                      durumId.Value == 2 ||
                      durumId.Value == 3 ||
                      durumId.Value == 4 ||
                      durumId.Value == 5 ||
                      durumId.Value == 6 ||
                      durumId.Value == 7
                  )
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

        // Mobil filtreler için izin verilen kurs durumları ve mevcut kategoriler hazırlanır.
        var durumlar = await _context.Durumlar
            .AsNoTracking()
            .Where(x =>
                x.DurumId == 2 ||
                x.DurumId == 3 ||
                x.DurumId == 4 ||
                x.DurumId == 5 ||
                x.DurumId == 6 ||
                x.DurumId == 7)
            .OrderBy(x => x.DurumId)
            .Select(x => new MobileAdminSecenekResponse
            {
                Id = x.DurumId,
                Ad = x.DurumAdi
            })
            .ToListAsync();

        var kategoriler = await _context.KursKategorileri
            .AsNoTracking()
            .GroupBy(x => new
            {
                x.KategoriId,
                x.Kategori.KategoriAdi
            })
            .Select(x => new MobileAdminSecenekResponse
            {
                Id = x.Key.KategoriId,
                Ad = x.Key.KategoriAdi
            })
            .OrderBy(x => x.Ad)
            .ToListAsync();

        // Admin kurs listesi tüm kurslardan başlar; filtreler aşağıda uygulanır.
        var query = _context.Kurslar
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            // Arama kurs, eğitmen, durum ve kategori adları üzerinden yapılır.
            query = query.Where(x =>
                x.KursAdi.Contains(arama) ||
                x.Egitmen.Ad.Contains(arama) ||
                x.Egitmen.Soyad.Contains(arama) ||
                x.Durum.DurumAdi.Contains(arama) ||
                x.KursKategorileri.Any(k => k.Kategori.KategoriAdi.Contains(arama)));
        }

        if (durumId.HasValue)
        {
            query = query.Where(x => x.DurumId == durumId.Value);
        }

        if (kategoriId.HasValue)
        {
            // Kategori filtresi çoklu kurs-kategori ilişkisi üzerinden uygulanır.
            query = query.Where(x =>
                x.KursKategorileri.Any(k => k.KategoriId == kategoriId.Value));
        }

        int toplamKayit = await query.CountAsync();
        int toplamSayfa = ToplamSayfaHesapla(toplamKayit, sayfaBasinaKayit);

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        // Sıralama; tarih, ad, puan ve öğrenci sayısı seçeneklerine göre veritabanında uygulanır.
        query = sirala switch
        {
            "eski" => query.OrderBy(x => x.OlusturmaTarihi),

            "ad-az" => query.OrderBy(x => x.KursAdi),
            "ad-za" => query.OrderByDescending(x => x.KursAdi),

            "puan-yuksek" => query
                .OrderByDescending(x =>
                    _context.KursDegerlendirmeleri
                        .Where(d => d.KursId == x.KursId)
                        .Select(d => (double?)d.Puan)
                        .Average() ?? 0)
                .ThenByDescending(x =>
                    _context.KursDegerlendirmeleri.Count(d => d.KursId == x.KursId)),

            "puan-dusuk" => query
                .OrderBy(x =>
                    _context.KursDegerlendirmeleri
                        .Where(d => d.KursId == x.KursId)
                        .Select(d => (double?)d.Puan)
                        .Average() ?? 0)
                .ThenBy(x =>
                    _context.KursDegerlendirmeleri.Count(d => d.KursId == x.KursId)),

            "ogrenci-cok" => query
                .OrderByDescending(x =>
                    _context.KursKayitlari.Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi))
                .ThenBy(x => x.KursAdi),

            "ogrenci-az" => query
                .OrderBy(x =>
                    _context.KursKayitlari.Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi))
                .ThenBy(x => x.KursAdi),

            _ => query.OrderByDescending(x => x.GuncellemeTarihi ?? x.OlusturmaTarihi)
        };

        // Liste DTO'su için kurs istatistikleri alt sorgularla birlikte seçilir.
        var kurslar = await query
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new MobileAdminKursItemResponse
            {
                KursId = x.KursId,

                KursAdi = x.KursAdi,
                KapakGorselUrl = x.KapakGorselUrl,

                EgitmenAdSoyad = x.Egitmen.Ad + " " + x.Egitmen.Soyad,

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
            })
            .ToListAsync();

        foreach (var kurs in kurslar)
        {
            // Mobil görünüm için ad soyad ve ortalama puan formatı sonradan düzenlenir.
            kurs.EgitmenAdSoyad = kurs.EgitmenAdSoyad.Trim();
            // Ortalama puan 1 ondalık basamak ile yuvarlanır.
            kurs.OrtalamaPuan = Math.Round(kurs.OrtalamaPuan, 1);
        }

        return Ok(new MobileAdminKurslarResponse
        {
            Basarili = true,
            Mesaj = "Kurs listesi getirildi.",

            Arama = arama,
            DurumId = durumId,
            KategoriId = kategoriId,
            Sirala = sirala,

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            SayfaBasinaKayit = sayfaBasinaKayit,
            ToplamSayfa = toplamSayfa,

            Durumlar = durumlar,
            Kategoriler = kategoriler,
            Kurslar = kurslar
        });
    }

    // Admin mobil kurs detayını döndürür.
    // Mobilde kurs onay/red işlemi yoktur, sadece görüntüleme vardır.
    // Ders altında materyal adı ve tipi döner. Materyal URL dönülmez.
    // GET /api/mobile/admin/kurslar/{kursId}
    [HttpGet("{kursId:int}")]
    public async Task<ActionResult<MobileAdminKursDetayResponse>> Detay(int kursId)
    {
        // Kursun genel bilgileri ve istatistikleri tek detay DTO'suna projekte edilir.
        var kursDetay = await _context.Kurslar
            .AsNoTracking()
            .Where(x => x.KursId == kursId)
            .Select(x => new MobileAdminKursDetayResponse
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

                Kategoriler = x.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList(),

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
            return NotFound(new MobileAdminKursDetayResponse
            {
                Basarili = false,
                Mesaj = "Kurs bulunamadı."
            });
        }

        kursDetay.EgitmenAdSoyad = kursDetay.EgitmenAdSoyad.Trim();
        kursDetay.OrtalamaPuan = Math.Round(kursDetay.OrtalamaPuan, 1);

        // Bölüm, ders ve materyal başlıkları sıralı şekilde ayrı sorguyla doldurulur.
        kursDetay.Bolumler = await _context.Bolumler
            .AsNoTracking()
            .Where(x => x.KursId == kursId)
            .OrderBy(x => x.SiraNo)
            .Select(x => new MobileAdminKursBolumItemResponse
            {
                BolumId = x.BolumId,
                BolumAdi = x.BolumAdi,
                SiraNo = x.SiraNo,

                Dersler = x.Dersler
                    .Where(d =>
                        d.AktifMi &&
                        !d.SistemDersiMi)
                    .OrderBy(d => d.SiraNo)
                    .Select(d => new MobileAdminKursDersItemResponse
                    {
                        DersId = d.DersId,
                        DersAdi = d.DersAdi,
                        SiraNo = d.SiraNo,
                        AktifMi = d.AktifMi,

                        MateryalVarMi = d.DersMateryalleri.Any(),
                        MateryalSayisi = d.DersMateryalleri.Count(),

                        Materyaller = d.DersMateryalleri
                            .OrderBy(m => m.YuklenmeTarihi)
                            .Select(m => new MobileAdminKursDersMateryalItemResponse
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
            // Ders sayısı aktif ve sistem dersi olmayan filtrelenmiş liste üzerinden hesaplanır.
            bolum.DersSayisi = bolum.Dersler.Count;
        }

        return Ok(kursDetay);
    }
}
