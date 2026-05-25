using CoursVia.Models;
using CoursVia.Services;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Data.Seed;

public class DemoKullaniciSeeder
{
    private readonly AppDbContext _context;
    private readonly PasswordService _passwordService;

    public DemoKullaniciSeeder(
        AppDbContext context,
        PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task SeedAsync()
    {
        await DemoKullanicilariEkleVeyaGuncelleAsync();
        await DemoRolleriEkleAsync();
        await DemoEgitmenProfilleriniEkleVeyaGuncelleAsync();
    }

    private async Task DemoKullanicilariEkleVeyaGuncelleAsync()
    {
        var demoKullanicilar = DemoSeedSabitleri.TumKullanicilar();

        var demoEpostalar = demoKullanicilar
            .Select(x => x.Eposta)
            .ToList();

        var mevcutKullanicilar = await _context.Kullanicilar
            .Where(x => demoEpostalar.Contains(x.Eposta))
            .ToListAsync();

        var mevcutKullaniciSozluk = mevcutKullanicilar
            .ToDictionary(x => x.Eposta, StringComparer.OrdinalIgnoreCase);

        foreach (var demoKullanici in demoKullanicilar)
        {
            if (mevcutKullaniciSozluk.TryGetValue(demoKullanici.Eposta, out var mevcutKullanici))
            {
                mevcutKullanici.DurumId = DemoSeedSabitleri.DurumAktif;
                mevcutKullanici.Ad = demoKullanici.Ad;
                mevcutKullanici.Soyad = demoKullanici.Soyad;
                mevcutKullanici.Telefon = demoKullanici.Telefon;
                mevcutKullanici.OnlineMi = false;

                continue;
            }

            _context.Kullanicilar.Add(KullaniciOlustur(demoKullanici));
        }

        await _context.SaveChangesAsync();
    }

    private async Task DemoRolleriEkleAsync()
    {
        var demoKullanicilar = DemoSeedSabitleri.TumKullanicilar();

        var demoEpostalar = demoKullanicilar
            .Select(x => x.Eposta)
            .ToList();

        var kullanicilar = await _context.Kullanicilar
            .Where(x => demoEpostalar.Contains(x.Eposta))
            .ToListAsync();

        var kullaniciSozluk = kullanicilar
            .ToDictionary(x => x.Eposta, StringComparer.OrdinalIgnoreCase);

        var kullaniciIdleri = kullanicilar
            .Select(x => x.KullaniciId)
            .ToList();

        var mevcutRoller = await _context.KullaniciRolleri
            .Where(x => kullaniciIdleri.Contains(x.KullaniciId))
            .Select(x => new
            {
                x.KullaniciId,
                x.RolId
            })
            .ToListAsync();

        foreach (var demoKullanici in demoKullanicilar)
        {
            if (!kullaniciSozluk.TryGetValue(demoKullanici.Eposta, out var kullanici))
            {
                continue;
            }

            bool rolVarMi = mevcutRoller.Any(x =>
                x.KullaniciId == kullanici.KullaniciId &&
                x.RolId == demoKullanici.RolId);

            if (rolVarMi)
            {
                continue;
            }

            _context.KullaniciRolleri.Add(new KullaniciRol
            {
                KullaniciId = kullanici.KullaniciId,
                RolId = demoKullanici.RolId
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task DemoEgitmenProfilleriniEkleVeyaGuncelleAsync()
    {
        var egitmenEpostalari = DemoSeedSabitleri.Egitmenler
            .Select(x => x.Eposta)
            .ToList();

        var egitmenler = await _context.Kullanicilar
            .Where(x => egitmenEpostalari.Contains(x.Eposta))
            .ToListAsync();

        var egitmenSozluk = egitmenler
            .ToDictionary(x => x.Eposta, StringComparer.OrdinalIgnoreCase);

        var kategoriler = await _context.Kategoriler
            .ToListAsync();

        var kategoriIdleri = kategoriler
            .ToDictionary(
                x => x.KategoriAdi,
                x => x.KategoriId,
                StringComparer.OrdinalIgnoreCase
            );

        foreach (var profilBilgisi in DemoSeedSabitleri.EgitmenProfilleri)
        {
            if (!egitmenSozluk.TryGetValue(profilBilgisi.Eposta, out var egitmen))
            {
                continue;
            }

            var egitmenProfili = await _context.EgitmenProfilleri
                .FirstOrDefaultAsync(x => x.KullaniciId == egitmen.KullaniciId);

            if (egitmenProfili == null)
            {
                egitmenProfili = new EgitmenProfili
                {
                    KullaniciId = egitmen.KullaniciId,

                    // Eğitmen tarafında Düzeltme İsteniyor kullanılmaz.
                    // Demo eğitmenler onaylı başlar.
                    DurumId = DemoSeedSabitleri.DurumEgitmenOnaylandi,

                    UzmanlikAlani = profilBilgisi.UzmanlikAlani,
                    Biyografi = profilBilgisi.Biyografi,
                    DeneyimYili = profilBilgisi.DeneyimYili,
                    WebsiteUrl = profilBilgisi.WebsiteUrl
                };

                _context.EgitmenProfilleri.Add(egitmenProfili);

                await _context.SaveChangesAsync();
            }
            else
            {
                // Eğitmen profili için sadece 4, 6, 8 kullanılacak.
                // Demo eğitmenleri daima onaylı tutuyoruz.
                egitmenProfili.DurumId = DemoSeedSabitleri.DurumEgitmenOnaylandi;

                egitmenProfili.UzmanlikAlani = profilBilgisi.UzmanlikAlani;
                egitmenProfili.Biyografi = profilBilgisi.Biyografi;
                egitmenProfili.DeneyimYili = profilBilgisi.DeneyimYili;
                egitmenProfili.WebsiteUrl = profilBilgisi.WebsiteUrl;
            }

            await DemoEgitmenBranslariniSenkronizeEtAsync(
                egitmenProfili.EgitmenProfilId,
                profilBilgisi.Branslar,
                kategoriIdleri
            );
        }

        await _context.SaveChangesAsync();
    }

    private async Task DemoEgitmenBranslariniSenkronizeEtAsync(
        int egitmenProfilId,
        string[] bransAdlari,
        Dictionary<string, int> kategoriIdleri)
    {
        var hedefKategoriIdleri = new List<int>();

        foreach (var bransAdi in bransAdlari)
        {
            if (kategoriIdleri.TryGetValue(bransAdi, out int kategoriId))
            {
                hedefKategoriIdleri.Add(kategoriId);
            }
        }

        hedefKategoriIdleri = hedefKategoriIdleri
            .Distinct()
            .ToList();

        var mevcutBranslar = await _context.EgitmenBranslari
            .Where(x => x.EgitmenProfilId == egitmenProfilId)
            .ToListAsync();

        var silinecekBranslar = mevcutBranslar
            .Where(x => !hedefKategoriIdleri.Contains(x.KategoriId))
            .ToList();

        if (silinecekBranslar.Count > 0)
        {
            _context.EgitmenBranslari.RemoveRange(silinecekBranslar);
        }

        var mevcutKategoriIdleri = mevcutBranslar
            .Select(x => x.KategoriId)
            .ToHashSet();

        foreach (var kategoriId in hedefKategoriIdleri)
        {
            if (mevcutKategoriIdleri.Contains(kategoriId))
            {
                continue;
            }

            _context.EgitmenBranslari.Add(new EgitmenBransi
            {
                EgitmenProfilId = egitmenProfilId,
                KategoriId = kategoriId
            });
        }
    }

    private Kullanici KullaniciOlustur(DemoSeedSabitleri.DemoKullaniciBilgisi demoKullanici)
    {
        return new Kullanici
        {
            DurumId = DemoSeedSabitleri.DurumAktif,

            Ad = demoKullanici.Ad,
            Soyad = demoKullanici.Soyad,
            Eposta = demoKullanici.Eposta,
            Telefon = demoKullanici.Telefon,

            SifreHash = _passwordService.HashPassword(DemoSeedSabitleri.DemoSifre),

            KayitTarihi = DateTime.Now.AddDays(-30),
            SonGirisTarihi = null,
            SonIpAdresi = null,
            OnlineMi = false
        };
    }
}