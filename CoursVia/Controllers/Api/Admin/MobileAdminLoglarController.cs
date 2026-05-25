using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Admin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Admin;

[ApiController]
[Route("api/mobile/admin/loglar")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Admin"
)]
public class MobileAdminLoglarController : MobileAdminBaseController
{
    public MobileAdminLoglarController(AppDbContext context) : base(context)
    {
    }

    // Admin mobil log listesini döndürür.
    // Arama, kategori filtresi, yeni/eski sıralama ve sayfalama destekler.
    // GET /api/mobile/admin/loglar?arama=kurs&kategori=kurs&sirala=yeni&sayfa=1&sayfaBasinaKayit=10
    [HttpGet]
    public async Task<ActionResult<MobileAdminLoglarResponse>> Loglar(
        [FromQuery] string? arama,
        [FromQuery] string? kategori = "tum",
        [FromQuery] string? sirala = "yeni",
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBasinaKayit = 10)
    {
        arama = string.IsNullOrWhiteSpace(arama)
            ? null
            : arama.Trim();

        kategori = string.IsNullOrWhiteSpace(kategori)
            ? "tum"
            : kategori.Trim().ToLower();

        sirala = string.IsNullOrWhiteSpace(sirala)
            ? "yeni"
            : sirala.Trim().ToLower();

        if (sirala != "yeni" && sirala != "eski")
        {
            sirala = "yeni";
        }

        sayfa = SayfaNormalizeEt(sayfa);
        sayfaBasinaKayit = SayfaBasinaKayitNormalizeEt(sayfaBasinaKayit);

        var kategoriler = LogKategorileriGetir();

        if (!kategoriler.Any(x => x.Kategori == kategori))
        {
            kategori = "tum";
        }

        var query = _context.AdminLoglari
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(arama))
        {
            string aramaDegeri = arama;

            query = query.Where(x =>
                (x.Admin == null ? "" : x.Admin.Ad).Contains(aramaDegeri) ||
                (x.Admin == null ? "" : x.Admin.Soyad).Contains(aramaDegeri) ||
                (x.Admin == null ? "" : x.Admin.Eposta).Contains(aramaDegeri) ||
                (x.IslemTipi == null ? "" : x.IslemTipi.IslemTipAdi).Contains(aramaDegeri) ||
                (x.Aciklama ?? "").Contains(aramaDegeri) ||
                (x.IpAdresi ?? "").Contains(aramaDegeri));
        }

        query = kategori switch
        {
            "kullanici" => query.Where(x =>
                (x.IslemTipi == null ? "" : x.IslemTipi.IslemTipAdi).Contains("Kullanıcı") ||
                (x.Aciklama ?? "").Contains("Kullanıcı")),

            "egitmen-basvuru" => query.Where(x =>
                (x.IslemTipi == null ? "" : x.IslemTipi.IslemTipAdi).Contains("Eğitmen") ||
                (x.IslemTipi == null ? "" : x.IslemTipi.IslemTipAdi).Contains("Başvuru") ||
                (x.Aciklama ?? "").Contains("Eğitmen") ||
                (x.Aciklama ?? "").Contains("Başvuru")),

            "kurs-onay" => query.Where(x =>
                (x.IslemTipi == null ? "" : x.IslemTipi.IslemTipAdi).Contains("Kurs Onay") ||
                (x.Aciklama ?? "").Contains("Kurs Onay") ||
                (x.Aciklama ?? "").Contains("onay") ||
                (x.Aciklama ?? "").Contains("red")),

            "kurs" => query.Where(x =>
                (x.IslemTipi == null ? "" : x.IslemTipi.IslemTipAdi).Contains("Kurs") ||
                (x.Aciklama ?? "").Contains("Kurs")),

            "yorum" => query.Where(x =>
                (x.IslemTipi == null ? "" : x.IslemTipi.IslemTipAdi).Contains("Yorum") ||
                (x.IslemTipi == null ? "" : x.IslemTipi.IslemTipAdi).Contains("Değerlendirme") ||
                (x.Aciklama ?? "").Contains("Yorum") ||
                (x.Aciklama ?? "").Contains("Değerlendirme")),

            "kategori" => query.Where(x =>
                (x.IslemTipi == null ? "" : x.IslemTipi.IslemTipAdi).Contains("Kategori") ||
                (x.Aciklama ?? "").Contains("Kategori")),

            "sistem" => query.Where(x =>
                (x.IslemTipi == null ? "" : x.IslemTipi.IslemTipAdi).Contains("Sistem") ||
                (x.IslemTipi == null ? "" : x.IslemTipi.IslemTipAdi).Contains("Giriş") ||
                (x.Aciklama ?? "").Contains("Sistem") ||
                (x.Aciklama ?? "").Contains("Giriş")),

            _ => query
        };

