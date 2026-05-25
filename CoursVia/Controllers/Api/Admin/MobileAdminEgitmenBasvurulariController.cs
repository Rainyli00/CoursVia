using System.Net;
using CoursVia.Data;
using CoursVia.Models;
using CoursVia.Services;
using CoursVia.ViewModels.Mobile.Admin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Admin;

[ApiController]
[Route("api/mobile/admin/egitmen-basvurulari")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Admin"
)]
public class MobileAdminEgitmenBasvurulariController : MobileAdminBaseController
{
    private readonly AdminLogService _adminLogService;
    private readonly EmailService _emailService;
    private readonly BildirimService _bildirimService;

    public MobileAdminEgitmenBasvurulariController(
        AppDbContext context,
        AdminLogService adminLogService,
        EmailService emailService,
        BildirimService bildirimService) : base(context)
    {
        _adminLogService = adminLogService;
        _emailService = emailService;
        _bildirimService = bildirimService;
    }

    // Admin mobil eğitmen başvurularını döndürür.
    // Asıl başvuru kaydı EgitmenProfilleri tablosudur.
    // Arama, durum filtresi ve sayfalama destekler.
    // Durumlar:
    // 4 = Onay Bekliyor
    // 6 = Reddedildi
    // 8 = Onaylandı
    // GET /api/mobile/admin/egitmen-basvurulari?arama=mehmet&durumId=4&sayfa=1&sayfaBasinaKayit=10
    [HttpGet]
    public async Task<ActionResult<MobileAdminEgitmenBasvurulariResponse>> Basvurular(
        [FromQuery] string? arama,
        [FromQuery] int? durumId,
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBasinaKayit = 10)
    {
        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        durumId = durumId.HasValue &&
                  (
                      durumId.Value == 4 ||
                      durumId.Value == 6 ||
                      durumId.Value == 8
                  )
            ? durumId
            : null;

        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        var durumlar = await _context.Durumlar
            .AsNoTracking()
            .Where(x =>
                x.DurumId == 4 ||
                x.DurumId == 6 ||
                x.DurumId == 8)
            .OrderBy(x => x.DurumId == 4 ? 1 :
                          x.DurumId == 8 ? 2 :
                          x.DurumId == 6 ? 3 : 99)
            .Select(x => new MobileAdminSecenekResponse
            {
                Id = x.DurumId,
                Ad = x.DurumAdi
            })
            .ToListAsync();

        var query = _context.EgitmenProfilleri
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.Kullanici.Ad.Contains(arama) ||
                x.Kullanici.Soyad.Contains(arama) ||
                x.Kullanici.Eposta.Contains(arama) ||
                (x.UzmanlikAlani != null && x.UzmanlikAlani.Contains(arama)));
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

        var basvurularHamListe = await query
            .OrderByDescending(x => x.EgitmenProfilId)
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new
            {
                x.EgitmenProfilId,
                x.KullaniciId,

                x.Kullanici.Ad,
                x.Kullanici.Soyad,
                x.Kullanici.Eposta,

                x.DurumId,
                DurumAdi = x.Durum.DurumAdi,

                SonIslemTarihi = _context.EgitmenOnaylari
                    .Where(o => o.KullaniciId == x.KullaniciId)
                    .OrderByDescending(o => o.IslemTarihi)
                    .Select(o => (DateTime?)o.IslemTarihi)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var basvurular = basvurularHamListe
            .Select(x => new MobileAdminEgitmenBasvuruItemResponse
            {
                EgitmenProfilId = x.EgitmenProfilId,
                KullaniciId = x.KullaniciId,

                AdSoyad = $"{x.Ad} {x.Soyad}".Trim(),
                Eposta = x.Eposta,

                DurumId = x.DurumId,
                DurumAdi = x.DurumAdi,

                SonIslemTarihi = x.SonIslemTarihi
            })
            .ToList();

        return Ok(new MobileAdminEgitmenBasvurulariResponse
        {
            Basarili = true,
            Mesaj = "Eğitmen başvuruları getirildi.",

            Arama = arama,
            DurumId = durumId,

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            SayfaBasinaKayit = sayfaBasinaKayit,
            ToplamSayfa = toplamSayfa,

            Durumlar = durumlar,
            Basvurular = basvurular
        });
    }

