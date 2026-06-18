using CoursVia.Data;
using CoursVia.Models;
using CoursVia.ViewModels.Mobile.Ogrenci;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Ogrenci;

[ApiController]
[Route("api/mobile/ogrenci/kurslarim")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Öğrenci"
)]
public class MobileOgrenciKurslarimController : MobileOgrenciBaseController
{
    public MobileOgrenciKurslarimController(AppDbContext context) : base(context)
    {
    }

    // Öğrencinin kayıtlı olduğu aktif kursları listeler.
    // GET /api/mobile/ogrenci/kurslarim
    [HttpGet]
    public async Task<ActionResult<MobileOgrenciKurslarimResponse>> Kurslarim(
        
        [FromQuery] string? arama,
        [FromQuery] int? kategoriId,
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBasinaKayit = 10)
    {
        int kullaniciId = KullaniciIdGetir();

        // Arama, kategori ve sayfalama değerleri mobil istek için normalize edilir.
        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        kategoriId = kategoriId.GetValueOrDefault() > 0
            ? kategoriId
            : null;

        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        // Filtre dropdown'ı için öğrencinin kayıtlı olduğu kurs kategorileri hazırlanır.
        var kategoriler = await OgrenciKursKategorileriGetirAsync(kullaniciId);

        // Öğrenci sadece kendi aktif kurs kayıtlarını görebilir.
        var query = _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                x.AktifMi);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            // Arama kurs, eğitmen ve kategori adları üzerinden yapılır.
            query = query.Where(x =>
                x.Kurs.KursAdi.Contains(arama) ||
                (x.Kurs.Aciklama != null && x.Kurs.Aciklama.Contains(arama)) ||
                x.Kurs.Egitmen.Ad.Contains(arama) ||
                x.Kurs.Egitmen.Soyad.Contains(arama) ||
                x.Kurs.KursKategorileri.Any(k => k.Kategori.KategoriAdi.Contains(arama)));
        }

        if (kategoriId.HasValue)
        {
            // Kategori filtresi kurs-kategori ilişkisi üzerinden uygulanır.
            query = query.Where(x =>
                x.Kurs.KursKategorileri.Any(k => k.KategoriId == kategoriId.Value));
        }

        int toplamKayit = await query.CountAsync();
        int toplamSayfa = ToplamSayfaHesapla(toplamKayit, sayfaBasinaKayit);

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        // Liste DTO'su, ilerleme ve öğrencinin kendi değerlendirme bilgileriyle birlikte hazırlanır.
        var kurslar = await query
            .OrderByDescending(x => x.KayitTarihi)
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new MobileOgrenciKursItemResponse
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

                Kategoriler = x.Kurs.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList(),

                KayitTarihi = x.KayitTarihi,

                ToplamDersSayisi = x.Kurs.Dersler
                    .Count(d =>
                        d.AktifMi &&
                        !d.SistemDersiMi),

                TamamlananDersSayisi = x.DersIlerlemeleri
                    .Count(i =>
                        i.TamamlandiMi &&
                        i.Ders.AktifMi &&
                        !i.Ders.SistemDersiMi),

                KursTamamlandiMi = x.TamamlandiMi,
                TamamlanmaTarihi = x.TamamlanmaTarihi,

                DegerlendirmeVarMi = _context.KursDegerlendirmeleri.Any(d =>
                    d.KullaniciId == kullaniciId &&
                    d.KursId == x.KursId),

                KendiPuan = _context.KursDegerlendirmeleri
                    .Where(d =>
                        d.KullaniciId == kullaniciId &&
                        d.KursId == x.KursId)
                    .Select(d => (int?)d.Puan)
                    .FirstOrDefault(),

                KendiYorumMetni = _context.KursDegerlendirmeleri
                    .Where(d =>
                        d.KullaniciId == kullaniciId &&
                        d.KursId == x.KursId)
                    .Select(d => d.YorumMetni)
                    .FirstOrDefault()
            })
            .ToListAsync();

        foreach (var kurs in kurslar)
        {
            // İlerleme yüzdesi filtrelenmiş aktif dersler üzerinden hesaplanır.
            kurs.EgitmenAdSoyad = kurs.EgitmenAdSoyad.Trim();

            kurs.IlerlemeYuzdesi = kurs.ToplamDersSayisi == 0
                ? 0
                : (int)Math.Round(kurs.TamamlananDersSayisi * 100.0 / kurs.ToplamDersSayisi);
        }

        return Ok(new MobileOgrenciKurslarimResponse
        {
            Basarili = true,
            Mesaj = "Öğrencinin kayıtlı kursları getirildi.",
            Arama = arama,
            KategoriId = kategoriId,
            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            SayfaBasinaKayit = sayfaBasinaKayit,
            ToplamSayfa = toplamSayfa,
            Kategoriler = kategoriler,
            Kurslar = kurslar
        });
    }

    // Öğrencinin kayıtlı olduğu bir kursun detaylı ilerleme bilgisini döndürür.
    // Ders altında materyal adı ve tipi bilgisi gelir.
    // GET /api/mobile/ogrenci/kurslarim/{kursKayitId}
    [HttpGet("{kursKayitId:int}")]
    public async Task<ActionResult<MobileOgrenciKursDetayResponse>> KursDetay(int kursKayitId)
    {
        int kullaniciId = KullaniciIdGetir();

        // Kurs kaydı kullanıcı id ile filtrelenir; başka öğrencinin kaydına erişim engellenir.
        var kursKaydi = await _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.KursKayitId == kursKayitId &&
                x.KullaniciId == kullaniciId &&
                x.AktifMi)
            .Select(x => new
            {
                x.KursKayitId,
                x.KursId,
                x.KayitTarihi,
                x.TamamlandiMi,

                x.Kurs.KursAdi,
                x.Kurs.Aciklama,
                x.Kurs.KapakGorselUrl,
                x.Kurs.DurumId,
                DurumAdi = x.Kurs.Durum.DurumAdi,

                EgitmenAdSoyad = x.Kurs.Egitmen.Ad + " " + x.Kurs.Egitmen.Soyad,

                Kategoriler = x.Kurs.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList(),

                ToplamDersSayisi = x.Kurs.Dersler
                    .Count(d =>
                        d.AktifMi &&
                        !d.SistemDersiMi),

                TamamlananDersSayisi = x.DersIlerlemeleri
                    .Count(i =>
                        i.TamamlandiMi &&
                        i.Ders.AktifMi &&
                        !i.Ders.SistemDersiMi),

                DegerlendirmeVarMi = _context.KursDegerlendirmeleri.Any(d =>
                    d.KullaniciId == kullaniciId &&
                    d.KursId == x.KursId),

                KendiPuan = _context.KursDegerlendirmeleri
                    .Where(d =>
                        d.KullaniciId == kullaniciId &&
                        d.KursId == x.KursId)
                    .Select(d => (int?)d.Puan)
                    .FirstOrDefault(),

                KendiYorumMetni = _context.KursDegerlendirmeleri
                    .Where(d =>
                        d.KullaniciId == kullaniciId &&
                        d.KursId == x.KursId)
                    .Select(d => d.YorumMetni)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (kursKaydi == null)
        {
            return NotFound(new MobileOgrenciKursDetayResponse
            {
                Basarili = false,
                Mesaj = "Kurs kaydı bulunamadı."
            });
        }

        // Güncellenen kurslarda içerik açılmaz, sadece mevcut durum bilgisi döndürülür.
        if (kursKaydi.DurumId == 7)
        {
            return BadRequest(new MobileOgrenciKursDetayResponse
            {
                Basarili = false,
                Mesaj = "Bu kurs şu anda güncelleniyor. Güncelleme tamamlandığında tekrar devam edebilirsiniz.",
                KursKayitId = kursKaydi.KursKayitId,
                KursId = kursKaydi.KursId,
                KursAdi = kursKaydi.KursAdi,
                KapakGorselUrl = kursKaydi.KapakGorselUrl,
                EgitmenAdSoyad = kursKaydi.EgitmenAdSoyad.Trim(),
                DurumId = kursKaydi.DurumId,
                DurumAdi = kursKaydi.DurumAdi,
                GuncelleniyorMu = true,
                DevamEdilebilirMi = false,
                KayitTarihi = kursKaydi.KayitTarihi,
                KursTamamlandiMi = kursKaydi.TamamlandiMi
            });
        }

        // Tamamlanan ders id'leri HashSet'e çevrilerek ders listesinde hızlı işaretleme yapılır.
        var tamamlananDersIdleri = await _context.DersIlerlemeleri
            .AsNoTracking()
            .Where(x =>
                x.KursKayitId == kursKayitId &&
                x.TamamlandiMi)
            .Select(x => x.DersId)
            .Distinct()
            .ToListAsync();

        var tamamlananDersSet = tamamlananDersIdleri.ToHashSet();

        // Bölüm, ders ve materyaller mobil detay ekranının beklediği sırayla hazırlanır.
        var bolumler = await _context.Bolumler
            .AsNoTracking()
            .Where(x => x.KursId == kursKaydi.KursId)
            .OrderBy(x => x.SiraNo)
            .Select(x => new MobileOgrenciBolumItemResponse
            {
                BolumId = x.BolumId,
                BolumAdi = x.BolumAdi,
                SiraNo = x.SiraNo,

                Dersler = x.Dersler
                    .Where(d =>
                        d.AktifMi &&
                        !d.SistemDersiMi)
                    .OrderBy(d => d.SiraNo)
                    .Select(d => new MobileOgrenciDersItemResponse
                    {
                        DersId = d.DersId,
                        DersAdi = d.DersAdi,
                        SiraNo = d.SiraNo,

                        MateryalVarMi = d.DersMateryalleri.Any(),
                        MateryalSayisi = d.DersMateryalleri.Count(),

                        Materyaller = d.DersMateryalleri
                            .OrderBy(m => m.YuklenmeTarihi)
                            .Select(m => new MobileOgrenciDersMateryalItemResponse
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

        foreach (var bolum in bolumler)
        {
            // Her ders için tamamlanma durumu ve bölüm bazlı ilerleme yüzdesi hesaplanır.
            foreach (var ders in bolum.Dersler)
            {
                ders.TamamlandiMi = tamamlananDersSet.Contains(ders.DersId);
            }

            bolum.ToplamDersSayisi = bolum.Dersler.Count;
            bolum.TamamlananDersSayisi = bolum.Dersler.Count(x => x.TamamlandiMi);

            bolum.IlerlemeYuzdesi = bolum.ToplamDersSayisi == 0
                ? 0
                : (int)Math.Round(bolum.TamamlananDersSayisi * 100.0 / bolum.ToplamDersSayisi);
        }

        // Genel kurs ilerlemesi toplam aktif ders sayısına göre hesaplanır.
        int genelIlerlemeYuzdesi = kursKaydi.ToplamDersSayisi == 0
            ? 0
            : (int)Math.Round(kursKaydi.TamamlananDersSayisi * 100.0 / kursKaydi.ToplamDersSayisi);

        return Ok(new MobileOgrenciKursDetayResponse
        {
            Basarili = true,
            Mesaj = "Kurs ilerleme detayı getirildi.",

            KursKayitId = kursKaydi.KursKayitId,
            KursId = kursKaydi.KursId,

            KursAdi = kursKaydi.KursAdi,
            Aciklama = kursKaydi.Aciklama,
            KapakGorselUrl = kursKaydi.KapakGorselUrl,

            EgitmenAdSoyad = kursKaydi.EgitmenAdSoyad.Trim(),

            DurumId = kursKaydi.DurumId,
            DurumAdi = kursKaydi.DurumAdi,
            GuncelleniyorMu = kursKaydi.DurumId == 7,
            DevamEdilebilirMi = kursKaydi.DurumId == 5,

            Kategoriler = kursKaydi.Kategoriler,

            KayitTarihi = kursKaydi.KayitTarihi,

            ToplamDersSayisi = kursKaydi.ToplamDersSayisi,
            TamamlananDersSayisi = kursKaydi.TamamlananDersSayisi,
            IlerlemeYuzdesi = genelIlerlemeYuzdesi,
            KursTamamlandiMi = kursKaydi.TamamlandiMi,

            DegerlendirmeVarMi = kursKaydi.DegerlendirmeVarMi,
            KendiPuan = kursKaydi.KendiPuan,
            KendiYorumMetni = kursKaydi.KendiYorumMetni,

            Bolumler = bolumler
        });
    }

    // Öğrencinin kayıtlı olduğu kursa puan ve yorum vermesini sağlar.
    // POST /api/mobile/ogrenci/kurslarim/{kursKayitId}/degerlendir

    [HttpPost("{kursKayitId:int}/degerlendir")]
    public async Task<ActionResult<MobileOgrenciIslemResponse>> Degerlendir(
        int kursKayitId,
        [FromBody] MobileOgrenciDegerlendirRequest request)
    {
        int kullaniciId = KullaniciIdGetir();

        // Mobil puanlama 1-5 aralığıyla sınırlıdır.
        if (request.Puan < 1 || request.Puan > 5)
        {
            return BadRequest(new MobileOgrenciIslemResponse
            {
                Basarili = false,
                Mesaj = "Puan 1 ile 5 arasında olmalıdır."
            });
        }

        string? yorumMetni = string.IsNullOrWhiteSpace(request.YorumMetni)
            ? null
            : request.YorumMetni.Trim();

        // Çok uzun yorumlar hem veri tabanı hem de mobil görünüm için reddedilir.
        if (yorumMetni != null && yorumMetni.Length > 1000)
        {
            return BadRequest(new MobileOgrenciIslemResponse
            {
                Basarili = false,
                Mesaj = "Yorum en fazla 1000 karakter olabilir."
            });
        }

        // Değerlendirme sadece öğrencinin aktif olarak kayıtlı olduğu kursa yapılabilir.
        var kursKaydi = await _context.KursKayitlari
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.KursKayitId == kursKayitId &&
                x.KullaniciId == kullaniciId &&
                x.AktifMi);

        if (kursKaydi == null)
        {
            return NotFound(new MobileOgrenciIslemResponse
            {
                Basarili = false,
                Mesaj = "Kurs kaydı bulunamadı."
            });
        }

        // Daha önce değerlendirme varsa güncellenir, yoksa yeni kayıt oluşturulur.
        var mevcutDegerlendirme = await _context.KursDegerlendirmeleri
            .FirstOrDefaultAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == kursKaydi.KursId);

        if (mevcutDegerlendirme == null)
        {
            _context.KursDegerlendirmeleri.Add(new KursDegerlendirmesi
            {
                KullaniciId = kullaniciId,
                KursId = kursKaydi.KursId,
                Puan = (byte)request.Puan,
                YorumMetni = yorumMetni,
                DegerlendirmeTarihi = DateTime.Now
            });
        }
        else
        {
            mevcutDegerlendirme.Puan = (byte)request.Puan;
            mevcutDegerlendirme.YorumMetni = yorumMetni;
            mevcutDegerlendirme.DegerlendirmeTarihi = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return Ok(new MobileOgrenciIslemResponse
        {
            Basarili = true,
            Mesaj = "Kurs değerlendirmeniz kaydedildi."
        });
    }

    // Öğrencinin kurs kaydını iptal eder.
    // POST /api/mobile/ogrenci/kurslarim/{kursKayitId}/kayit-iptal
    [HttpPost("{kursKayitId:int}/kayit-iptal")]
    public async Task<ActionResult<MobileOgrenciIslemResponse>> KayitIptal(int kursKayitId)
    {
        int kullaniciId = KullaniciIdGetir();

        // Kayıt iptali yalnızca öğrencinin kendi aktif kurs kaydı için yapılabilir.
        var kursKaydi = await _context.KursKayitlari
            .FirstOrDefaultAsync(x =>
                x.KursKayitId == kursKayitId &&
                x.KullaniciId == kullaniciId &&
                x.AktifMi);

        if (kursKaydi == null)
        {
            return NotFound(new MobileOgrenciIslemResponse
            {
                Basarili = false,
                Mesaj = "Kurs kaydı bulunamadı."
            });
        }

        int kursId = kursKaydi.KursId;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Önce sınav cevapları silinir; katılım kayıtları bu cevaplara bağlıdır.
            var sinavKatilimIdleri = await _context.SinavKatilimlari
                .Where(x => x.KursKayitId == kursKaydi.KursKayitId)
                .Select(x => x.SinavKatilimId)
                .ToListAsync();

            var ogrenciCevaplari = await _context.OgrenciCevaplari
                .Where(x => sinavKatilimIdleri.Contains(x.SinavKatilimId))
                .ToListAsync();

            _context.OgrenciCevaplari.RemoveRange(ogrenciCevaplari);

            // Kursa ait sınav katılım, ilerleme, sertifika ve kişisel kayıtlar temizlenir.
            var sinavKatilimlari = await _context.SinavKatilimlari
                .Where(x => x.KursKayitId == kursKaydi.KursKayitId)
                .ToListAsync();

            _context.SinavKatilimlari.RemoveRange(sinavKatilimlari);

            var dersIlerlemeleri = await _context.DersIlerlemeleri
                .Where(x => x.KursKayitId == kursKaydi.KursKayitId)
                .ToListAsync();

            _context.DersIlerlemeleri.RemoveRange(dersIlerlemeleri);

            var sertifikalar = await _context.Sertifikalar
                .Where(x =>
                    x.KullaniciId == kullaniciId &&
                    x.KursId == kursId)
                .ToListAsync();

            _context.Sertifikalar.RemoveRange(sertifikalar);

            var favoriler = await _context.Favoriler
                .Where(x =>
                    x.KullaniciId == kullaniciId &&
                    x.KursId == kursId)
                .ToListAsync();

            _context.Favoriler.RemoveRange(favoriler);

            var degerlendirmeler = await _context.KursDegerlendirmeleri
                .Where(x =>
                    x.KullaniciId == kullaniciId &&
                    x.KursId == kursId)
                .ToListAsync();

            _context.KursDegerlendirmeleri.RemoveRange(degerlendirmeler);

            // En son ana kurs kaydı silinir.
            _context.KursKayitlari.Remove(kursKaydi);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new MobileOgrenciIslemResponse
            {
                Basarili = true,
                Mesaj = "Kurs kaydı ve bu kursa ait ilerleme bilgileriniz silindi."
            });
        }
        catch
        {
            await transaction.RollbackAsync();

            return StatusCode(500, new MobileOgrenciIslemResponse
            {
                Basarili = false,
                Mesaj = "Kurs kaydı iptal edilirken bir hata oluştu."
            });
        }
    }

    // Öğrencinin kayıtlı kurs kategorilerini getirir.
    private async Task<List<MobileOgrenciKategoriSecenekResponse>> OgrenciKursKategorileriGetirAsync(int kullaniciId)
    {
        // Öğrencinin aktif kurslarındaki kategoriler gruplanarak filtre seçenekleri oluşturulur.
        var kategoriler = await _context.KursKategorileri
            .AsNoTracking()
            .Where(x => _context.KursKayitlari.Any(kayit =>
                kayit.KullaniciId == kullaniciId &&
                kayit.AktifMi &&
                kayit.KursId == x.KursId))
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
                // Kategori bazında kaç farklı kursa kayıtlı olduğunu sayarak kayıt sayısı bilgisi hazırlanır.
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
