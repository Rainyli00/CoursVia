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

// Admin panelinde kurs onay başvurularını yönetir.
[Authorize(Roles = "Admin")]
public class AdminKursOnayController : Controller
{
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;
    private readonly AdminLogService _adminLogService;
    private readonly BildirimService _bildirimService;

    public AdminKursOnayController(
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

    // Kurs başvurularını arama, durum filtresi ve sayfalama ile listeler.
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

        // Seçilen sekmeye göre kurs durum Id değeri belirlenir.
        int? durumId = durum switch
        {
            "bekleyen" => 4,
            "onaylanan" => 5,
            "reddedilen" => 6,
            "tum" => null,
            _ => 4
        };

        var query = _context.Kurslar
            .AsNoTracking()
            .Include(x => x.Egitmen)
            .Include(x => x.Durum)
            .Include(x => x.KursKategorileri)
                .ThenInclude(x => x.Kategori)
            .Include(x => x.Bolumler)
            .Include(x => x.Dersler)
            .Include(x => x.Sinav)
                .ThenInclude(x => x!.Sorular)
            .AsQueryable();

        if (durumId.HasValue)
        {
            query = query.Where(x => x.DurumId == durumId.Value);
        }

        // Kurs adı, açıklama veya eğitmen bilgisine göre arama yapılır.
        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                x.KursAdi.Contains(arama) ||
                (x.Aciklama != null && x.Aciklama.Contains(arama)) ||
                x.Egitmen.Ad.Contains(arama) ||
                x.Egitmen.Soyad.Contains(arama) ||
                x.Egitmen.Eposta.Contains(arama));
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