    // Admin mobil eğitmen başvuru detayını döndürür.
    // GET /api/mobile/admin/egitmen-basvurulari/{egitmenProfilId}
    [HttpGet("{egitmenProfilId:int}")]
    public async Task<ActionResult<MobileAdminEgitmenBasvuruDetayResponse>> Detay(int egitmenProfilId)
    {
        var basvuru = await _context.EgitmenProfilleri
            .AsNoTracking()
            .Where(x => x.EgitmenProfilId == egitmenProfilId)
            .Select(x => new
            {
                x.EgitmenProfilId,
                x.KullaniciId,

                x.Kullanici.Ad,
                x.Kullanici.Soyad,
                x.Kullanici.Eposta,
                x.Kullanici.ProfilFotoUrl,

                x.Biyografi,
                x.UzmanlikAlani,
                x.DeneyimYili,
                x.WebsiteUrl,

                x.DurumId,
                DurumAdi = x.Durum.DurumAdi,

                Branslar = x.EgitmenBranslari
                    .Select(b => b.Kategori.KategoriAdi)
                    .OrderBy(b => b)
                    .ToList(),

                SonIslemTarihi = _context.EgitmenOnaylari
                    .Where(o => o.KullaniciId == x.KullaniciId)
                    .OrderByDescending(o => o.IslemTarihi)
                    .Select(o => (DateTime?)o.IslemTarihi)
                    .FirstOrDefault(),

                Aciklama = _context.EgitmenOnaylari
                    .Where(o => o.KullaniciId == x.KullaniciId)
                    .OrderByDescending(o => o.IslemTarihi)
                    .Select(o => o.Aciklama)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (basvuru == null)
        {
            return NotFound(new MobileAdminEgitmenBasvuruDetayResponse
            {
                Basarili = false,
                Mesaj = "Eğitmen başvurusu bulunamadı."
            });
        }

        return Ok(new MobileAdminEgitmenBasvuruDetayResponse
        {
            Basarili = true,
            Mesaj = "Eğitmen başvuru detayı getirildi.",

            EgitmenProfilId = basvuru.EgitmenProfilId,
            KullaniciId = basvuru.KullaniciId,

            AdSoyad = $"{basvuru.Ad} {basvuru.Soyad}".Trim(),
            Eposta = basvuru.Eposta,
            ProfilFotoUrl = basvuru.ProfilFotoUrl,

            Biyografi = basvuru.Biyografi,
            UzmanlikAlani = basvuru.UzmanlikAlani,
            DeneyimYili = basvuru.DeneyimYili,
            WebsiteUrl = basvuru.WebsiteUrl,

            Branslar = basvuru.Branslar,

            DurumId = basvuru.DurumId,
            DurumAdi = basvuru.DurumAdi,

            SonIslemTarihi = basvuru.SonIslemTarihi,
            Aciklama = basvuru.Aciklama
        });
    }

    // Eğitmen başvurusunu onaylar.
    // EgitmenProfili.DurumId = 8 yapılır.
    // EgitmenOnayi tablosuna karar kaydı atılır.
    // Kullanıcıya Eğitmen rolü yoksa eklenir.
    // İşlem başarılı olursa kullanıcıya bilgilendirme maili gönderilir.
    // POST /api/mobile/admin/egitmen-basvurulari/{egitmenProfilId}/onayla
    [HttpPost("{egitmenProfilId:int}/onayla")]
    public async Task<ActionResult<MobileAdminIslemResponse>> Onayla(
        int egitmenProfilId,
        [FromBody] MobileAdminEgitmenBasvuruKararRequest? request)
    {
        int adminId = KullaniciIdGetir();

        var profil = await _context.EgitmenProfilleri
            .Include(x => x.Kullanici)
            .FirstOrDefaultAsync(x => x.EgitmenProfilId == egitmenProfilId);

        if (profil == null)
        {
            return NotFound(new MobileAdminIslemResponse
            {
                Basarili = false,
                Mesaj = "Eğitmen başvurusu bulunamadı."
            });
        }

        if (profil.DurumId == 8)
        {
            return BadRequest(new MobileAdminIslemResponse
            {
                Basarili = false,
                Mesaj = "Bu başvuru zaten onaylanmış."
            });
        }

        string aciklama = string.IsNullOrWhiteSpace(request?.Aciklama)
            ? "Mobil admin üzerinden eğitmen başvurusu onaylandı."
            : request.Aciklama.Trim();

        if (aciklama.Length > 1000)
        {
            return BadRequest(new MobileAdminIslemResponse
            {
                Basarili = false,
                Mesaj = "Açıklama en fazla 1000 karakter olabilir."
            });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            profil.DurumId = 8;

            bool egitmenRoluVarMi = await _context.KullaniciRolleri
                .AnyAsync(x =>
                    x.KullaniciId == profil.KullaniciId &&
                    x.RolId == 2);

            if (!egitmenRoluVarMi)
            {
                _context.KullaniciRolleri.Add(new KullaniciRol
                {
                    KullaniciId = profil.KullaniciId,
                    RolId = 2
                });
            }

            _context.EgitmenOnaylari.Add(new EgitmenOnayi
            {
                KullaniciId = profil.KullaniciId,
                AdminId = adminId,
                DurumId = 8,
                Aciklama = aciklama,
                IslemTarihi = DateTime.Now
            });

            await _bildirimService.BildirimOlusturAsync(
                profil.KullaniciId,
                "Bilgilendirme",
                "Eğitmen başvurunuz onaylandı",
                "Eğitmen başvurunuz onaylandı. Artık eğitmen panelinden kurs oluşturabilirsiniz."
            );

            await _adminLogService.KaydetAsync(
                adminId,
                AdminLogService.EgitmenBasvurulari,
                $"Mobil admin üzerinden eğitmen başvurusu onaylandı. EğitmenProfilId: {profil.EgitmenProfilId}, KullaniciId: {profil.KullaniciId}"
            );

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            return StatusCode(500, new MobileAdminIslemResponse
            {
                Basarili = false,
                Mesaj = "Eğitmen başvurusu onaylanırken bir hata oluştu."
            });
        }

        bool mailGonderildi = await OnayMailiGonderAsync(profil, adminId);

        return Ok(new MobileAdminIslemResponse
        {
            Basarili = true,
            Mesaj = mailGonderildi
                ? "Eğitmen başvurusu onaylandı ve kullanıcıya e-posta gönderildi."
                : "Eğitmen başvurusu onaylandı fakat e-posta gönderilemedi."
        });
    }

    // Eğitmen başvurusunu reddeder.
    // Red açıklaması zorunludur.
    // EgitmenProfili.DurumId = 6 yapılır.
    // EgitmenOnayi tablosuna karar kaydı atılır.
    // İşlem başarılı olursa kullanıcıya bilgilendirme maili gönderilir.
    // POST /api/mobile/admin/egitmen-basvurulari/{egitmenProfilId}/reddet
    [HttpPost("{egitmenProfilId:int}/reddet")]
    public async Task<ActionResult<MobileAdminIslemResponse>> Reddet(
        int egitmenProfilId,
        [FromBody] MobileAdminEgitmenBasvuruKararRequest? request)
    {
        int adminId = KullaniciIdGetir();

        string? aciklama = string.IsNullOrWhiteSpace(request?.Aciklama)
            ? null
            : request.Aciklama.Trim();

        if (string.IsNullOrWhiteSpace(aciklama))
        {
            return BadRequest(new MobileAdminIslemResponse
            {
                Basarili = false,
                Mesaj = "Red açıklaması zorunludur."
            });
        }

        if (aciklama.Length > 1000)
        {
            return BadRequest(new MobileAdminIslemResponse
            {
                Basarili = false,
                Mesaj = "Red açıklaması en fazla 1000 karakter olabilir."
            });
        }

        var profil = await _context.EgitmenProfilleri
            .Include(x => x.Kullanici)
            .FirstOrDefaultAsync(x => x.EgitmenProfilId == egitmenProfilId);

        if (profil == null)
        {
            return NotFound(new MobileAdminIslemResponse
            {
                Basarili = false,
                Mesaj = "Eğitmen başvurusu bulunamadı."
            });
        }

        if (profil.DurumId == 8)
        {
            return BadRequest(new MobileAdminIslemResponse
            {
                Basarili = false,
                Mesaj = "Onaylanmış başvuru reddedilemez."
            });
        }

        if (profil.DurumId == 6)
        {
            return BadRequest(new MobileAdminIslemResponse
            {
                Basarili = false,
                Mesaj = "Bu başvuru zaten reddedilmiş."
            });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            profil.DurumId = 6;

            _context.EgitmenOnaylari.Add(new EgitmenOnayi
            {
                KullaniciId = profil.KullaniciId,
                AdminId = adminId,
                DurumId = 6,
                Aciklama = aciklama,
                IslemTarihi = DateTime.Now
            });

            string bildirimMesaji = string.IsNullOrWhiteSpace(aciklama)
                ? "Eğitmen başvurunuz admin tarafından reddedildi."
                : $"Eğitmen başvurunuz admin tarafından reddedildi. Red sebebi: {aciklama}";

            await _bildirimService.BildirimOlusturAsync(
                profil.KullaniciId,
                "Uyarı",
                "Eğitmen başvurunuz reddedildi",
                bildirimMesaji
            );

            await _adminLogService.KaydetAsync(
                adminId,
                AdminLogService.EgitmenBasvurulari,
                $"Mobil admin üzerinden eğitmen başvurusu reddedildi. EğitmenProfilId: {profil.EgitmenProfilId}, KullaniciId: {profil.KullaniciId}. Açıklama: {aciklama}"
            );

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();

            return StatusCode(500, new MobileAdminIslemResponse
            {
                Basarili = false,
                Mesaj = "Eğitmen başvurusu reddedilirken bir hata oluştu."
            });
        }

        bool mailGonderildi = await RedMailiGonderAsync(profil, aciklama, adminId);

        return Ok(new MobileAdminIslemResponse
        {
            Basarili = true,
            Mesaj = mailGonderildi
                ? "Eğitmen başvurusu reddedildi ve kullanıcıya e-posta gönderildi."
                : "Eğitmen başvurusu reddedildi fakat e-posta gönderilemedi."
        });
    }

    private async Task<bool> OnayMailiGonderAsync(EgitmenProfili profil, int adminId)
    {
        try
        {
            string adSoyad = $"{profil.Kullanici.Ad} {profil.Kullanici.Soyad}".Trim();

            await _emailService.SendEmailAsync(
                profil.Kullanici.Eposta,
                "CoursVia Eğitmen Başvurunuz Onaylandı",
                OnayMailHtmlOlustur(adSoyad)
            );

            return true;
        }
        catch (Exception ex)
        {
            await _adminLogService.KaydetAsync(
                adminId,
                AdminLogService.SistemKullanici,
                $"Eğitmen başvurusu onaylandı ancak onay maili gönderilemedi. EğitmenProfilId: {profil.EgitmenProfilId}, KullaniciId: {profil.KullaniciId}. Hata: {ex.Message}"
            );

            await _context.SaveChangesAsync();

            return false;
        }
    }

    private async Task<bool> RedMailiGonderAsync(EgitmenProfili profil, string redAciklamasi, int adminId)
    {
        try
        {
            string adSoyad = $"{profil.Kullanici.Ad} {profil.Kullanici.Soyad}".Trim();

            await _emailService.SendEmailAsync(
                profil.Kullanici.Eposta,
                "CoursVia Eğitmen Başvurunuz Reddedildi",
                RedMailHtmlOlustur(adSoyad, redAciklamasi)
            );

            return true;
        }
        catch (Exception ex)
        {
            await _adminLogService.KaydetAsync(
                adminId,
                AdminLogService.SistemKullanici,
                $"Eğitmen başvurusu reddedildi ancak red maili gönderilemedi. EğitmenProfilId: {profil.EgitmenProfilId}, KullaniciId: {profil.KullaniciId}. Hata: {ex.Message}"
            );

            await _context.SaveChangesAsync();

            return false;
        }
    }

    private static string OnayMailHtmlOlustur(string adSoyad)
    {
        string guvenliAdSoyad = WebUtility.HtmlEncode(adSoyad);

        return $@"
<div style=""font-family:Arial,sans-serif;background:#f8fafc;padding:24px;"">
    <div style=""max-width:620px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:16px;padding:24px;"">
        <h2 style=""margin:0 0 12px;color:#0f172a;"">Eğitmen Başvurunuz Onaylandı</h2>

        <p style=""font-size:15px;color:#334155;line-height:1.6;"">
            Merhaba <strong>{guvenliAdSoyad}</strong>,
        </p>

        <p style=""font-size:15px;color:#334155;line-height:1.6;"">
            CoursVia eğitmen başvurunuz incelendi ve onaylandı.
            Artık eğitmen paneline erişebilir, kurs oluşturabilir ve öğrencilerinizin ilerlemesini takip edebilirsiniz.
        </p>

        <div style=""margin:20px 0;padding:14px;border-radius:12px;background:#dcfce7;color:#166534;font-weight:600;"">
            Başvuru durumunuz: Onaylandı
        </div>

        <p style=""font-size:13px;color:#64748b;line-height:1.6;"">
            Bu e-posta CoursVia sistemi tarafından otomatik gönderilmiştir.
        </p>
    </div>
</div>";
    }

    private static string RedMailHtmlOlustur(string adSoyad, string redAciklamasi)
    {
        string guvenliAdSoyad = WebUtility.HtmlEncode(adSoyad);
        string guvenliAciklama = WebUtility.HtmlEncode(redAciklamasi);

        return $@"
<div style=""font-family:Arial,sans-serif;background:#f8fafc;padding:24px;"">
    <div style=""max-width:620px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:16px;padding:24px;"">
        <h2 style=""margin:0 0 12px;color:#0f172a;"">Eğitmen Başvurunuz Reddedildi</h2>

        <p style=""font-size:15px;color:#334155;line-height:1.6;"">
            Merhaba <strong>{guvenliAdSoyad}</strong>,
        </p>

        <p style=""font-size:15px;color:#334155;line-height:1.6;"">
            CoursVia eğitmen başvurunuz incelendi ve şu an için onaylanmadı.
        </p>

        <div style=""margin:20px 0;padding:14px;border-radius:12px;background:#fee2e2;color:#991b1b;font-weight:600;"">
            Başvuru durumunuz: Reddedildi
        </div>

        <div style=""margin:18px 0;padding:14px;border-radius:12px;background:#f8fafc;border:1px solid #e5e7eb;"">
            <div style=""font-size:13px;color:#64748b;font-weight:700;margin-bottom:6px;"">Red Açıklaması</div>
            <div style=""font-size:15px;color:#334155;line-height:1.6;"">{guvenliAciklama}</div>
        </div>

        <p style=""font-size:13px;color:#64748b;line-height:1.6;"">
            Bu e-posta CoursVia sistemi tarafından otomatik gönderilmiştir.
        </p>
    </div>
</div>";
    }
}
