using CoursVia.Data;
using CoursVia.Models;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Services.Ai;

public class AiOneriService
{
    private readonly AppDbContext _context;

    public AiOneriService(AppDbContext context)
    {
        _context = context;
    }

    public async Task OnerileriKaydetAsync(
        int kullaniciId,
        int? kursId,
        string oneriTipAdi,
        IEnumerable<AiAnalizSonucu> sonuclar,
        CancellationToken cancellationToken = default)
    {
        var basariliSonuclar = sonuclar
            .Where(x => x.BasariliMi && !string.IsNullOrWhiteSpace(x.TemizCikti))
            .ToList();

        if (!basariliSonuclar.Any())
        {
            return;
        }

        var oneriTipi = await OneriTipiGetirVeyaOlusturAsync(
            oneriTipAdi,
            cancellationToken);

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

    public async Task<bool> OneriSilAsync(
        int kullaniciId,
        int oneriId,
        CancellationToken cancellationToken = default)
    {
        var oneri = await _context.Oneriler
            .FirstOrDefaultAsync(x =>
                x.OneriId == oneriId &&
                x.KullaniciId == kullaniciId,
                cancellationToken);

        if (oneri == null)
        {
            return false;
        }

        _context.Oneriler.Remove(oneri);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<OneriTipi> OneriTipiGetirVeyaOlusturAsync(
        string oneriTipAdi,
        CancellationToken cancellationToken)
    {
        var oneriTipi = await _context.OneriTipleri
            .FirstOrDefaultAsync(x => x.OneriTipAdi == oneriTipAdi, cancellationToken);

        if (oneriTipi != null)
        {
            return oneriTipi;
        }

        oneriTipi = new OneriTipi
        {
            OneriTipAdi = oneriTipAdi
        };

        _context.OneriTipleri.Add(oneriTipi);
        await _context.SaveChangesAsync(cancellationToken);

        return oneriTipi;
    }
}
