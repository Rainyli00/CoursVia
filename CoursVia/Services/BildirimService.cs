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

    // Kullanıcıya yeni bir bildirim oluşturur, eğer gelen bildirim tipi tabloda yoksa otomatik ekler.
    public async Task BildirimOlusturAsync(
        int kullaniciId,
        string bildirimTipiAdi,
        string baslik,
        string mesaj)
    {
        // Eksik veya geçersiz veri varsa bildirim oluşturulmadan sessizce çıkılır.
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

        // Bildirim tipi yoksa aynı işlem içinde yeni tip kaydı oluşturulur.
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

        // SaveChanges burada çağrılmaz; transaction kullanan üst akışla birlikte kaydedilir.
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

    // İlgili kullanıcının okunmamış bildirim sayısını döndürür.
    public async Task<int> OkunmamisBildirimSayisiAsync(int kullaniciId)
    {
        // Geçersiz kullanıcı için bildirim sayısı 0 kabul edilir.
        if (kullaniciId <= 0)
        {
            return 0;
        }

        // Sadece okunmamış bildirimler sayılır.
        return await _context.Bildirimler
            .AsNoTracking()
            .CountAsync(x =>
                x.KullaniciId == kullaniciId &&
                !x.OkunduMu);
    }

    // Kullanıcının sistemdeki okunmamış tüm bildirimlerini tek seferde okundu olarak işaretler.
    public async Task TumunuOkunduYapAsync(int kullaniciId)
    {
        // Geçersiz kullanıcı için işlem yapılmaz.
        if (kullaniciId <= 0)
        {
            return;
        }

        // Kullanıcının okunmamış tüm bildirimleri belleğe alınır ve okundu işaretlenir.
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

    // Belirtilen tek bir bildirimi okundu olarak işaretler.
    public async Task<bool> OkunduYapAsync(int kullaniciId, int bildirimId)
    {
        // Kullanıcı ve bildirim id geçerli değilse işlem başarısız kabul edilir.
        if (kullaniciId <= 0 || bildirimId <= 0)
        {
            return false;
        }

        // Bildirim kullanıcı id ile filtrelenir; başkasının bildirimi güncellenemez.
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

    // Yanlışlıkla okundu yapılan bildirimi tekrar okunmadı durumuna çeker.
    public async Task<bool> OkunmadiYapAsync(int kullaniciId, int bildirimId)
    {
        // Kullanıcı ve bildirim id geçerli değilse işlem başarısız kabul edilir.
        if (kullaniciId <= 0 || bildirimId <= 0)
        {
            return false;
        }

        // Bildirim kullanıcı id ile filtrelenir; başkasının bildirimi güncellenemez.
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
