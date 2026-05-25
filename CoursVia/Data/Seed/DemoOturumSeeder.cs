using Microsoft.EntityFrameworkCore;

namespace CoursVia.Data.Seed;

public class DemoOturumSeeder
{
    private readonly AppDbContext _context;

    public DemoOturumSeeder(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await KullaniciOturumBilgileriniGuncelleAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task KullaniciOturumBilgileriniGuncelleAsync()
    {
        var epostalar = DemoOturumSabitleri.Oturumlar
            .Select(x => x.Eposta)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var kullanicilar = await _context.Kullanicilar
            .Where(x => epostalar.Contains(x.Eposta))
            .ToListAsync();

        var kullaniciSozluk = kullanicilar
            .ToDictionary(
                x => x.Eposta,
                x => x,
                StringComparer.OrdinalIgnoreCase
            );

        foreach (var bilgi in DemoOturumSabitleri.Oturumlar)
        {
            if (!kullaniciSozluk.TryGetValue(bilgi.Eposta, out var kullanici))
            {
                continue;
            }

            kullanici.SonIpAdresi = bilgi.SonIpAdresi;
            kullanici.OnlineMi = bilgi.OnlineMi;
            kullanici.SonGirisTarihi = SonGirisTarihiHesapla(
                bilgi.SonGirisGunOnce,
                bilgi.SonGirisSaatOnce
            );
        }

        await _context.SaveChangesAsync();
    }

    private static DateTime SonGirisTarihiHesapla(
        int gunOnce,
        int saatOnce)
    {
        return DateTime.Now
            .AddDays(-Math.Max(gunOnce, 0))
            .AddHours(-Math.Max(saatOnce, 0));
    }
}