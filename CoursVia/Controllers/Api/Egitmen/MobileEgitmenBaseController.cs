using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Egitmen;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoursVia.Controllers.Api.Egitmen;

// Eğitmen mobil controllerlarının ortak base class'ı.
// Kullanıcı id alma, sayfalama yardımcıları, ortak kurs sorgusu ve ortak öğrenci sorgusu burada tutulur.
public abstract class MobileEgitmenBaseController : ControllerBase
{
    protected readonly AppDbContext _context;

    protected MobileEgitmenBaseController(AppDbContext context)
    {
        _context = context;
    }

    // JWT token içinden giriş yapan kullanıcının KullaniciId değerini alır.
    protected int KullaniciIdGetir()
    {
        string? kullaniciIdDegeri = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(kullaniciIdDegeri, out int kullaniciId))
        {
            throw new UnauthorizedAccessException("Geçersiz kullanıcı bilgisi.");
        }

        return kullaniciId;
    }

    // Sayfa değerini normalize eder.
    protected static int SayfaNormalizeEt(int sayfa)
    {
        return sayfa < 1 ? 1 : sayfa;
    }

    // Sayfa başına kayıt değerini normalize eder.
    protected static int SayfaBasinaKayitNormalizeEt(int sayfaBasinaKayit)
    {
        if (sayfaBasinaKayit < 1)
        {
            return 10;
        }

        if (sayfaBasinaKayit > 50)
        {
            return 50;
        }

        return sayfaBasinaKayit;
    }

    // Toplam sayfa sayısını hesaplar.
    protected static int ToplamSayfaHesapla(int toplamKayit, int sayfaBasinaKayit)
    {
        if (toplamKayit <= 0)
        {
            return 1;
        }

        return (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);
    }

    // Eğitmene ait kursları tek tip mobil kurs kartına dönüştüren ortak sorgu.
    protected IQueryable<MobileEgitmenKursItemResponse> KursItemQuery(int kullaniciId)
    {
        return _context.Kurslar
            .AsNoTracking()
            .Where(x => x.EgitmenId == kullaniciId)
            .Select(x => new MobileEgitmenKursItemResponse
            {
                KursId = x.KursId,
                KursAdi = x.KursAdi,
                KapakGorselUrl = x.KapakGorselUrl,

                DurumId = x.DurumId,
                DurumAdi = x.Durum.DurumAdi,

                OgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi),

                TamamlayanOgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi &&
                        k.TamamlandiMi),

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
            });
    }

  
}