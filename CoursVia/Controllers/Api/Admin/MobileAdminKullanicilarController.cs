using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Admin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Admin;

[ApiController]
[Route("api/mobile/admin/kullanicilar")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Admin"
)]
public class MobileAdminKullanicilarController : MobileAdminBaseController
{
    public MobileAdminKullanicilarController(AppDbContext context) : base(context)
    {
    }

    // Admin mobil kullanıcı listesini döndürür.
    // Arama, rol filtresi, aktif/pasif durum filtresi ve sayfalama destekler.
    [HttpGet]
    public async Task<ActionResult<MobileAdminKullanicilarResponse>> Kullanicilar(
        [FromQuery] string? arama,
        [FromQuery] int? rolId,
        [FromQuery] int? durumId,
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBasinaKayit = 10)
    {
        // Arama, rol, durum ve sayfalama değerleri sorgudan önce normalize edilir.
        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        rolId = rolId.GetValueOrDefault() > 0
            ? rolId
            : null;

        // Kullanıcı yönetiminde sadece Aktif / Pasif filtrelenir.
        // 1 = Aktif, 2 = Pasif
        durumId = durumId.HasValue && (durumId.Value == 1 || durumId.Value == 2)
            ? durumId
            : null;

        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        // Mobil filtre seçenekleri için rol ve durum listeleri birlikte döndürülür.
        var roller = await _context.Roller
            .AsNoTracking()
            .OrderBy(x => x.RolId)
            .Select(x => new MobileAdminSecenekResponse
            {
                Id = x.RolId,
                Ad = x.RolAdi
            })
            .ToListAsync();

        var durumlar = await _context.Durumlar
            .AsNoTracking()
            .Where(x =>
                x.DurumId == 1 ||
                x.DurumId == 2)
            .OrderBy(x => x.DurumId)
            .Select(x => new MobileAdminSecenekResponse
            {
                Id = x.DurumId,
                Ad = x.DurumAdi
            })
            .ToListAsync();

        // Kullanıcı listesi tüm kullanıcılardan başlar; filtreler aşağıda uygulanır.
        var query = _context.Kullanicilar
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            // Arama ad, soyad ve e-posta alanları üzerinden yapılır.
            query = query.Where(x =>
                x.Ad.Contains(arama) ||
                x.Soyad.Contains(arama) ||
                x.Eposta.Contains(arama));
        }

        if (rolId.HasValue)
        {
            // Rol filtresi kullanıcı-rol ilişki tablosu üzerinden uygulanır.
            query = query.Where(x =>
                x.KullaniciRolleri.Any(r => r.RolId == rolId.Value));
        }

        if (durumId.HasValue)
        {
            query = query.Where(x => x.DurumId == durumId.Value);
        }

