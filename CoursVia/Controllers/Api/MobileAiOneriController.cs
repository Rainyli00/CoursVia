using System.Security.Claims;
using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Ai;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api;

[ApiController]
[Route("api/mobile/ai-oneriler")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Öğrenci,Eğitmen")]
public class MobileAiOneriController : ControllerBase
{
    private readonly AppDbContext _context;

    public MobileAiOneriController(AppDbContext context)
    {
        _context = context;
    }

    private int AktifKullaniciId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Listele(
        [FromQuery] string? arama,
        [FromQuery] string siralama = "yeni",
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBoyutu = 20)
    {
        if (sayfa < 1)
            sayfa = 1;

        if (sayfaBoyutu < 1 || sayfaBoyutu > 50)
            sayfaBoyutu = 20;

        arama = arama?.Trim();

        siralama = string.IsNullOrWhiteSpace(siralama)
            ? "yeni"
            : siralama.Trim().ToLower();

        var izinliSiralamalar = new[] { "yeni", "eski", "kurs-az", "kurs-za" };

        if (!izinliSiralamalar.Contains(siralama))
            siralama = "yeni";

        var kullaniciId = AktifKullaniciId;

        var query = _context.Oneriler
            .AsNoTracking()
            .Where(x => x.KullaniciId == kullaniciId);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            query = query.Where(x =>
                EF.Functions.Like(x.OneriMetni, $"%{arama}%") ||
                EF.Functions.Like(x.OneriTipi.OneriTipAdi, $"%{arama}%") ||
                (x.Kurs != null && EF.Functions.Like(x.Kurs.KursAdi, $"%{arama}%")));
        }

        query = siralama switch
        {
            "eski" => query
                .OrderBy(x => x.OlusturmaTarihi),

            "kurs-az" => query
                .OrderBy(x => x.Kurs != null ? x.Kurs.KursAdi : "")
                .ThenByDescending(x => x.OlusturmaTarihi),

            "kurs-za" => query
                .OrderByDescending(x => x.Kurs != null ? x.Kurs.KursAdi : "")
                .ThenByDescending(x => x.OlusturmaTarihi),

            _ => query
                .OrderByDescending(x => x.OlusturmaTarihi)
        };

        var toplamKayit = await query.CountAsync();

        var toplamSayfa = toplamKayit == 0
            ? 0
            : (int)Math.Ceiling(toplamKayit / (double)sayfaBoyutu);

        if (toplamSayfa > 0 && sayfa > toplamSayfa)
            sayfa = toplamSayfa;

        var oneriler = await query
            .Skip((sayfa - 1) * sayfaBoyutu)
            .Take(sayfaBoyutu)
            .Select(x => new MobileAiOneriListeItemDto
            {
                OneriId = x.OneriId,
                OneriTipId = x.OneriTipId,
                OneriTipAdi = x.OneriTipi.OneriTipAdi,
                KursId = x.KursId,
                KursAdi = x.Kurs != null ? x.Kurs.KursAdi : null,
                OneriMetni = x.OneriMetni,
                OlusturmaTarihi = x.OlusturmaTarihi
            })
            .ToListAsync();

        return Ok(new MobileAiOneriListeResponseDto
        {
            Basarili = true,
            Arama = arama,
            Siralama = siralama,
            ToplamKayit = toplamKayit,
            Sayfa = sayfa,
            SayfaBoyutu = sayfaBoyutu,
            ToplamSayfa = toplamSayfa,
            OncekiSayfaVarMi = sayfa > 1,
            SonrakiSayfaVarMi = toplamSayfa > 0 && sayfa < toplamSayfa,
            Oneriler = oneriler
        });
    }

    [HttpDelete("{oneriId:int}")]
    public async Task<IActionResult> Sil(int oneriId)
    {
        var kullaniciId = AktifKullaniciId;

        var oneri = await _context.Oneriler
            .FirstOrDefaultAsync(x => x.OneriId == oneriId && x.KullaniciId == kullaniciId);

        if (oneri == null)
        {
            return NotFound(new
            {
                basarili = false,
                mesaj = "AI önerisi bulunamadı."
            });
        }

        _context.Oneriler.Remove(oneri);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            basarili = true,
            mesaj = "AI önerisi silindi."
        });
    }
}