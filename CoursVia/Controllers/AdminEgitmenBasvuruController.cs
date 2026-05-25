using CoursVia.Data;
using CoursVia.Models;
using CoursVia.Services;
using CoursVia.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;

namespace CoursVia.Controllers;

[Authorize(Roles = "Admin")]
public class AdminEgitmenBasvuruController : Controller
{
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;
    private readonly AdminLogService _adminLogService;
    private readonly BildirimService _bildirimService;

    public AdminEgitmenBasvuruController(
        AppDbContext context,
        EmailService emailService,
        AdminLogService adminLogService,
        BildirimService bildirimService)
    {
        _context = context;
        _emailService = emailService;
        _adminLogService = adminLogService;
        _bildirimService = bildirimService;

    }

    [HttpGet]
    public async Task<IActionResult> Basvurular(string? arama, string durum = "bekleyen", int sayfa = 1)
    {
        const int sayfaBasinaKayit = 8;

        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        durum = string.IsNullOrWhiteSpace(durum)
            ? "bekleyen"
            : durum.Trim().ToLower();

        if (sayfa < 1)
        {
            sayfa = 1;
        }

        int? durumId = durum switch
        {
            "bekleyen" => 4,
            "onaylanan" => 8,
            "reddedilen" => 6,
            "tum" => null,
            _ => 4
        };

        var query = _context.EgitmenProfilleri
            .AsNoTracking()
            .Include(x => x.Kullanici)
            .Include(x => x.Durum)
            .Include(x => x.EgitmenBranslari)
                .ThenInclude(x => x.Kategori)
            .AsQueryable();

        if (durumId.HasValue)
        {
            query = query.Where(x => x.DurumId == durumId.Value);
        }

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.Kullanici.Ad.Contains(arama) ||
                x.Kullanici.Soyad.Contains(arama) ||
                x.Kullanici.Eposta.Contains(arama) ||
                (x.Kullanici.Telefon != null && x.Kullanici.Telefon.Contains(arama)) ||
                (x.UzmanlikAlani != null && x.UzmanlikAlani.Contains(arama)));
        }

        int toplamKayit = await query.CountAsync();

        int toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);

        if (toplamSayfa < 1)
        {
            toplamSayfa = 1;
        }

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        var basvurular = await query
            .OrderByDescending(x => x.EgitmenProfilId)
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new EgitmenOnayListeItemViewModel
            {
                EgitmenProfilId = x.EgitmenProfilId,
                KullaniciId = x.KullaniciId,

                AdSoyad = x.Kullanici.Ad + " " + x.Kullanici.Soyad,
                Eposta = x.Kullanici.Eposta,
                Telefon = x.Kullanici.Telefon,
                ProfilFotoUrl = x.Kullanici.ProfilFotoUrl,

                UzmanlikAlani = x.UzmanlikAlani,
                Biyografi = x.Biyografi,
                DeneyimYili = x.DeneyimYili,
                WebsiteUrl = x.WebsiteUrl,

                DurumId = x.DurumId,
                DurumAdi = x.Durum.DurumAdi,

                Branslar = x.EgitmenBranslari
                    .Select(b => b.Kategori.KategoriAdi)
                    .OrderBy(b => b)
                    .ToList()
            })
            .ToListAsync();

        var model = new EgitmenOnaylariViewModel
        {
            Arama = arama,
            Durum = durum,

            Basvurular = basvurular,

            BekleyenSayisi = await _context.EgitmenProfilleri
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 4),

            OnaylananSayisi = await _context.EgitmenProfilleri
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 8),

            ReddedilenSayisi = await _context.EgitmenProfilleri
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 6),

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            ToplamSayfa = toplamSayfa,
            SayfaBasinaKayit = sayfaBasinaKayit
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Karar(
       int egitmenProfilId,
       string karar,
       string? aciklama)
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        karar = string.IsNullOrWhiteSpace(karar)
            ? string.Empty
            : karar.Trim().ToLower();

        int yeniDurumId = karar switch
        {
            "onayla" => 8,
            "reddet" => 6,
            _ => 0
        };

        if (yeniDurumId == 0)
        {
            TempData["AdminHata"] = "Geçersiz işlem.";
            return RedirectToAction(nameof(Basvurular));
        }

        aciklama = string.IsNullOrWhiteSpace(aciklama)
            ? null
            : aciklama.Trim();

        if (karar == "reddet")
        {
            if (string.IsNullOrWhiteSpace(aciklama))
            {
                TempData["AdminHata"] = "Reddetme işleminde red sebebi zorunludur.";
                return RedirectToAction(nameof(Basvurular));
            }

            if (aciklama.Length < 5)
            {
                TempData["AdminHata"] = "Red sebebi en az 5 karakter olmalıdır.";
                return RedirectToAction(nameof(Basvurular));
            }
        }

        var egitmenProfili = await _context.EgitmenProfilleri
            .Include(x => x.Kullanici)
            .FirstOrDefaultAsync(x => x.EgitmenProfilId == egitmenProfilId);

        if (egitmenProfili == null)
        {
            TempData["AdminHata"] = "Eğitmen başvurusu bulunamadı.";
            return RedirectToAction(nameof(Basvurular));
        }

        if (egitmenProfili.DurumId != 4)
        {
            TempData["AdminHata"] = "Sadece onay bekleyen başvurular için karar verilebilir.";
            return RedirectToAction(nameof(Basvurular));
        }

        string egitmenAdSoyad = $"{egitmenProfili.Kullanici.Ad} {egitmenProfili.Kullanici.Soyad}".Trim();
        string egitmenEposta = egitmenProfili.Kullanici.Eposta;

        string islemMetni = karar switch
        {
            "onayla" => "Eğitmen başvurusu onaylandı",
            "reddet" => "Eğitmen başvurusu reddedildi",
            _ => "Eğitmen başvurusu güncellendi"
        };

        bool islemBasarili = false;

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            egitmenProfili.DurumId = yeniDurumId;

            _context.EgitmenOnaylari.Add(new EgitmenOnayi
            {
                KullaniciId = egitmenProfili.KullaniciId,
                AdminId = adminId,
                DurumId = yeniDurumId,
                Aciklama = aciklama,
                IslemTarihi = DateTime.Now
            });

            if (karar == "onayla")
            {
                bool egitmenRoluVar = await _context.KullaniciRolleri
                    .AnyAsync(x =>
                        x.KullaniciId == egitmenProfili.KullaniciId &&
                        x.RolId == 2);

                if (!egitmenRoluVar)
                {
                    _context.KullaniciRolleri.Add(new KullaniciRol
                    {
                        KullaniciId = egitmenProfili.KullaniciId,
                        RolId = 2
                    });
                }

                await _bildirimService.BildirimOlusturAsync(
                    egitmenProfili.KullaniciId,
                    "Bilgilendirme",
                    "Eğitmen başvurunuz onaylandı",
                    "Eğitmen başvurunuz onaylandı. Artık eğitmen panelinden kurs oluşturabilirsiniz."
                );
            }
            else if (karar == "reddet")
            {
                string bildirimMesaji = string.IsNullOrWhiteSpace(aciklama)
                    ? "Eğitmen başvurunuz admin tarafından reddedildi."
                    : $"Eğitmen başvurunuz admin tarafından reddedildi. Red sebebi: {aciklama}";

                await _bildirimService.BildirimOlusturAsync(
                    egitmenProfili.KullaniciId,
                    "Uyarı",
                    "Eğitmen başvurunuz reddedildi",
                    bildirimMesaji
                );
            }

            await _adminLogService.KaydetAsync(
                adminId,
                AdminLogService.EgitmenBasvurulari,
                $"{egitmenAdSoyad} - {islemMetni}");

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            islemBasarili = true;

            TempData["AdminBasari"] = islemMetni + ".";
        }
        catch
        {
            await transaction.RollbackAsync();

            TempData["AdminHata"] = "İşlem sırasında beklenmeyen bir hata oluştu.";
        }

        if (islemBasarili)
        {
            try
            {
                await EgitmenBasvuruMailGonderAsync(
                    egitmenEposta,
                    egitmenAdSoyad,
                    karar,
                    aciklama);

                TempData["AdminBasari"] = islemMetni + ". Bilgilendirme maili gönderildi.";
            }
            catch
            {
                TempData["AdminBasari"] = islemMetni + ". Bilgilendirme maili gönderilemedi.";
            }
        }

        return RedirectToAction(nameof(Basvurular));
    }



    private async Task EgitmenBasvuruMailGonderAsync(
        string toEmail,
        string adSoyad,
        string karar,
        string? aciklama)
    {
        string guvenliAdSoyad = WebUtility.HtmlEncode(adSoyad);
        string guvenliAciklama = WebUtility.HtmlEncode(aciklama ?? string.Empty);

        string subject = karar switch
        {
            "onayla" => "CoursVia Eğitmen Başvurunuz Onaylandı",
            "reddet" => "CoursVia Eğitmen Başvurunuz Reddedildi",
            _ => "CoursVia Eğitmen Başvurusu"
        };

        string baslik = karar switch
        {
            "onayla" => "Eğitmen başvurunuz onaylandı.",
            "reddet" => "Eğitmen başvurunuz reddedildi.",
            _ => "Eğitmen başvurunuz güncellendi."
        };

        string mesaj = karar switch
        {
            "onayla" => "CoursVia üzerinde eğitmen hesabınız aktif hale getirildi. Artık eğitmen paneline giriş yaparak kurs oluşturabilirsiniz.",
            "reddet" => "Başvurunuz yapılan değerlendirme sonucunda uygun bulunmadı.",
            _ => "Başvurunuzla ilgili durum güncellendi."
        };

        string aciklamaHtml = string.IsNullOrWhiteSpace(guvenliAciklama)
            ? string.Empty
            : $"""
              <div style="margin-top:18px; padding:14px 16px; border-radius:12px; background:#f8fafc; border:1px solid #e2e8f0;">
                  <p style="margin:0 0 6px 0; font-size:13px; font-weight:700; color:#475569;">Admin Açıklaması</p>
                  <p style="margin:0; font-size:14px; line-height:1.6; color:#334155;">{guvenliAciklama}</p>
              </div>
              """;

        string body = $"""
        <div style="font-family:Arial, sans-serif; background:#f8fafc; padding:24px;">
            <div style="max-width:620px; margin:0 auto; background:#ffffff; border:1px solid #e2e8f0; border-radius:18px; overflow:hidden;">

                <div style="padding:22px 24px; border-bottom:1px solid #e2e8f0;">
                    <p style="margin:0; font-size:13px; font-weight:700; color:#2563eb; letter-spacing:.04em;">
                        CoursVia
                    </p>

                    <h1 style="margin:8px 0 0 0; font-size:22px; color:#0f172a;">
                        {baslik}
                    </h1>
                </div>

                <div style="padding:24px;">
                    <p style="margin:0 0 14px 0; font-size:15px; color:#334155;">
                        Merhaba <strong>{guvenliAdSoyad}</strong>,
                    </p>

                    <p style="margin:0; font-size:15px; line-height:1.7; color:#334155;">
                        {mesaj}
                    </p>

                    {aciklamaHtml}

                    <p style="margin:22px 0 0 0; font-size:13px; color:#64748b;">
                        Bu e-posta CoursVia yönetim paneli tarafından otomatik gönderilmiştir.
                    </p>
                </div>

            </div>
        </div>
        """;

        await _emailService.SendEmailAsync(toEmail, subject, body);
    }
    
}