        int toplamKayit = await query.CountAsync();
        int toplamSayfa = ToplamSayfaHesapla(toplamKayit, sayfaBasinaKayit);

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        // Liste için gereken alanlar ve roller tek projeksiyonda alınır.
        var kullaniciHamListe = await query
            .OrderByDescending(x => x.KayitTarihi)
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new
            {
                x.KullaniciId,
                x.Ad,
                x.Soyad,
                x.ProfilFotoUrl,

                x.DurumId,
                DurumAdi = x.Durum.DurumAdi,

                x.OnlineMi,

                Roller = x.KullaniciRolleri
                    .OrderBy(r => r.RolId)
                    .Select(r => r.Rol.RolAdi)
                    .ToList()
            })
            .ToListAsync();

        // Roller mobil görünüm için virgülle ayrılmış tek metne dönüştürülür.
        var kullanicilar = kullaniciHamListe
            .Select(x => new MobileAdminKullaniciItemResponse
            {
                KullaniciId = x.KullaniciId,
                AdSoyad = $"{x.Ad} {x.Soyad}".Trim(),
                ProfilFotoUrl = x.ProfilFotoUrl,

                Roller = string.Join(", ", x.Roller),

                DurumId = x.DurumId,
                DurumAdi = x.DurumAdi,

                OnlineMi = x.OnlineMi
            })
            .ToList();

        return Ok(new MobileAdminKullanicilarResponse
        {
            Basarili = true,
            Mesaj = "Kullanıcı listesi getirildi.",

            Arama = arama,
            RolId = rolId,
            DurumId = durumId,

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            SayfaBasinaKayit = sayfaBasinaKayit,
            ToplamSayfa = toplamSayfa,

            Roller = roller,
            Durumlar = durumlar,
            Kullanicilar = kullanicilar
        });
    }

    // Admin mobil kullanıcı detayını döndürür.
    // Kullanıcı eğitmense eğitmen profil bilgileri ve branşları da döner.
    // GET /api/mobile/admin/kullanicilar/{kullaniciId}
    [HttpGet("{kullaniciId:int}")]
    public async Task<ActionResult<MobileAdminKullaniciDetayResponse>> Detay(int kullaniciId)
    {
        // Kullanıcının temel bilgileri ve rolleri detay ekranı için alınır.
        var kullanici = await _context.Kullanicilar
            .AsNoTracking()
            .Where(x => x.KullaniciId == kullaniciId)
            .Select(x => new
            {
                x.KullaniciId,
                x.Ad,
                x.Soyad,
                x.Eposta,
                x.Telefon,
                x.ProfilFotoUrl,

                x.DurumId,
                DurumAdi = x.Durum.DurumAdi,

                x.OnlineMi,
                x.KayitTarihi,
                x.SonGirisTarihi,
                x.SonIpAdresi,

                Roller = x.KullaniciRolleri
                    .OrderBy(r => r.RolId)
                    .Select(r => r.Rol.RolAdi)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (kullanici == null)
        {
            return NotFound(new MobileAdminKullaniciDetayResponse
            {
                Basarili = false,
                Mesaj = "Kullanıcı bulunamadı."
            });
        }

        // Kullanıcıya ait öğrenci/eğitmen özet sayaçları detay kartlarında kullanılır.
        int kayitliKursSayisi = await _context.KursKayitlari
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.AktifMi);

        int tamamlananKursSayisi = await _context.KursKayitlari
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.AktifMi &&
                x.TamamlandiMi);

        int sertifikaSayisi = await _context.Sertifikalar
            .AsNoTracking()
            .CountAsync(x => x.KullaniciId == kullaniciId);

        // Kullanıcı eğitmen ise eğitmen kurs sayısı da detay kartında gösterilir.
        int egitmenKursSayisi = await _context.Kurslar
            .AsNoTracking()
            .CountAsync(x => x.EgitmenId == kullaniciId);

        // Kullanıcı eğitmen ise profil ve branş bilgileri de cevapta yer alır.
        var egitmenProfili = await _context.EgitmenProfilleri
            .AsNoTracking()
            .Where(x => x.KullaniciId == kullaniciId)
            .Select(x => new
            {
                x.EgitmenProfilId,
                x.DurumId,
                DurumAdi = x.Durum.DurumAdi,

                x.UzmanlikAlani,
                x.Biyografi,
                x.DeneyimYili,
                x.WebsiteUrl,

                Branslar = x.EgitmenBranslari
                    .Select(b => b.Kategori.KategoriAdi)
                    .OrderBy(b => b)
                    .ToList()
            })
            .FirstOrDefaultAsync();

        return Ok(new MobileAdminKullaniciDetayResponse
        {
            Basarili = true,
            Mesaj = "Kullanıcı detayı getirildi.",

            KullaniciId = kullanici.KullaniciId,
            AdSoyad = $"{kullanici.Ad} {kullanici.Soyad}".Trim(),
            Eposta = kullanici.Eposta,
            Telefon = kullanici.Telefon,
            ProfilFotoUrl = kullanici.ProfilFotoUrl,

            Roller = string.Join(", ", kullanici.Roller),

            DurumId = kullanici.DurumId,
            DurumAdi = kullanici.DurumAdi,

            OnlineMi = kullanici.OnlineMi,
            KayitTarihi = kullanici.KayitTarihi,
            SonGirisTarihi = kullanici.SonGirisTarihi,
            SonIpAdresi = kullanici.SonIpAdresi,

            KayitliKursSayisi = kayitliKursSayisi,
            TamamlananKursSayisi = tamamlananKursSayisi,
            SertifikaSayisi = sertifikaSayisi,
            EgitmenKursSayisi = egitmenKursSayisi,

            EgitmenProfiliVarMi = egitmenProfili != null,
            EgitmenProfilId = egitmenProfili?.EgitmenProfilId,
            EgitmenDurumId = egitmenProfili?.DurumId,
            EgitmenDurumAdi = egitmenProfili?.DurumAdi,
            UzmanlikAlani = egitmenProfili?.UzmanlikAlani,
            Biyografi = egitmenProfili?.Biyografi,
            DeneyimYili = egitmenProfili?.DeneyimYili,
            WebsiteUrl = egitmenProfili?.WebsiteUrl,
            Branslar = egitmenProfili?.Branslar ?? new List<string>()
        });
    }

    // Admin mobilde kullanıcının kayıtlı olduğu kursları döndürür.
    [HttpGet("{kullaniciId:int}/kurslar")]
    public async Task<ActionResult<MobileAdminKullaniciKurslarResponse>> KullaniciKurslari(
        int kullaniciId,
        [FromQuery] string? arama,
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBasinaKayit = 10)
    {
        // Arama ve sayfalama parametreleri kurs listesi için normalize edilir.
        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        // Önce kullanıcı varlığı kontrol edilir; yoksa boş kurs listesi yerine 404 döner.
        var kullanici = await _context.Kullanicilar
            .AsNoTracking()
            .Where(x => x.KullaniciId == kullaniciId)
            .Select(x => new
            {
                x.KullaniciId,
                x.Ad,
                x.Soyad
            })
            .FirstOrDefaultAsync();

        if (kullanici == null)
        {
            return NotFound(new MobileAdminKullaniciKurslarResponse
            {
                Basarili = false,
                Mesaj = "Kullanıcı bulunamadı."
            });
        }

        // Admin görünümünde kullanıcının aktif/pasif tüm kurs kayıtları gösterilir.
        var query = _context.KursKayitlari
            .AsNoTracking()
            .Where(x => x.KullaniciId == kullaniciId);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            // Arama kurs adı ve eğitmen adı üzerinden yapılır.
            query = query.Where(x =>
                x.Kurs.KursAdi.Contains(arama) ||
                x.Kurs.Egitmen.Ad.Contains(arama) ||
                x.Kurs.Egitmen.Soyad.Contains(arama));
        }

        int toplamKayit = await query.CountAsync();
        int toplamSayfa = ToplamSayfaHesapla(toplamKayit, sayfaBasinaKayit);

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        // Kurs kayıtları mobil listede gösterilecek özet alanlara indirgenir.
        var kursHamListe = await query
            .OrderByDescending(x => x.KayitTarihi)
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new
            {
                x.KursKayitId,
                x.KursId,

                x.Kurs.KursAdi,

                EgitmenAd = x.Kurs.Egitmen.Ad,
                EgitmenSoyad = x.Kurs.Egitmen.Soyad,

                x.KayitTarihi,
                x.AktifMi,
                x.TamamlandiMi
            })
            .ToListAsync();

        var kurslar = kursHamListe
            .Select(x => new MobileAdminKullaniciKursItemResponse
            {
                KursKayitId = x.KursKayitId,
                KursId = x.KursId,

                KursAdi = x.KursAdi,
                EgitmenAdSoyad = $"{x.EgitmenAd} {x.EgitmenSoyad}".Trim(),

                KayitTarihi = x.KayitTarihi,
                AktifMi = x.AktifMi,
                TamamlandiMi = x.TamamlandiMi
            })
            .ToList();

        return Ok(new MobileAdminKullaniciKurslarResponse
        {
            Basarili = true,
            Mesaj = "Kullanıcının kayıtlı olduğu kurslar getirildi.",

            KullaniciId = kullanici.KullaniciId,
            AdSoyad = $"{kullanici.Ad} {kullanici.Soyad}".Trim(),

            Arama = arama,

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            SayfaBasinaKayit = sayfaBasinaKayit,
            ToplamSayfa = toplamSayfa,

            Kurslar = kurslar
        });
    }
}
