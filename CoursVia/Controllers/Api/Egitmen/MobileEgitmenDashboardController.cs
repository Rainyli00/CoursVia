using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Egitmen;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Egitmen;

// Mobil uygulamada eğitmen dashboard ekranı için özet bilgileri döndüren API controller.
[ApiController]
[Route("api/mobile/egitmen")]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Eğitmen"
)]
public class MobileEgitmenDashboardController : MobileEgitmenBaseController
{
    public MobileEgitmenDashboardController(AppDbContext context) : base(context)
    {
    }

    // Eğitmenin dashboard ekranında göreceği genel istatistikleri getirir.
    [HttpGet("dashboard")]
    public async Task<ActionResult<MobileEgitmenDashboardResponse>> Dashboard()
    {
        // JWT token içinden giriş yapan eğitmenin kullanıcı Id değeri alınır.
        int kullaniciId = KullaniciIdGetir();

        // Eğitmenin oluşturduğu toplam kurs sayısı hesaplanır.
        int toplamKursSayisi = await _context.Kurslar
            .AsNoTracking()
            .CountAsync(x => x.EgitmenId == kullaniciId);

        // Eğitmenin yayında olan kurslarının sayısı hesaplanır.
        // DurumId = 5 => Yayında
        int yayindakiKursSayisi = await _context.Kurslar
            .AsNoTracking()
            .CountAsync(x =>
                x.EgitmenId == kullaniciId &&
                x.DurumId == 5);

        // Eğitmenin kurslarına kayıtlı benzersiz aktif öğrenci sayısı hesaplanır.
        // Aynı öğrenci birden fazla kursa kayıtlıysa bir kere sayılır.
        int toplamOgrenciSayisi = await _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.AktifMi &&
                x.Kurs.EgitmenId == kullaniciId)
            .Select(x => x.KullaniciId)
            .Distinct()
            .CountAsync();

        // Eğitmenin okunmamış bildirim sayısı hesaplanır.
        int okunmamisBildirimSayisi = await _context.Bildirimler
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciId == kullaniciId &&
                !x.OkunduMu);

        // Eğitmenin en son güncellenen veya oluşturulan 3 kursu alınır.
        var sonKurslar = await _context.Kurslar
            .AsNoTracking()
            .Where(x => x.EgitmenId == kullaniciId)
            .OrderByDescending(x => x.GuncellemeTarihi ?? x.OlusturmaTarihi)
            .Take(3)
            .Select(x => new MobileEgitmenDashboardKursItemResponse
            {
                KursId = x.KursId,
                KursAdi = x.KursAdi,
                DurumAdi = x.Durum.DurumAdi,

                // Bu kursa aktif kayıtlı öğrenci sayısı hesaplanır.
                OgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi),

                // Kurstaki aktif ve sistem dersi olmayan normal ders sayısı hesaplanır.
                DersSayisi = _context.Dersler
                    .Count(d =>
                        d.KursId == x.KursId &&
                        d.AktifMi &&
                        !d.SistemDersiMi)
            })
            .ToListAsync();

        // Dashboard için gerekli tüm bilgiler mobil uygulamaya response olarak döndürülür.
        return Ok(new MobileEgitmenDashboardResponse
        {
            Basarili = true,
            Mesaj = "Eğitmen dashboard bilgileri getirildi.",

            ToplamKursSayisi = toplamKursSayisi,
            YayindakiKursSayisi = yayindakiKursSayisi,
            ToplamOgrenciSayisi = toplamOgrenciSayisi,
            OkunmamisBildirimSayisi = okunmamisBildirimSayisi,

            SonKurslar = sonKurslar
        });
    }
}