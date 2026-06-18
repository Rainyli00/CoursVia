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

    // JWT içindeki kullanıcı kimliğini aktif mobil kullanıcı olarak alır.
    private int AktifKullaniciId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Mobil uygulamada kullanıcının AI önerilerini listeler.
    // Arama, sıralama ve sayfalama destekler.
    // Sıralama değerleri: yeni, eski, kurs-az, kurs-za
    [HttpGet]
    public async Task<IActionResult> Listele(
        [FromQuery] string? arama,
        [FromQuery] string siralama = "yeni",
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBoyutu = 20)
    {
        // Sayfa ve sayfa boyutu, hatalı veya aşırı büyük mobil istekleri engellemek için normalize edilir.
        if (sayfa < 1)
            sayfa = 1;

        if (sayfaBoyutu < 1 || sayfaBoyutu > 50)
            sayfaBoyutu = 20;

        // Arama ve sıralama parametreleri boşluklardan temizlenir; geçersiz sıralama varsayılana çekilir.
        arama = arama?.Trim();

        siralama = string.IsNullOrWhiteSpace(siralama)
            ? "yeni"
            : siralama.Trim().ToLower();

        var izinliSiralamalar = new[] { "yeni", "eski", "kurs-az", "kurs-za" };

        if (!izinliSiralamalar.Contains(siralama))
            siralama = "yeni";

        var kullaniciId = AktifKullaniciId;

        // Kullanıcı sadece kendi AI önerilerini görebilir.
        var query = _context.Oneriler
            .AsNoTracking()
            .Where(x => x.KullaniciId == kullaniciId);

        if (!string.IsNullOrWhiteSpace(arama))
        {
            // Arama; öneri metni, öneri tipi ve varsa kurs adı üzerinden yapılır.
            query = query.Where(x =>
                EF.Functions.Like(x.OneriMetni, $"%{arama}%") ||
                EF.Functions.Like(x.OneriTipi.OneriTipAdi, $"%{arama}%") ||
                (x.Kurs != null && EF.Functions.Like(x.Kurs.KursAdi, $"%{arama}%")));
        }

        // Sıralama veritabanı sorgusunda uygulanır; kursu olmayan kayıtlar boş kurs adıyla sıralanır.
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

        // Entity yerine mobil liste DTO'su seçilerek yalnızca ekranda gereken alanlar döndürülür.
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

    // Mobil kullanıcının kendi AI önerisini siler.
    // DELETE /api/mobile/ai-oneriler/{oneriId}
    [HttpDelete("{oneriId:int}")]
    public async Task<IActionResult> Sil(int oneriId)
    {
        var kullaniciId = AktifKullaniciId;

        // Kullanıcı id filtresi, başka kullanıcıya ait önerinin silinmesini engeller.
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

        // Kayıt bulunduysa silinir ve değişiklik veritabanına kaydedilir.
        _context.Oneriler.Remove(oneri);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            basarili = true,
            mesaj = "AI önerisi silindi."
        });
    }
}
