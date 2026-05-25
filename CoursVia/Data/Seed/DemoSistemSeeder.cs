using CoursVia.Models;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Data.Seed;

public class DemoSistemSeeder
{
    private readonly AppDbContext _context;

    public DemoSistemSeeder(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await YardimciTipleriHazirlaAsync();

            await EgitmenOnaylariniEkleVeyaGuncelleAsync();
            await KursOnaylariniEkleVeyaGuncelleAsync();
            await AdminLoglariniEkleVeyaGuncelleAsync();
            await BildirimleriEkleVeyaGuncelleAsync();
            await OnerileriEkleVeyaGuncelleAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task YardimciTipleriHazirlaAsync()
    {
        await BildirimTipiGetirVeyaOlusturAsync("Bilgilendirme");
        await BildirimTipiGetirVeyaOlusturAsync("Uyarı");
        await BildirimTipiGetirVeyaOlusturAsync("Hata");

        await OneriTipiGetirVeyaOlusturAsync("Eğitmen Kurs Analizi");
        await OneriTipiGetirVeyaOlusturAsync("Öğrenci Çalışma Önerisi");

        await IslemTipiGetirVeyaOlusturAsync("Sistem");
        await IslemTipiGetirVeyaOlusturAsync("Kullanıcı İşlemleri");
        await IslemTipiGetirVeyaOlusturAsync("Kurs Onayları");
        await IslemTipiGetirVeyaOlusturAsync("Eğitmen Başvuruları");
        await IslemTipiGetirVeyaOlusturAsync("Kurs");
        await IslemTipiGetirVeyaOlusturAsync("Kategori");
        await IslemTipiGetirVeyaOlusturAsync("Yorum");

        await _context.SaveChangesAsync();
    }

    private async Task EgitmenOnaylariniEkleVeyaGuncelleAsync()
    {
        var adminler = await _context.Kullanicilar
            .Where(x => DemoSeedSabitleri.Adminler.Select(a => a.Eposta).Contains(x.Eposta))
            .ToListAsync();

        var egitmenler = await _context.Kullanicilar
            .Include(x => x.EgitmenProfili)
            .Where(x => DemoSeedSabitleri.Egitmenler.Select(e => e.Eposta).Contains(x.Eposta))
            .ToListAsync();

        if (adminler.Count == 0 || egitmenler.Count == 0)
        {
            return;
        }

        var adminSozluk = adminler
            .ToDictionary(x => x.Eposta, x => x, StringComparer.OrdinalIgnoreCase);

        var adminEpostalari = new[]
        {
            "admin1@coursvia.com",
            "admin2@coursvia.com",
            "admin3@coursvia.com",
            "admin4@coursvia.com",
            "admin5@coursvia.com"
        };

        int index = 0;

        foreach (var egitmen in egitmenler.OrderBy(x => x.KullaniciId))
        {
            if (egitmen.EgitmenProfili == null)
            {
                continue;
            }

            string adminEposta = adminEpostalari[index % adminEpostalari.Length];

            if (!adminSozluk.TryGetValue(adminEposta, out var admin))
            {
                admin = adminler.First();
            }

            egitmen.EgitmenProfili.DurumId = DemoSeedSabitleri.DurumEgitmenOnaylandi;

            string aciklama =
                $"Demo veri: {egitmen.Ad} {egitmen.Soyad} eğitmen başvurusu incelendi ve onaylandı.";

            var mevcutOnay = await _context.EgitmenOnaylari
                .FirstOrDefaultAsync(x =>
                    x.KullaniciId == egitmen.KullaniciId &&
                    x.DurumId == DemoSeedSabitleri.DurumEgitmenOnaylandi &&
                    x.Aciklama == aciklama);

            DateTime islemTarihi = DateTime.Now.AddDays(-(20 - index));

            if (mevcutOnay == null)
            {
                _context.EgitmenOnaylari.Add(new EgitmenOnayi
                {
                    KullaniciId = egitmen.KullaniciId,
                    AdminId = admin.KullaniciId,
                    DurumId = DemoSeedSabitleri.DurumEgitmenOnaylandi,
                    Aciklama = aciklama,
                    IslemTarihi = islemTarihi
                });
            }
            else
            {
                mevcutOnay.AdminId = admin.KullaniciId;
                mevcutOnay.IslemTarihi = islemTarihi;
            }

            bool egitmenRoluVarMi = await _context.KullaniciRolleri
                .AnyAsync(x =>
                    x.KullaniciId == egitmen.KullaniciId &&
                    x.RolId == DemoSeedSabitleri.RolEgitmen);

            if (!egitmenRoluVarMi)
            {
                _context.KullaniciRolleri.Add(new KullaniciRol
                {
                    KullaniciId = egitmen.KullaniciId,
                    RolId = DemoSeedSabitleri.RolEgitmen
                });
            }

            index++;
        }

        await _context.SaveChangesAsync();
    }

    private async Task KursOnaylariniEkleVeyaGuncelleAsync()
    {
        var kursAdlari = DemoSistemSabitleri.KursOnaylari
            .Select(x => x.KursAdi)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var adminEpostalari = DemoSistemSabitleri.KursOnaylari
            .Select(x => x.AdminEposta)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var kurslar = await _context.Kurslar
            .Where(x => kursAdlari.Contains(x.KursAdi))
            .ToListAsync();

        var adminler = await _context.Kullanicilar
            .Where(x => adminEpostalari.Contains(x.Eposta))
            .ToListAsync();

        var kursSozluk = kurslar
            .ToDictionary(x => x.KursAdi, x => x, StringComparer.OrdinalIgnoreCase);

        var adminSozluk = adminler
            .ToDictionary(x => x.Eposta, x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var bilgi in DemoSistemSabitleri.KursOnaylari)
        {
            if (!kursSozluk.TryGetValue(bilgi.KursAdi, out var kurs))
            {
                continue;
            }

            if (!adminSozluk.TryGetValue(bilgi.AdminEposta, out var admin))
            {
                continue;
            }

            var mevcutOnay = await _context.KursOnaylari
                .FirstOrDefaultAsync(x =>
                    x.KursId == kurs.KursId &&
                    x.DurumId == bilgi.DurumId &&
                    x.Aciklama == bilgi.Aciklama);

            DateTime islemTarihi = DateTime.Now.AddDays(-Math.Max(bilgi.GunOnce, 1));

            if (mevcutOnay == null)
            {
                _context.KursOnaylari.Add(new KursOnayi
                {
                    KursId = kurs.KursId,
                    AdminId = admin.KullaniciId,
                    DurumId = bilgi.DurumId,
                    Aciklama = bilgi.Aciklama,
                    IslemTarihi = islemTarihi
                });

                continue;
            }

            mevcutOnay.AdminId = admin.KullaniciId;
            mevcutOnay.IslemTarihi = islemTarihi;
        }

        await _context.SaveChangesAsync();
    }

    private async Task AdminLoglariniEkleVeyaGuncelleAsync()
    {
        var adminEpostalari = DemoSistemSabitleri.AdminLoglari
            .Select(x => x.AdminEposta)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var adminler = await _context.Kullanicilar
            .Where(x => adminEpostalari.Contains(x.Eposta))
            .ToListAsync();

        var adminSozluk = adminler
            .ToDictionary(x => x.Eposta, x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var bilgi in DemoSistemSabitleri.AdminLoglari)
        {
            if (!adminSozluk.TryGetValue(bilgi.AdminEposta, out var admin))
            {
                continue;
            }

            var islemTipi = await IslemTipiGetirVeyaOlusturAsync(bilgi.IslemTipAdi);

            var mevcutLog = await _context.AdminLoglari
                .FirstOrDefaultAsync(x =>
                    x.AdminId == admin.KullaniciId &&
                    x.IslemTipId == islemTipi.IslemTipId &&
                    x.Aciklama == bilgi.Aciklama);

            DateTime islemTarihi = DateTime.Now.AddDays(-Math.Max(bilgi.GunOnce, 1));

            if (mevcutLog == null)
            {
                _context.AdminLoglari.Add(new AdminLog
                {
                    AdminId = admin.KullaniciId,
                    IslemTipId = islemTipi.IslemTipId,
                    Aciklama = bilgi.Aciklama,
                    IpAdresi = bilgi.IpAdresi,
                    IslemTarihi = islemTarihi
                });

                continue;
            }

            mevcutLog.IpAdresi = bilgi.IpAdresi;
            mevcutLog.IslemTarihi = islemTarihi;
        }

        await _context.SaveChangesAsync();
    }

    private async Task BildirimleriEkleVeyaGuncelleAsync()
    {
        var epostalar = DemoSistemSabitleri.Bildirimler
            .Select(x => x.KullaniciEposta)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var kullanicilar = await _context.Kullanicilar
            .Where(x => epostalar.Contains(x.Eposta))
            .ToListAsync();

        var kullaniciSozluk = kullanicilar
            .ToDictionary(x => x.Eposta, x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var bilgi in DemoSistemSabitleri.Bildirimler)
        {
            if (!kullaniciSozluk.TryGetValue(bilgi.KullaniciEposta, out var kullanici))
            {
                continue;
            }

            var bildirimTipi = await BildirimTipiGetirVeyaOlusturAsync(bilgi.BildirimTipAdi);

            var bildirim = await _context.Bildirimler
                .FirstOrDefaultAsync(x =>
                    x.KullaniciId == kullanici.KullaniciId &&
                    x.Baslik == bilgi.Baslik);

            DateTime olusturmaTarihi = DateTime.Now.AddDays(-Math.Max(bilgi.GunOnce, 1));

            if (bildirim == null)
            {
                _context.Bildirimler.Add(new Bildirim
                {
                    KullaniciId = kullanici.KullaniciId,
                    BildirimTipId = bildirimTipi.BildirimTipId,
                    Baslik = bilgi.Baslik,
                    Mesaj = bilgi.Mesaj,
                    OlusturmaTarihi = olusturmaTarihi,
                    OkunduMu = bilgi.OkunduMu
                });

                continue;
            }

            bildirim.BildirimTipId = bildirimTipi.BildirimTipId;
            bildirim.Mesaj = bilgi.Mesaj;
            bildirim.OlusturmaTarihi = olusturmaTarihi;
            bildirim.OkunduMu = bilgi.OkunduMu;
        }

        await _context.SaveChangesAsync();
    }

    private async Task OnerileriEkleVeyaGuncelleAsync()
    {
        var kullaniciEpostalari = DemoSistemSabitleri.Oneriler
            .Select(x => x.KullaniciEposta)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var kursAdlari = DemoSistemSabitleri.Oneriler
            .Select(x => x.KursAdi)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var kullanicilar = await _context.Kullanicilar
            .Where(x => kullaniciEpostalari.Contains(x.Eposta))
            .ToListAsync();

        var kurslar = await _context.Kurslar
            .Where(x => kursAdlari.Contains(x.KursAdi))
            .ToListAsync();

        var kullaniciSozluk = kullanicilar
            .ToDictionary(x => x.Eposta, x => x, StringComparer.OrdinalIgnoreCase);

        var kursSozluk = kurslar
            .ToDictionary(x => x.KursAdi, x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var bilgi in DemoSistemSabitleri.Oneriler)
        {
            if (!kullaniciSozluk.TryGetValue(bilgi.KullaniciEposta, out var kullanici))
            {
                continue;
            }

            if (!kursSozluk.TryGetValue(bilgi.KursAdi, out var kurs))
            {
                continue;
            }

            var oneriTipi = await OneriTipiGetirVeyaOlusturAsync(bilgi.OneriTipAdi);

            var oneri = await _context.Oneriler
                .FirstOrDefaultAsync(x =>
                    x.KullaniciId == kullanici.KullaniciId &&
                    x.OneriTipId == oneriTipi.OneriTipId &&
                    x.KursId == kurs.KursId &&
                    x.OneriMetni.StartsWith("[DEMO]"));

            if (oneri == null)
            {
                _context.Oneriler.Add(new Oneri
                {
                    KullaniciId = kullanici.KullaniciId,
                    OneriTipId = oneriTipi.OneriTipId,
                    KursId = kurs.KursId,
                    OneriMetni = bilgi.OneriMetni,
                    OlusturmaTarihi = DateTime.Now.AddDays(-1)
                });

                continue;
            }

            oneri.OneriMetni = bilgi.OneriMetni;
            oneri.OlusturmaTarihi = DateTime.Now.AddDays(-1);
        }

        await _context.SaveChangesAsync();
    }

    private async Task<BildirimTipi> BildirimTipiGetirVeyaOlusturAsync(string bildirimTipAdi)
    {
        bildirimTipAdi = bildirimTipAdi.Trim();

        var bildirimTipi = await _context.BildirimTipleri
            .FirstOrDefaultAsync(x => x.BildirimTipAdi == bildirimTipAdi);

        if (bildirimTipi != null)
        {
            return bildirimTipi;
        }

        bildirimTipi = new BildirimTipi
        {
            BildirimTipAdi = bildirimTipAdi
        };

        _context.BildirimTipleri.Add(bildirimTipi);
        await _context.SaveChangesAsync();

        return bildirimTipi;
    }

    private async Task<OneriTipi> OneriTipiGetirVeyaOlusturAsync(string oneriTipAdi)
    {
        oneriTipAdi = oneriTipAdi.Trim();

        var oneriTipi = await _context.OneriTipleri
            .FirstOrDefaultAsync(x => x.OneriTipAdi == oneriTipAdi);

        if (oneriTipi != null)
        {
            return oneriTipi;
        }

        oneriTipi = new OneriTipi
        {
            OneriTipAdi = oneriTipAdi
        };

        _context.OneriTipleri.Add(oneriTipi);
        await _context.SaveChangesAsync();

        return oneriTipi;
    }

    private async Task<IslemTipi> IslemTipiGetirVeyaOlusturAsync(string islemTipAdi)
    {
        islemTipAdi = islemTipAdi.Trim();

        var islemTipi = await _context.IslemTipleri
            .FirstOrDefaultAsync(x => x.IslemTipAdi == islemTipAdi);

        if (islemTipi != null)
        {
            return islemTipi;
        }

        islemTipi = new IslemTipi
        {
            IslemTipAdi = islemTipAdi
        };

        _context.IslemTipleri.Add(islemTipi);
        await _context.SaveChangesAsync();

        return islemTipi;
    }
}