        query = sirala == "eski"
            ? query.OrderBy(x => x.IslemTarihi)
            : query.OrderByDescending(x => x.IslemTarihi);

        int toplamKayit = await query.CountAsync();
        int toplamSayfa = ToplamSayfaHesapla(toplamKayit, sayfaBasinaKayit);

        if (sayfa > toplamSayfa)
        {
            sayfa = toplamSayfa;
        }

        var logHamListe = await query
            .Skip((sayfa - 1) * sayfaBasinaKayit)
            .Take(sayfaBasinaKayit)
            .Select(x => new
            {
                x.AdminLogId,

                AdminAd = x.Admin == null ? "" : x.Admin.Ad,
                AdminSoyad = x.Admin == null ? "" : x.Admin.Soyad,

                IslemTipi = x.IslemTipi == null
                    ? ""
                    : x.IslemTipi.IslemTipAdi,

                Aciklama = x.Aciklama ?? string.Empty,
                x.IpAdresi,
                x.IslemTarihi
            })
            .ToListAsync();

        var loglar = logHamListe
            .Select(x => new MobileAdminLogItemResponse
            {
                AdminLogId = x.AdminLogId,

                AdminAdSoyad = string.IsNullOrWhiteSpace($"{x.AdminAd} {x.AdminSoyad}".Trim())
                    ? "Bilinmeyen Admin"
                    : $"{x.AdminAd} {x.AdminSoyad}".Trim(),

                IslemTipi = string.IsNullOrWhiteSpace(x.IslemTipi)
                    ? "Bilinmeyen İşlem"
                    : x.IslemTipi,

                Aciklama = x.Aciklama,
                IpAdresi = x.IpAdresi,
                IslemTarihi = x.IslemTarihi
            })
            .ToList();

        return Ok(new MobileAdminLoglarResponse
        {
            Basarili = true,
            Mesaj = "Admin logları getirildi.",

            Arama = arama,
            Kategori = kategori,
            Sirala = sirala,

            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            SayfaBasinaKayit = sayfaBasinaKayit,
            ToplamSayfa = toplamSayfa,

            Kategoriler = kategoriler,
            Loglar = loglar
        });
    }
    // Log kategorilerini döndürür.
    private static List<MobileAdminLogKategoriResponse> LogKategorileriGetir()
    {
        return new List<MobileAdminLogKategoriResponse>
        {
            new()
            {
                Kategori = "tum",
                KategoriAdi = "Tüm Kategoriler"
            },
            new()
            {
                Kategori = "kullanici",
                KategoriAdi = "Kullanıcı İşlemleri"
            },
            new()
            {
                Kategori = "egitmen-basvuru",
                KategoriAdi = "Eğitmen Başvuruları"
            },
            new()
            {
                Kategori = "kurs-onay",
                KategoriAdi = "Kurs Onayları"
            },
            new()
            {
                Kategori = "kurs",
                KategoriAdi = "Kurs İşlemleri"
            },
            new()
            {
                Kategori = "yorum",
                KategoriAdi = "Yorum İşlemleri"
            },
            new()
            {
                Kategori = "kategori",
                KategoriAdi = "Kategori İşlemleri"
            },
            new()
            {
                Kategori = "sistem",
                KategoriAdi = "Sistem İşlemleri"
            }
        };
    }
}