        // Liste ekranında gösterilecek kurs bilgileri ViewModel'e aktarılır.
        var kurslar = await query
            .OrderByDescending(x => x.GuncellemeTarihi ?? x.OlusturmaTarihi)
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new KursOnayListeItemViewModel
            {
                KursId = x.KursId,
                KursAdi = x.KursAdi,
                Aciklama = x.Aciklama,
                KapakGorselUrl = x.KapakGorselUrl,

                EgitmenAdSoyad = x.Egitmen.Ad + " " + x.Egitmen.Soyad,
                EgitmenEposta = x.Egitmen.Eposta,

                DurumId = x.DurumId,
                DurumAdi = x.Durum.DurumAdi,

                OlusturmaTarihi = x.OlusturmaTarihi,
                GuncellemeTarihi = x.GuncellemeTarihi,

                BolumSayisi = x.Bolumler.Count,

                DersSayisi = x.Dersler.Count(d =>
                    d.AktifMi &&
                    !d.SistemDersiMi),

                SinavVarMi = x.Sinav != null,

                SoruSayisi = x.Sinav == null
                    ? 0
                    : x.Sinav.Sorular.Count(s => s.AktifMi),

                Kategoriler = x.KursKategorileri
                    .Select(k => k.Kategori.KategoriAdi)
                    .OrderBy(k => k)
                    .ToList()
            })
            .ToListAsync();

        var model = new KursOnaylariViewModel
        {
            Arama = arama,
            Durum = durum,

            Kurslar = kurslar,

            BekleyenSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 4),

            OnaylananSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 5),

            ReddedilenSayisi = await _context.Kurslar
                .AsNoTracking()
                .CountAsync(x => x.DurumId == 6),

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            ToplamSayfa = toplamSayfa,
            SayfaBasinaKayit = sayfaBasinaKayit
        };

        return View(model);
    }

    // Adminin kurs başvurusunu detaylı incelemesini sağlar.
    [HttpGet]
    public async Task<IActionResult> Detay(int id)
    {
        var kurs = await _context.Kurslar
            .AsNoTracking()
            .Include(x => x.Egitmen)
            .Include(x => x.Durum)
            .Include(x => x.KursKategorileri)
                .ThenInclude(x => x.Kategori)
            .Include(x => x.Bolumler.OrderBy(b => b.SiraNo))
                .ThenInclude(b => b.Dersler
                    .Where(d => d.AktifMi && !d.SistemDersiMi)
                    .OrderBy(d => d.SiraNo))
                    .ThenInclude(d => d.DersMateryalleri)
                        .ThenInclude(m => m.MateryalTipi)
            .Include(x => x.Sinav)
                .ThenInclude(x => x!.Sorular)
                    .ThenInclude(s => s.SoruSecenekleri)
            .FirstOrDefaultAsync(x => x.KursId == id);

        if (kurs == null)
        {
            TempData["AdminHata"] = "Kurs bulunamadı.";
            return RedirectToAction(nameof(Basvurular));
        }

        // Kursun bölüm, ders, materyal, sınav ve kategori bilgileri detay ekranı için hazırlanır.
        var model = new KursOnayDetayViewModel
        {
            KursId = kurs.KursId,
            KursAdi = kurs.KursAdi,
            Aciklama = kurs.Aciklama,
            KapakGorselUrl = kurs.KapakGorselUrl,

            EgitmenAdSoyad = $"{kurs.Egitmen.Ad} {kurs.Egitmen.Soyad}".Trim(),
            EgitmenEposta = kurs.Egitmen.Eposta,

            DurumId = kurs.DurumId,
            DurumAdi = kurs.Durum.DurumAdi,

            OlusturmaTarihi = kurs.OlusturmaTarihi,
            GuncellemeTarihi = kurs.GuncellemeTarihi,

            Kategoriler = kurs.KursKategorileri
                .Select(x => x.Kategori.KategoriAdi)
                .OrderBy(x => x)
                .ToList(),

            Bolumler = kurs.Bolumler
                .OrderBy(x => x.SiraNo)
                .Select(x => new KursOnayBolumViewModel
                {
                    BolumId = x.BolumId,
                    BolumAdi = x.BolumAdi,
                    SiraNo = x.SiraNo,

                    Dersler = x.Dersler
                        .OrderBy(d => d.SiraNo)
                        .Select(d => new KursOnayDersViewModel
                        {
                            DersId = d.DersId,
                            DersAdi = d.DersAdi,
                            Aciklama = d.Aciklama,
                            VideoUrl = d.VideoUrl ?? string.Empty,
                            SiraNo = d.SiraNo,

                            MateryalSayisi = d.DersMateryalleri.Count,

                            Materyaller = d.DersMateryalleri
                                .OrderBy(m => m.YuklenmeTarihi)
                                .Select(m => new KursOnayDersMateryalViewModel
                                {
                                    MateryalId = m.MateryalId,
                                    Baslik = m.Baslik,
                                    MateryalUrl = m.MateryalUrl,
                                    MateryalTipAdi = m.MateryalTipi.MateryalTipAdi
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToList(),

            Sinav = kurs.Sinav == null
                ? null
                : new KursOnaySinavViewModel
                {
                    SinavId = kurs.Sinav.SinavId,
                    SinavAdi = kurs.Sinav.SinavAdi,
                    Aciklama = kurs.Sinav.Aciklama,
                    GecmeNotu = kurs.Sinav.GecmeNotu,
                    SureDakika = kurs.Sinav.SureDakika,
                    SoruSayisi = kurs.Sinav.SoruSayisi,

                    AktifSoruSayisi = kurs.Sinav.Sorular
                        .Count(x => x.AktifMi),

                    Sorular = kurs.Sinav.Sorular
                        .Where(s => s.AktifMi)
                        .OrderBy(s => s.SoruId)
                        .Select(s => new KursOnaySoruViewModel
                        {
                            SoruId = s.SoruId,
                            SoruMetni = s.SoruMetni,
                            AktifMi = s.AktifMi,

                            Secenekler = s.SoruSecenekleri
                                .OrderBy(secenek => secenek.SecenekId)
                                .Select(secenek => new KursOnaySoruSecenegiViewModel
                                {
                                    SecenekId = secenek.SecenekId,
                                    SecenekMetni = secenek.SecenekMetni,
                                    DogruMu = secenek.DogruMu,
                                    AktifMi = secenek.AktifMi
                                })
                                .ToList()
                        })
                        .ToList()
                }
        };

        return View(model);
    }

    // Adminin kurs başvurusunu onaylamasını veya reddetmesini sağlar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Karar(
        int kursId,
        string karar,
        string? aciklama)
    {
        int adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        karar = string.IsNullOrWhiteSpace(karar)
            ? string.Empty
            : karar.Trim().ToLower();

        // Onay veya red kararına göre yeni kurs durumu Id değeri belirlenir.
        int yeniDurumId = karar switch
        {
            "onayla" => 5,
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

        // Red kararında eğitmene sebep gösterilebilmesi için açıklama zorunludur.
        if (karar == "reddet")
        {
            if (string.IsNullOrWhiteSpace(aciklama))
            {
                TempData["AdminHata"] = "Reddetme işleminde red sebebi zorunludur.";
                return RedirectToAction(nameof(Detay), new { id = kursId });
            }

            if (aciklama.Length < 5)
            {
                TempData["AdminHata"] = "Red sebebi en az 5 karakter olmalıdır.";
                return RedirectToAction(nameof(Detay), new { id = kursId });
            }
        }

        var kurs = await _context.Kurslar
            .Include(x => x.Egitmen)
            .Include(x => x.KursKategorileri)
            .Include(x => x.Bolumler)
            .Include(x => x.Dersler)
            .Include(x => x.Sinav)
                .ThenInclude(x => x!.Sorular)
            .FirstOrDefaultAsync(x => x.KursId == kursId);

        if (kurs == null)
        {
            TempData["AdminHata"] = "Kurs bulunamadı.";
            return RedirectToAction(nameof(Basvurular));
        }

        if (kurs.DurumId != 4)
        {
            TempData["AdminHata"] = "Sadece onay bekleyen kurslar için karar verilebilir.";
            return RedirectToAction(nameof(Detay), new { id = kursId });
        }

        // Onaylama işleminden önce kursun yayına hazır olup olmadığı kontrol edilir.
        if (karar == "onayla")
        {
            string? yayinHatasi = KursYayinKontrolEt(kurs);

            if (!string.IsNullOrWhiteSpace(yayinHatasi))
            {
                TempData["AdminHata"] = yayinHatasi;
                return RedirectToAction(nameof(Detay), new { id = kursId });
            }
        }

        string egitmenAdSoyad = $"{kurs.Egitmen.Ad} {kurs.Egitmen.Soyad}".Trim();
        string egitmenEposta = kurs.Egitmen.Eposta;

        string islemMetni = karar switch
        {
            "onayla" => "Kurs başvurusu onaylandı",
            "reddet" => "Kurs başvurusu reddedildi",
            _ => "Kurs başvurusu güncellendi"
        };

        bool islemBasarili = false;

        // Kurs durumu, onay kaydı, bildirim ve log işlemleri tek transaction içinde yapılır.
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            kurs.DurumId = yeniDurumId;
            kurs.GuncellemeTarihi = DateTime.Now;

            _context.KursOnaylari.Add(new KursOnayi
            {
                KursId = kurs.KursId,
                AdminId = adminId,
                DurumId = yeniDurumId,
                Aciklama = aciklama,
                IslemTarihi = DateTime.Now
            });

            if (karar == "onayla")
            {
                await _bildirimService.BildirimOlusturAsync(
                    kurs.EgitmenId,
                    "Bilgilendirme",
                    "Kursunuz onaylandı",
                    $"\"{kurs.KursAdi}\" adlı kursunuz onaylandı ve öğrenciler tarafından görüntülenebilir."
                );
            }
            else if (karar == "reddet")
            {
                string bildirimMesaji = string.IsNullOrWhiteSpace(aciklama)
                    ? $"\"{kurs.KursAdi}\" adlı kursunuz admin tarafından reddedildi."
                    : $"\"{kurs.KursAdi}\" adlı kursunuz admin tarafından reddedildi. Red sebebi: {aciklama}";

                await _bildirimService.BildirimOlusturAsync(
                    kurs.EgitmenId,
                    "Uyarı",
                    "Kursunuz reddedildi",
                    bildirimMesaji
                );
            }

            await _adminLogService.KaydetAsync(
                adminId,
                AdminLogService.KursOnaylari,
                $"{kurs.KursAdi} - {egitmenAdSoyad} - {islemMetni}");

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

        // Veritabanı işlemi başarılıysa eğitmene bilgilendirme maili gönderilir.
        if (islemBasarili)
        {
            try
            {
                await KursOnayMailGonderAsync(
                    egitmenEposta,
                    egitmenAdSoyad,
                    kurs.KursAdi,
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

    // Kursun yayına alınabilmesi için gerekli şartları kontrol eder.
    private string? KursYayinKontrolEt(Kurs kurs)
    {
        bool kategoriVar = kurs.KursKategorileri.Any();

        if (!kategoriVar)
        {
            return "Kurs onaylanamaz. En az bir kategori seçilmiş olmalıdır.";
        }

        bool bolumVar = kurs.Bolumler.Any();

        if (!bolumVar)
        {
            return "Kurs onaylanamaz. En az bir bölüm eklenmiş olmalıdır.";
        }

        bool dersVar = kurs.Dersler.Any(x =>
            x.AktifMi &&
            !x.SistemDersiMi);

        if (!dersVar)
        {
            return "Kurs onaylanamaz. En az bir aktif ders eklenmiş olmalıdır.";
        }

        if (kurs.Sinav == null)
        {
            return "Kurs onaylanamaz. Kurs sınavı hazırlanmış olmalıdır.";
        }

        int aktifSoruSayisi = kurs.Sinav.Sorular.Count(x => x.AktifMi);

        if (aktifSoruSayisi < kurs.Sinav.SoruSayisi)
        {
            return "Kurs onaylanamaz. Aktif soru sayısı sınav soru sayısından az olamaz.";
        }

        return null;
    }

    // Kurs onay veya red kararına göre eğitmene HTML formatında bilgilendirme maili gönderir.
    private async Task KursOnayMailGonderAsync(
        string toEmail,
        string adSoyad,
        string kursAdi,
        string karar,
        string? aciklama)
    {
        // Mail içeriğinde kullanılan metinler HTML encode edilerek güvenli hale getirilir.
        string guvenliAdSoyad = WebUtility.HtmlEncode(adSoyad);
        string guvenliKursAdi = WebUtility.HtmlEncode(kursAdi);
        string guvenliAciklama = WebUtility.HtmlEncode(aciklama ?? string.Empty);

        string subject = karar switch
        {
            "onayla" => "CoursVia Kursunuz Yayına Alındı",
            "reddet" => "CoursVia Kurs Başvurunuz Reddedildi",
            _ => "CoursVia Kurs Başvurusu"
        };

        string baslik = karar switch
        {
            "onayla" => "Kursunuz yayına alındı.",
            "reddet" => "Kurs başvurunuz reddedildi.",
            _ => "Kurs başvurunuz güncellendi."
        };

        string mesaj = karar switch
        {
            "onayla" => "Kursunuz admin incelemesinden geçti ve öğrencilerin erişimine açıldı.",
            "reddet" => "Kursunuz yapılan değerlendirme sonucunda yayına uygun bulunmadı. Red sebebine göre düzenleme yaparak kursunuzu tekrar yayına gönderebilirsiniz.",
            _ => "Kursunuzla ilgili durum güncellendi."
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

                    <p style="margin:0 0 12px 0; font-size:15px; line-height:1.7; color:#334155;">
                        <strong>{guvenliKursAdi}</strong> adlı kursunuz için değerlendirme sonucu aşağıdadır.
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

        // Servis aracılığıyla eğitmene bilgilendirme maili gönderilir.
        await _emailService.SendEmailAsync(toEmail, subject, body);
    }
}