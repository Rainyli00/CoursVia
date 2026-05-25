using CoursVia.Data;
using CoursVia.Models;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Services;

public class BildirimService
{
    private readonly AppDbContext _context;

    public BildirimService(AppDbContext context)
    {
        _context = context;
    }

    public async Task BildirimOlusturAsync(
        int kullaniciId,
        string bildirimTipiAdi,
        string baslik,
        string mesaj)
    {
        if (kullaniciId <= 0 ||
            string.IsNullOrWhiteSpace(bildirimTipiAdi) ||
            string.IsNullOrWhiteSpace(baslik) ||
            string.IsNullOrWhiteSpace(mesaj))
        {
            return;
        }

        bildirimTipiAdi = bildirimTipiAdi.Trim();
        baslik = baslik.Trim();
        mesaj = mesaj.Trim();

        var bildirimTipi = await _context.BildirimTipleri
            .FirstOrDefaultAsync(x => x.BildirimTipAdi == bildirimTipiAdi);

        if (bildirimTipi == null)
        {
            bildirimTipi = new BildirimTipi
            {
                BildirimTipAdi = bildirimTipiAdi
            };

            _context.BildirimTipleri.Add(bildirimTipi);
        }

        _context.Bildirimler.Add(new Bildirim
        {
            KullaniciId = kullaniciId,
            BildirimTipi = bildirimTipi,
            Baslik = baslik,
            Mesaj = mesaj,
            OlusturmaTarihi = DateTime.Now,
            OkunduMu = false
        });
    }

    public async Task<int> OkunmamisBildirimSayisiAsync(int kullaniciId)
    {
        if (kullaniciId <= 0)
        {
            return 0;
        }

        return await _context.Bildirimler
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciId == kullaniciId &&
                !x.OkunduMu);
    }

    public async Task TumunuOkunduYapAsync(int kullaniciId)
    {
        if (kullaniciId <= 0)
        {
            return;
        }

        var bildirimler = await _context.Bildirimler
            .Where(x =>
                x.KullaniciId == kullaniciId &&
                !x.OkunduMu)
            .ToListAsync();

        foreach (var bildirim in bildirimler)
        {
            bildirim.OkunduMu = true;
        }
    }

    public async Task<bool> OkunduYapAsync(int kullaniciId, int bildirimId)
    {
        if (kullaniciId <= 0 || bildirimId <= 0)
        {
            return false;
        }

        var bildirim = await _context.Bildirimler
            .FirstOrDefaultAsync(x =>
                x.BildirimId == bildirimId &&
                x.KullaniciId == kullaniciId);

        if (bildirim == null)
        {
            return false;
        }

        bildirim.OkunduMu = true;

        return true;
    }

    public async Task<bool> OkunmadiYapAsync(int kullaniciId, int bildirimId)
    {
        if (kullaniciId <= 0 || bildirimId <= 0)
        {
            return false;
        }

        var bildirim = await _context.Bildirimler
            .FirstOrDefaultAsync(x =>
                x.BildirimId == bildirimId &&
                x.KullaniciId == kullaniciId);

        if (bildirim == null)
        {
            return false;
        }

        bildirim.OkunduMu = false;

        return true;
    }
}
