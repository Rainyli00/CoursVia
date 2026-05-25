using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Sertifika;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api;

[ApiController]
[Route("api/mobile/sertifika-dogrulama")]
[AllowAnonymous]
public class MobileSertifikaDogrulamaController : ControllerBase
{
    private readonly AppDbContext _context;

    public MobileSertifikaDogrulamaController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<MobileSertifikaDogrulamaResponseDto>> Dogrula(
        [FromQuery] string? kod)
    {
        if (string.IsNullOrWhiteSpace(kod))
        {
            return BadRequest(new MobileSertifikaDogrulamaResponseDto
            {
                Basarili = false,
                GecerliMi = false,
                Mesaj = "Sertifika kodu zorunludur."
            });
        }

        kod = kod.Trim();

        var sertifika = await _context.Sertifikalar
            .AsNoTracking()
            .Include(x => x.Kullanici)
            .Include(x => x.Kurs)
            .Where(x => x.SertifikaKodu == kod)
            .Select(x => new MobileSertifikaDogrulamaDetayDto
            {
                SertifikaId = x.SertifikaId,
                SertifikaKodu = x.SertifikaKodu,
                OgrenciAdSoyad = (x.Kullanici.Ad + " " + x.Kullanici.Soyad).Trim(),
                KursAdi = x.Kurs.KursAdi,
                VerilmeTarihi = x.VerilmeTarihi
            })
            .FirstOrDefaultAsync();

        if (sertifika == null)
        {
            return NotFound(new MobileSertifikaDogrulamaResponseDto
            {
                Basarili = false,
                GecerliMi = false,
                Mesaj = "Bu koda ait geçerli bir sertifika bulunamadı."
            });
        }

        return Ok(new MobileSertifikaDogrulamaResponseDto
        {
            Basarili = true,
            GecerliMi = true,
            Mesaj = "Sertifika başarıyla doğrulandı.",
            Sertifika = sertifika
        });
    }
}