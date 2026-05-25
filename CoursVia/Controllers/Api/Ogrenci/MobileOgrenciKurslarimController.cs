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

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        kategoriId = kategoriId.GetValueOrDefault() > 0
            ? kategoriId
            : null;

        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        var kategoriler = await OgrenciKursKategorileriGetirAsync(kullaniciId);

        var query = _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                x.AktifMi);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.Kurs.KursAdi.Contains(arama) ||
                (x.Kurs.Aciklama != null && x.Kurs.Aciklama.Contains(arama)) ||
                x.Kurs.Egitmen.Ad.Contains(arama) ||
                x.Kurs.Egitmen.Soyad.Contains(arama) ||
                x.Kurs.KursKategorileri.Any(k => k.Kategori.KategoriAdi.Contains(arama)));
        }

        if (kategoriId.HasValue)
        {
            query = query.Where(x =>
                x.Kurs.KursKategorileri.Any(k => k.KategoriId == kategoriId.Value));
        }

        int toplamKayit = await query.CountAsync();
        int toplamSayfa = ToplamSayfaHesapla(toplamKayit, sayfaBasinaKayit);

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

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

        var tamamlananDersIdleri = await _context.DersIlerlemeleri
            .AsNoTracking()
            .Where(x =>
                x.KursKayitId == kursKayitId &&
                x.TamamlandiMi)
            .Select(x => x.DersId)
            .Distinct()
            .ToListAsync();

        var tamamlananDersSet = tamamlananDersIdleri.ToHashSet();

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

        if (yorumMetni != null && yorumMetni.Length > 1000)
        {
            return BadRequest(new MobileOgrenciIslemResponse
            {
                Basarili = false,
                Mesaj = "Yorum en fazla 1000 karakter olabilir."
            });
        }

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
            var sinavKatilimIdleri = await _context.SinavKatilimlari
                .Where(x => x.KursKayitId == kursKaydi.KursKayitId)
                .Select(x => x.SinavKatilimId)
                .ToListAsync();

            var ogrenciCevaplari = await _context.OgrenciCevaplari
                .Where(x => sinavKatilimIdleri.Contains(x.SinavKatilimId))
                .ToListAsync();

            _context.OgrenciCevaplari.RemoveRange(ogrenciCevaplari);

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
