using CoursVia.Data;
using CoursVia.ViewModels.Mobile.Sertifika;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Controllers.Api;

// Mobil uygulama için sertifika doğrulama API controller'ı.
// Sertifika kodu herkese açık şekilde doğrulanabildiği için giriş zorunlu değildir.
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

    // Mobil uygulamadan gelen sertifika kodunu doğrular.
    // Örnek istek: GET /api/mobile/sertifika-dogrulama?kod=CV-20260617-ABC12345
    [HttpGet]
    public async Task<ActionResult<MobileSertifikaDogrulamaResponseDto>> Dogrula(
        [FromQuery] string? kod)
    {
        // Sertifika kodu gönderilmemişse istek hatalı kabul edilir.
        if (string.IsNullOrWhiteSpace(kod))
        {
            return BadRequest(new MobileSertifikaDogrulamaResponseDto
            {
                Basarili = false,
                GecerliMi = false,
                Mesaj = "Sertifika kodu zorunludur."
            });
        }

        // Başındaki ve sonundaki boşluklar temizlenir.
        kod = kod.Trim();

        // Sertifika koduna göre sertifika, öğrenci ve kurs bilgileri veritabanından alınır.
        // AsNoTracking kullanıldığı için veri sadece okunur, güncelleme takibi yapılmaz.
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

        // Girilen koda ait sertifika bulunamazsa 404 NotFound döndürülür.
        if (sertifika == null)
        {
            return NotFound(new MobileSertifikaDogrulamaResponseDto
            {
                Basarili = false,
                GecerliMi = false,
                Mesaj = "Bu koda ait geçerli bir sertifika bulunamadı."
            });
        }

        // Sertifika bulunduysa başarılı doğrulama cevabı döndürülür.
        return Ok(new MobileSertifikaDogrulamaResponseDto
        {
            Basarili = true,
            GecerliMi = true,
            Mesaj = "Sertifika başarıyla doğrulandı.",
            Sertifika = sertifika
        });
    }
}