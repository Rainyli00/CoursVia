using CoursVia.Data;
using CoursVia.Models;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Services.Ai;

// AI analizlerinden çıkan metinleri kullanıcıya ait öneri kayıtlarına dönüştüren servis.
public class AiOneriService
{
    private readonly AppDbContext _context;

    public AiOneriService(AppDbContext context)
    {
        _context = context;
    }

    // Başarılı AI sonuçlarını belirtilen kullanıcı ve varsa kurs için Oneriler tablosuna kaydeder.
    public async Task OnerileriKaydetAsync(
        int kullaniciId,
        int? kursId,
        string oneriTipAdi,
        IEnumerable<AiAnalizSonucu> sonuclar,
        CancellationToken cancellationToken = default)
    {
        // Hatalı veya boş çıktılı model sonuçları öneri olarak saklanmaz.
        var basariliSonuclar = sonuclar
            .Where(x => x.BasariliMi && !string.IsNullOrWhiteSpace(x.TemizCikti))
            .ToList();

        // Kaydedilecek başarılı cevap yoksa veritabanına dokunulmaz.
        if (!basariliSonuclar.Any())
        {
            return;
        }

        // Öneri tipi yoksa otomatik oluşturulur; böylece yeni AI senaryoları kolay eklenir.
        var oneriTipi = await OneriTipiGetirVeyaOlusturAsync(
            oneriTipAdi,
            cancellationToken);

        // Her başarılı model çıktısı ayrı öneri olarak kaydedilir.
        foreach (var sonuc in basariliSonuclar)
        {
            _context.Oneriler.Add(new Oneri
            {
                KullaniciId = kullaniciId,
                OneriTipId = oneriTipi.OneriTipId,
                KursId = kursId,
                OneriMetni = $"[{sonuc.ModelAdi}]\n\n{sonuc.TemizCikti}",
                OlusturmaTarihi = DateTime.Now
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    // Kullanıcının kendi AI önerisini siler; başka kullanıcıya ait öneriye dokunmaz.
    public async Task<bool> OneriSilAsync(
        int kullaniciId,
        int oneriId,
        CancellationToken cancellationToken = default)
    {
        // Kullanıcı id filtresi sahiplik kontrolünü sağlar.
        var oneri = await _context.Oneriler
            .FirstOrDefaultAsync(x =>
                x.OneriId == oneriId &&
                x.KullaniciId == kullaniciId,
                cancellationToken);

        if (oneri == null)
        {
            return false;
        }

        // Kayıt bulunduysa fiziksel olarak silinir.
        _context.Oneriler.Remove(oneri);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    // Öneri tipini adına göre getirir; yoksa yeni tip oluşturup kaydeder.
    private async Task<OneriTipi> OneriTipiGetirVeyaOlusturAsync(
        string oneriTipAdi,
        CancellationToken cancellationToken)
    {
        // Aynı öneri tipi tekrar tekrar oluşmasın diye önce mevcut kayıt aranır.
        var oneriTipi = await _context.OneriTipleri
            .FirstOrDefaultAsync(x => x.OneriTipAdi == oneriTipAdi, cancellationToken);

        if (oneriTipi != null)
        {
            return oneriTipi;
        }

        // İlk kez kullanılan öneri tipi kalıcı hale getirilir.
        oneriTipi = new OneriTipi
        {
            OneriTipAdi = oneriTipAdi
        };

        _context.OneriTipleri.Add(oneriTipi);
        // cannelationToken ile birlikte kaydetme işlemi yapılır; böylece iptal durumunda kaynaklar serbest bırakılır.
        await _context.SaveChangesAsync(cancellationToken);

        return oneriTipi;
    }
}
