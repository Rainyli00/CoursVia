using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Egitmen;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Egitmen;

// Mobil uygulamada eğitmenin kendi kurslarını listelemesini,
// kurs detaylarını görüntülemesini ve kursu taslağa almasını yöneten API controller.
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
    // Örnek:
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
        // JWT token içinden giriş yapan eğitmenin kullanıcı Id değeri alınır.
        int kullaniciId = KullaniciIdGetir();

        // Arama metni boşsa null yapılır, doluysa baştaki ve sondaki boşluklar temizlenir.
        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        // DurumId 0 veya negatif gelirse filtre uygulanmaması için null yapılır.
        durumId = durumId.GetValueOrDefault() > 0
            ? durumId
            : null;

        // KategoriId 0 veya negatif gelirse filtre uygulanmaması için null yapılır.
        kategoriId = kategoriId.GetValueOrDefault() > 0
            ? kategoriId
            : null;

        // Sıralama parametresi boş gelirse varsayılan olarak güncel sıralama kullanılır.
        sirala = string.IsNullOrWhiteSpace(sirala)
            ? "guncel"
            : sirala.Trim().ToLower();

        // Sadece desteklenen sıralama değerlerine izin verilir.
        // Geçersiz değer gelirse varsayılan olarak guncel seçilir.
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

        // Sayfa ve sayfa başına kayıt değerleri güvenli aralığa çekilir.
        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        // Kategori filtre alanında gösterilecek, eğitmenin kurslarında kullanılan kategoriler alınır.
        var kategoriler = await EgitmenKategorileriGetirAsync(kullaniciId);

        // Temel sorgu sadece giriş yapan eğitmene ait kursları getirir.
        var query = _context.Kurslar
            .AsNoTracking()
            .Where(x => x.EgitmenId == kullaniciId);

        // Arama metni varsa kurs adı, durum adı veya kategori adına göre filtreleme yapılır.
        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.KursAdi.Contains(arama) ||
                x.Durum.DurumAdi.Contains(arama) ||
                x.KursKategorileri.Any(k => k.Kategori.KategoriAdi.Contains(arama)));
        }

        // Durum filtresi varsa sadece o durumdaki kurslar listelenir.
        if (durumId.HasValue)
        {
            query = query.Where(x => x.DurumId == durumId.Value);
        }

        // Kategori filtresi varsa sadece o kategoriye bağlı kurslar listelenir.
        if (kategoriId.HasValue)
        {
            query = query.Where(x =>
                x.KursKategorileri.Any(k => k.KategoriId == kategoriId.Value));
        }

        // Filtrelerden sonra toplam kayıt ve toplam sayfa hesaplanır.
        int toplamKayit = await query.CountAsync();
        int toplamSayfa = ToplamSayfaHesapla(toplamKayit, sayfaBasinaKayit);

        // İstenen sayfa toplam sayfadan büyükse son sayfaya çekilir.
        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        // Kurslar mobil liste ekranında kullanılacak ViewModel formatına dönüştürülür.
        var kursQuery = query
            .Select(x => new MobileEgitmenKursItemResponse
            {
                KursId = x.KursId,

                KursAdi = x.KursAdi,
                KapakGorselUrl = x.KapakGorselUrl,

                // Kursun bağlı olduğu kategoriler alfabetik olarak listelenir.
                Kategoriler = x.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList(),

                DurumId = x.DurumId,
                DurumAdi = x.Durum.DurumAdi,

                // Kursa aktif kayıtlı öğrenci sayısı hesaplanır.
                OgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi),

                // Kursu tamamlayan aktif öğrenci sayısı hesaplanır.
                TamamlayanOgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi &&
                        k.TamamlandiMi),

                // Kurstaki aktif ve sistem dersi olmayan normal ders sayısı hesaplanır.
                DersSayisi = _context.Dersler
                    .Count(d =>
                        d.KursId == x.KursId &&
                        d.AktifMi &&
                        !d.SistemDersiMi),

                // Kursa yapılan toplam değerlendirme sayısı hesaplanır.
                DegerlendirmeSayisi = _context.KursDegerlendirmeleri
                    .Count(d => d.KursId == x.KursId),

                // Kursun ortalama puanı hesaplanır.
                // Değerlendirme yoksa 0 döner.
                OrtalamaPuan = _context.KursDegerlendirmeleri
                    .Where(d => d.KursId == x.KursId)
                    .Select(d => (double?)d.Puan)
                    .Average() ?? 0,

                OlusturmaTarihi = x.OlusturmaTarihi,
                GuncellemeTarihi = x.GuncellemeTarihi
            });

        // Kullanıcının seçtiği sıralama türüne göre kurs listesi sıralanır.
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

        // Sayfalama uygulanarak ilgili sayfadaki kurslar alınır.
        var kurslar = await kursQuery
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .ToListAsync();

        // Ortalama puan mobil tarafta tek ondalık basamakla gösterilsin diye yuvarlanır.
        foreach (var kurs in kurslar)
        {
            kurs.OrtalamaPuan = Math.Round(kurs.OrtalamaPuan, 1);
        }

        // Mobil uygulamaya kurs listesi, filtre bilgileri ve sayfalama bilgileri döndürülür.
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
    // Son 5 yorum döndürülür.
    // Ders altında materyal adı ve tipi döner, materyal URL bilgisi döndürülmez.
    [HttpGet("{kursId:int}")]
    public async Task<ActionResult<MobileEgitmenKursDetayResponse>> KursDetay(int kursId)
    {
        // JWT token içinden giriş yapan eğitmenin kullanıcı Id değeri alınır.
        int kullaniciId = KullaniciIdGetir();

        // Kursun giriş yapan eğitmene ait olup olmadığı kontrol edilerek detay bilgisi hazırlanır.
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

                // Kursa kayıtlı aktif öğrenci sayısı hesaplanır.
                OgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi),

                // Kursu tamamlayan aktif öğrenci sayısı hesaplanır.
                TamamlayanOgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi &&
                        k.TamamlandiMi),

                // Kursun toplam bölüm sayısı hesaplanır.
                BolumSayisi = _context.Bolumler
                    .Count(b => b.KursId == x.KursId),

                // Kursun aktif normal ders sayısı hesaplanır.
                DersSayisi = _context.Dersler
                    .Count(d =>
                        d.KursId == x.KursId &&
                        d.AktifMi &&
                        !d.SistemDersiMi),

                // Kursa yapılan toplam değerlendirme sayısı hesaplanır.
                DegerlendirmeSayisi = _context.KursDegerlendirmeleri
                    .Count(d => d.KursId == x.KursId),

                // Kursun ortalama puanı hesaplanır.
                OrtalamaPuan = _context.KursDegerlendirmeleri
                    .Where(d => d.KursId == x.KursId)
                    .Select(d => (double?)d.Puan)
                    .Average() ?? 0,

                // Kursa sınav tanımlıysa sınav bilgileri döndürülür.
                SinavVarMi = x.Sinav != null,
                SinavAdi = x.Sinav == null ? null : x.Sinav.SinavAdi,
                SinavSoruSayisi = x.Sinav == null ? (int?)null : x.Sinav.SoruSayisi,
                SinavSureDakika = x.Sinav == null ? (int?)null : x.Sinav.SureDakika,
                SinavGecmeNotu = x.Sinav == null ? (int?)null : x.Sinav.GecmeNotu,

                OlusturmaTarihi = x.OlusturmaTarihi,
                GuncellemeTarihi = x.GuncellemeTarihi
            })
            .FirstOrDefaultAsync();

        // Kurs yoksa veya bu eğitmene ait değilse 404 döndürülür.
        if (kursDetay == null)
        {
            return NotFound(new MobileEgitmenKursDetayResponse
            {
                Basarili = false,
                Mesaj = "Kurs bulunamadı."
            });
        }

        // Ortalama puan tek ondalık basamağa yuvarlanır.
        kursDetay.OrtalamaPuan = Math.Round(kursDetay.OrtalamaPuan, 1);

        // Kursa ait bölümler, dersler ve ders materyal özetleri alınır.
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

                        // Derste materyal olup olmadığı ve materyal sayısı hesaplanır.
                        MateryalVarMi = d.DersMateryalleri.Any(),
                        MateryalSayisi = d.DersMateryalleri.Count(),

                        // Mobil detayda materyalin sadece adı ve tipi döndürülür.
                        // Dosya URL bilgisi burada döndürülmez.
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

        // Her bölüm için ders sayısı hesaplanır.
        foreach (var bolum in kursDetay.Bolumler)
        {
            bolum.DersSayisi = bolum.Dersler.Count;
        }

        // Kursa yapılan son 5 yorumlu değerlendirme alınır.
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

        // Öğrenci ad soyadındaki gereksiz boşluklar temizlenir.
        foreach (var yorum in kursDetay.SonYorumlar)
        {
            yorum.OgrenciAdSoyad = yorum.OgrenciAdSoyad.Trim();
        }

        return Ok(kursDetay);
    }

    // Eğitmenin kendi kursunu taslak durumuna alır.
    [HttpPost("{kursId:int}/taslaga-al")]
    public async Task<ActionResult<MobileEgitmenIslemResponse>> TaslagaAl(int kursId)
    {
        // JWT token içinden giriş yapan eğitmenin kullanıcı Id değeri alınır.
        int kullaniciId = KullaniciIdGetir();

        // Kursun gerçekten giriş yapan eğitmene ait olup olmadığı kontrol edilir.
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

        // Kurs zaten taslaksa tekrar taslağa alma işlemi yapılmaz.
        if (kurs.DurumId == 3)
        {
            return BadRequest(new MobileEgitmenIslemResponse
            {
                Basarili = false,
                Mesaj = "Bu kurs zaten taslak durumunda."
            });
        }

        // Kurs taslak durumuna alınır.
        // DurumId = 3 => Taslak
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
        // Eğitmenin kurslarına bağlı kategori kayıtları alınır.
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

        // Kategoriler gruplanır.
        // Her kategori için eğitmenin kaç farklı kursunda kullanıldığı hesaplanır.
        return kategoriler
            .GroupBy(x => new { x.KategoriId, x.KategoriAdi })
            .Select(x => new MobileEgitmenKategoriSecenekResponse
            {
                // x.key demek gruplama yapılan kategori bilgisi demektir.
                
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