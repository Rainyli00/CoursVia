using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Egitmen;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api.Egitmen;

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

    // Eğitmen mobil ana ekranı için sade dashboard bilgilerini döndürür.
    // Gereksiz detaylar dönülmez.
    // Son kurslar alanında sadece son 3 kurs döner.
    // GET /api/mobile/egitmen/dashboard
    [HttpGet("dashboard")]
    public async Task<ActionResult<MobileEgitmenDashboardResponse>> Dashboard()
    {
        int kullaniciId = KullaniciIdGetir();

        int toplamKursSayisi = await _context.Kurslar
            .AsNoTracking()
            .CountAsync(x => x.EgitmenId == kullaniciId);

        int yayindakiKursSayisi = await _context.Kurslar
            .AsNoTracking()
            .CountAsync(x =>
                x.EgitmenId == kullaniciId &&
                x.DurumId == 5);

        int toplamOgrenciSayisi = await _context.KursKayitlari
            .AsNoTracking()
            .Where(x =>
                x.AktifMi &&
                x.Kurs.EgitmenId == kullaniciId)
            .Select(x => x.KullaniciId)
            .Distinct()
            .CountAsync();

        int okunmamisBildirimSayisi = await _context.Bildirimler
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciId == kullaniciId &&
                !x.OkunduMu);

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

                OgrenciSayisi = _context.KursKayitlari
                    .Count(k =>
                        k.KursId == x.KursId &&
                        k.AktifMi),

                DersSayisi = _context.Dersler
                    .Count(d =>
                        d.KursId == x.KursId &&
                        d.AktifMi &&
                        !d.SistemDersiMi)
            })
            .ToListAsync();

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