using CoursVia.Models;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Data.Seed;

public class DemoKursSeeder
{
    private readonly AppDbContext _context;

    public DemoKursSeeder(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await DemoKurslariEkleVeyaGuncelleAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task DemoKurslariEkleVeyaGuncelleAsync()
    {
        var egitmenEpostalari = DemoKursSabitleri.Kurslar
            .Select(x => x.EgitmenEposta)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var kategoriAdlari = DemoKursSabitleri.Kurslar
            .Select(x => x.KategoriAdi)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var egitmenler = await _context.Kullanicilar
            .Where(x => egitmenEpostalari.Contains(x.Eposta))
            .ToListAsync();

        var kategoriler = await _context.Kategoriler
            .Where(x => kategoriAdlari.Contains(x.KategoriAdi))
            .ToListAsync();

        var egitmenSozluk = egitmenler
            .ToDictionary(
                x => x.Eposta,
                x => x,
                StringComparer.OrdinalIgnoreCase
            );

        var kategoriSozluk = kategoriler
            .ToDictionary(
                x => x.KategoriAdi,
                x => x.KategoriId,
                StringComparer.OrdinalIgnoreCase
            );

        foreach (var demoKurs in DemoKursSabitleri.Kurslar)
        {
            await KursEkleVeyaGuncelleAsync(
                demoKurs,
                egitmenSozluk,
                kategoriSozluk
            );
        }

        await _context.SaveChangesAsync();
    }

    private async Task KursEkleVeyaGuncelleAsync(
        DemoKursSabitleri.DemoKursBilgisi demoKurs,
        Dictionary<string, Kullanici> egitmenSozluk,
        Dictionary<string, int> kategoriSozluk)
    {
        if (!egitmenSozluk.TryGetValue(demoKurs.EgitmenEposta, out var egitmen))
        {
            return;
        }

        if (!kategoriSozluk.TryGetValue(demoKurs.KategoriAdi, out int kategoriId))
        {
            return;
        }

        var kurs = await _context.Kurslar
            .FirstOrDefaultAsync(x => x.KursAdi == demoKurs.KursAdi);

        if (kurs == null)
        {
            kurs = new Kurs
            {
                EgitmenId = egitmen.KullaniciId,
                DurumId = demoKurs.DurumId,
                KursAdi = demoKurs.KursAdi,
                Aciklama = demoKurs.Aciklama,
                KapakGorselUrl = demoKurs.KapakGorselUrl,
                OlusturmaTarihi = DateTime.Now.AddDays(-Math.Max(demoKurs.GunOnce, 1)),
                GuncellemeTarihi = demoKurs.DurumId == DemoSeedSabitleri.DurumKursDuzeltmeIsteniyor
                    ? DateTime.Now.AddDays(-2)
                    : null
            };

            _context.Kurslar.Add(kurs);
            await _context.SaveChangesAsync();
        }
        else
        {
            kurs.EgitmenId = egitmen.KullaniciId;
            kurs.DurumId = demoKurs.DurumId;
            kurs.KursAdi = demoKurs.KursAdi;
            kurs.Aciklama = demoKurs.Aciklama;
            kurs.KapakGorselUrl = demoKurs.KapakGorselUrl;

            if (kurs.OlusturmaTarihi == default)
            {
                kurs.OlusturmaTarihi = DateTime.Now.AddDays(-Math.Max(demoKurs.GunOnce, 1));
            }

            kurs.GuncellemeTarihi = demoKurs.DurumId == DemoSeedSabitleri.DurumKursDuzeltmeIsteniyor
                ? DateTime.Now.AddDays(-2)
                : kurs.GuncellemeTarihi;
        }

        await _context.SaveChangesAsync();

        await KursKategorisiniSenkronizeEtAsync(
            kurs.KursId,
            kategoriId
        );

        await BolumVeDersleriSenkronizeEtAsync(
            kurs.KursId,
            demoKurs
        );
    }

    private async Task KursKategorisiniSenkronizeEtAsync(
        int kursId,
        int hedefKategoriId)
    {
        var mevcutKategoriler = await _context.KursKategorileri
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        var silinecekKategoriler = mevcutKategoriler
            .Where(x => x.KategoriId != hedefKategoriId)
            .ToList();

        if (silinecekKategoriler.Count > 0)
        {
            _context.KursKategorileri.RemoveRange(silinecekKategoriler);
        }

        bool hedefKategoriVarMi = mevcutKategoriler
            .Any(x => x.KategoriId == hedefKategoriId);

        if (!hedefKategoriVarMi)
        {
            _context.KursKategorileri.Add(new KursKategorisi
            {
                KursId = kursId,
                KategoriId = hedefKategoriId
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task BolumVeDersleriSenkronizeEtAsync(
        int kursId,
        DemoKursSabitleri.DemoKursBilgisi demoKurs)
    {
        var mevcutBolumler = await _context.Bolumler
            .Where(x => x.KursId == kursId)
            .ToListAsync();

        var hedefBolumAdlari = demoKurs.Bolumler
            .Select(x => x.BolumAdi)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await SeedDisindaKalanBolumlerinDersleriniPasifeAlAsync(
            kursId,
            mevcutBolumler,
            hedefBolumAdlari
        );

        int dersGenelSiraNo = 1;

        for (int bolumIndex = 0; bolumIndex < demoKurs.Bolumler.Count; bolumIndex++)
        {
            var demoBolum = demoKurs.Bolumler[bolumIndex];

            var bolum = await _context.Bolumler
                .FirstOrDefaultAsync(x =>
                    x.KursId == kursId &&
                    x.BolumAdi == demoBolum.BolumAdi);

            if (bolum == null)
            {
                bolum = new Bolum
                {
                    KursId = kursId,
                    BolumAdi = demoBolum.BolumAdi,
                    SiraNo = bolumIndex + 1
                };

                _context.Bolumler.Add(bolum);
                await _context.SaveChangesAsync();
            }
            else
            {
                bolum.SiraNo = bolumIndex + 1;
            }

            await DersleriSenkronizeEtAsync(
                kursId,
                bolum.BolumId,
                demoBolum,
                demoKurs.GunOnce,
                dersGenelSiraNo
            );

            dersGenelSiraNo += demoBolum.Dersler.Count;
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedDisindaKalanBolumlerinDersleriniPasifeAlAsync(
        int kursId,
        List<Bolum> mevcutBolumler,
        HashSet<string> hedefBolumAdlari)
    {
        var seedDisindaKalanBolumler = mevcutBolumler
            .Where(x => !hedefBolumAdlari.Contains(x.BolumAdi))
            .ToList();

        if (seedDisindaKalanBolumler.Count == 0)
        {
            return;
        }

        var seedDisindaKalanBolumIdleri = seedDisindaKalanBolumler
            .Select(x => x.BolumId)
            .ToList();

        var pasifeAlinacakDersler = await _context.Dersler
            .Where(x =>
                x.KursId == kursId &&
                seedDisindaKalanBolumIdleri.Contains(x.BolumId))
            .ToListAsync();

        foreach (var ders in pasifeAlinacakDersler)
        {
            ders.AktifMi = false;
            ders.SiraNo = 9000 + ders.SiraNo;
        }

        int pasifBolumSiraNo = 9000;

        foreach (var bolum in seedDisindaKalanBolumler)
        {
            bolum.SiraNo = pasifBolumSiraNo;
            pasifBolumSiraNo++;
        }

        await _context.SaveChangesAsync();
    }

    private async Task DersleriSenkronizeEtAsync(
        int kursId,
        int bolumId,
        DemoKursSabitleri.DemoBolumBilgisi demoBolum,
        int kursGunOnce,
        int baslangicSiraNo)
    {
        var mevcutDersler = await _context.Dersler
            .Where(x =>
                x.KursId == kursId &&
                x.BolumId == bolumId)
            .ToListAsync();

        var hedefDersAdlari = demoBolum.Dersler
            .Select(x => x.DersAdi)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await SeedDisindaKalanDersleriPasifeAlAsync(
            mevcutDersler,
            hedefDersAdlari
        );

        for (int dersIndex = 0; dersIndex < demoBolum.Dersler.Count; dersIndex++)
        {
            var demoDers = demoBolum.Dersler[dersIndex];

            int siraNo = baslangicSiraNo + dersIndex;

            var ders = await _context.Dersler
                .FirstOrDefaultAsync(x =>
                    x.KursId == kursId &&
                    x.BolumId == bolumId &&
                    x.DersAdi == demoDers.DersAdi);

            if (ders == null)
            {
                ders = new Ders
                {
                    KursId = kursId,
                    BolumId = bolumId,
                    DersAdi = demoDers.DersAdi,
                    Aciklama = demoDers.Aciklama,
                    VideoUrl = VideoUrlGetir(demoDers.VideoUrl),
                    SiraNo = siraNo,
                    OlusturmaTarihi = DateTime.Now.AddDays(-Math.Max(kursGunOnce - siraNo, 1)),
                    AktifMi = true,
                    SistemDersiMi = false
                };

                _context.Dersler.Add(ders);
                await _context.SaveChangesAsync();
            }
            else
            {
                ders.DersAdi = demoDers.DersAdi;
                ders.Aciklama = demoDers.Aciklama;
                ders.VideoUrl = VideoUrlGetir(demoDers.VideoUrl);
                ders.SiraNo = siraNo;
                ders.AktifMi = true;
                ders.SistemDersiMi = false;

                if (ders.OlusturmaTarihi == default)
                {
                    ders.OlusturmaTarihi = DateTime.Now.AddDays(-Math.Max(kursGunOnce - siraNo, 1));
                }

                await _context.SaveChangesAsync();
            }

            await DersMateryalleriniEkleVeyaGuncelleAsync(
                ders.DersId,
                siraNo,
                kursId
            );
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedDisindaKalanDersleriPasifeAlAsync(
        List<Ders> mevcutDersler,
        HashSet<string> hedefDersAdlari)
    {
        var pasifeAlinacakDersler = mevcutDersler
            .Where(x => !hedefDersAdlari.Contains(x.DersAdi))
            .ToList();

        if (pasifeAlinacakDersler.Count == 0)
        {
            return;
        }

        int pasifSiraNo = 9000;

        foreach (var ders in pasifeAlinacakDersler)
        {
            ders.AktifMi = false;
            ders.SiraNo = pasifSiraNo;
            pasifSiraNo++;
        }

        await _context.SaveChangesAsync();
    }

    private async Task DersMateryalleriniEkleVeyaGuncelleAsync(
        int dersId,
        int dersSiraNo,
        int kursId)
    {
        if (dersSiraNo == 1)
        {
            await MateryalEkleVeyaGuncelleAsync(
                dersId,
                "Ders Notları",
                DemoSeedSabitleri.MateryalTipDokuman,
                DemoKursSabitleri.DemoPdfUrl,
                DateTime.Now.AddDays(-5)
            );
        }

        if (dersSiraNo == 2)
        {
            await MateryalEkleVeyaGuncelleAsync(
                dersId,
                "Ek Kaynak",
                DemoSeedSabitleri.MateryalTipDokuman,
                DemoKursSabitleri.DemoPdfUrl,
                DateTime.Now.AddDays(-4)
            );
        }

        bool kodDosyasiGerekliMi = await KodDosyasiGerekliMiAsync(kursId);

        if (kodDosyasiGerekliMi && dersSiraNo == 3)
        {
            await MateryalEkleVeyaGuncelleAsync(
                dersId,
                "Örnek Kod Dosyası",
                DemoSeedSabitleri.MateryalTipKod,
                DemoKursSabitleri.DemoKodUrl,
                DateTime.Now.AddDays(-3)
            );
        }
    }

    private async Task<bool> KodDosyasiGerekliMiAsync(int kursId)
    {
        var kursAdi = await _context.Kurslar
            .Where(x => x.KursId == kursId)
            .Select(x => x.KursAdi)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(kursAdi))
        {
            return false;
        }

        return kursAdi.Contains("ASP.NET", StringComparison.OrdinalIgnoreCase) ||
               kursAdi.Contains("JavaScript", StringComparison.OrdinalIgnoreCase) ||
               kursAdi.Contains("Python", StringComparison.OrdinalIgnoreCase);
    }

    private async Task MateryalEkleVeyaGuncelleAsync(
        int dersId,
        string baslik,
        int materyalTipId,
        string materyalUrl,
        DateTime yuklenmeTarihi)
    {
        var materyal = await _context.DersMateryalleri
            .FirstOrDefaultAsync(x =>
                x.DersId == dersId &&
                x.Baslik == baslik);

        if (materyal == null)
        {
            _context.DersMateryalleri.Add(new DersMateryali
            {
                DersId = dersId,
                MateryalTipId = materyalTipId,
                Baslik = baslik,
                MateryalUrl = materyalUrl,
                YuklenmeTarihi = yuklenmeTarihi
            });

            await _context.SaveChangesAsync();

            return;
        }

        materyal.MateryalTipId = materyalTipId;
        materyal.MateryalUrl = materyalUrl;

        if (materyal.YuklenmeTarihi == default)
        {
            materyal.YuklenmeTarihi = yuklenmeTarihi;
        }

        await _context.SaveChangesAsync();
    }

    private static string VideoUrlGetir(string? videoUrl)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
        {
            return DemoKursSabitleri.DemoVarsayilanVideoUrl;
        }

        return videoUrl.Trim();
    }
}