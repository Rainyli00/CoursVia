using CoursVia.Models;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Data.Seed;

public class DemoOgrenciHareketSeeder
{
    private readonly AppDbContext _context;

    public DemoOgrenciHareketSeeder(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await DemoKursKayitlariniEkleVeyaGuncelleAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task DemoKursKayitlariniEkleVeyaGuncelleAsync()
    {
        var ogrenciEpostalari = DemoOgrenciHareketSabitleri.KursSenaryolari
            .Select(x => x.OgrenciEposta)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var kursAdlari = DemoOgrenciHareketSabitleri.KursSenaryolari
            .Select(x => x.KursAdi)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ogrenciler = await _context.Kullanicilar
            .Where(x => ogrenciEpostalari.Contains(x.Eposta))
            .ToListAsync();

        var kurslar = await _context.Kurslar
            .Where(x => kursAdlari.Contains(x.KursAdi))
            .ToListAsync();

        var ogrenciSozluk = ogrenciler
            .ToDictionary(
                x => x.Eposta,
                x => x,
                StringComparer.OrdinalIgnoreCase
            );

        var kursSozluk = kurslar
            .ToDictionary(
                x => x.KursAdi,
                x => x,
                StringComparer.OrdinalIgnoreCase
            );

        foreach (var senaryo in DemoOgrenciHareketSabitleri.KursSenaryolari)
        {
            await OgrenciKursSenaryosunuUygulaAsync(
                senaryo,
                ogrenciSozluk,
                kursSozluk
            );
        }

        await _context.SaveChangesAsync();
    }

    private async Task OgrenciKursSenaryosunuUygulaAsync(
        DemoOgrenciHareketSabitleri.DemoOgrenciKursSenaryosu senaryo,
        Dictionary<string, Kullanici> ogrenciSozluk,
        Dictionary<string, Kurs> kursSozluk)
    {
        if (!ogrenciSozluk.TryGetValue(senaryo.OgrenciEposta, out var ogrenci))
        {
            return;
        }

        if (!kursSozluk.TryGetValue(senaryo.KursAdi, out var kurs))
        {
            return;
        }

        bool kursKaydaUygunMu =
            kurs.DurumId == DemoSeedSabitleri.DurumYayinda ||
            kurs.DurumId == DemoSeedSabitleri.DurumKursDuzeltmeIsteniyor;

        if (!kursKaydaUygunMu)
        {
            return;
        }

        var kursKaydi = await KursKaydiEkleVeyaGuncelleAsync(
            ogrenci.KullaniciId,
            kurs.KursId,
            kurs.DurumId,
            senaryo
        );

        await DersIlerlemeleriniSenkronizeEtAsync(
            kursKaydi.KursKayitId,
            kurs.KursId,
            senaryo.IlerlemeYuzdesi
        );

        await FavoriSenkronizeEtAsync(
            ogrenci.KullaniciId,
            kurs.KursId,
            senaryo.FavoriMi,
            senaryo.KayitGunOnce
        );

        await DegerlendirmeSenkronizeEtAsync(
            ogrenci.KullaniciId,
            kurs.KursId,
            senaryo.Puan,
            senaryo.YorumMetni,
            senaryo.KayitGunOnce
        );
    }

    private async Task<KursKaydi> KursKaydiEkleVeyaGuncelleAsync(
        int ogrenciId,
        int kursId,
        int kursDurumId,
        DemoOgrenciHareketSabitleri.DemoOgrenciKursSenaryosu senaryo)
    {
        var kursKaydi = await _context.KursKayitlari
            .FirstOrDefaultAsync(x =>
                x.KullaniciId == ogrenciId &&
                x.KursId == kursId);

        DateTime kayitTarihi = DateTime.Now.AddDays(
            -Math.Max(senaryo.KayitGunOnce, 1)
        );

        bool aktifMi =
            kursDurumId == DemoSeedSabitleri.DurumYayinda ||
            kursDurumId == DemoSeedSabitleri.DurumKursDuzeltmeIsteniyor;

        /*
            ÖNEMLİ:
            Burada artık KursKaydi.TamamlandiMi = ilerleme %100 diye true yapılmıyor.

            Çünkü CoursVia mantığında:
            - Derslerin tamamlanması ayrı şeydir.
            - Kursun tamamlanması sınavla birlikte kesinleşir.
            - Öğrenci dersi %100 bitirse bile sınavdan kalırsa sertifika alamaz.
            - Bu yüzden final tamamlanma kararını DemoSinavSeeder verir.
        */

        if (kursKaydi == null)
        {
            kursKaydi = new KursKaydi
            {
                KullaniciId = ogrenciId,
                KursId = kursId,
                KayitTarihi = kayitTarihi,
                TamamlandiMi = false,
                TamamlanmaTarihi = null,
                AktifMi = aktifMi
            };

            _context.KursKayitlari.Add(kursKaydi);

            await _context.SaveChangesAsync();

            return kursKaydi;
        }

        kursKaydi.KayitTarihi = kayitTarihi;
        kursKaydi.TamamlandiMi = false;
        kursKaydi.TamamlanmaTarihi = null;
        kursKaydi.AktifMi = aktifMi;

        await _context.SaveChangesAsync();

        return kursKaydi;
    }

    private async Task DersIlerlemeleriniSenkronizeEtAsync(
        int kursKayitId,
        int kursId,
        int ilerlemeYuzdesi)
    {
        ilerlemeYuzdesi = Math.Clamp(ilerlemeYuzdesi, 0, 100);

        var dersler = await _context.Dersler
            .Where(x =>
                x.KursId == kursId &&
                x.AktifMi &&
                !x.SistemDersiMi)
            .OrderBy(x => x.SiraNo)
            .Select(x => new
            {
                x.DersId,
                x.SiraNo
            })
            .ToListAsync();

        if (dersler.Count == 0)
        {
            return;
        }

        int tamamlanacakDersSayisi = TamamlanacakDersSayisiHesapla(
            dersler.Count,
            ilerlemeYuzdesi
        );

        var tamamlananDersIdleri = dersler
            .Take(tamamlanacakDersSayisi)
            .Select(x => x.DersId)
            .ToHashSet();

        var aktifDersIdleri = dersler
            .Select(x => x.DersId)
            .ToHashSet();

        var mevcutIlerlemeler = await _context.DersIlerlemeleri
            .Where(x => x.KursKayitId == kursKayitId)
            .ToListAsync();

        var silinecekIlerlemeler = mevcutIlerlemeler
            .Where(x => !aktifDersIdleri.Contains(x.DersId))
            .ToList();

        if (silinecekIlerlemeler.Count > 0)
        {
            _context.DersIlerlemeleri.RemoveRange(silinecekIlerlemeler);
        }

        foreach (var ders in dersler)
        {
            bool tamamlandiMi = tamamlananDersIdleri.Contains(ders.DersId);

            var ilerleme = mevcutIlerlemeler
                .FirstOrDefault(x => x.DersId == ders.DersId);

            if (ilerleme == null)
            {
                _context.DersIlerlemeleri.Add(new DersIlerlemesi
                {
                    KursKayitId = kursKayitId,
                    DersId = ders.DersId,
                    TamamlandiMi = tamamlandiMi
                });

                continue;
            }

            ilerleme.TamamlandiMi = tamamlandiMi;
        }

        await _context.SaveChangesAsync();
    }

    private static int TamamlanacakDersSayisiHesapla(
        int toplamDersSayisi,
        int ilerlemeYuzdesi)
    {
        if (toplamDersSayisi <= 0)
        {
            return 0;
        }

        if (ilerlemeYuzdesi <= 0)
        {
            return 0;
        }

        if (ilerlemeYuzdesi >= 100)
        {
            return toplamDersSayisi;
        }

        int hesaplanan = (int)Math.Round(
            toplamDersSayisi * (ilerlemeYuzdesi / 100.0),
            MidpointRounding.AwayFromZero
        );

        return Math.Clamp(
            hesaplanan,
            1,
            Math.Max(toplamDersSayisi - 1, 1)
        );
    }

    private async Task FavoriSenkronizeEtAsync(
        int kullaniciId,
        int kursId,
        bool favoriMi,
        int gunOnce)
    {
        var favori = await _context.Favoriler
            .FirstOrDefaultAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == kursId);

        if (!favoriMi)
        {
            if (favori != null)
            {
                _context.Favoriler.Remove(favori);
                await _context.SaveChangesAsync();
            }

            return;
        }

        DateTime eklenmeTarihi = DateTime.Now.AddDays(
            -Math.Max(gunOnce - 2, 1)
        );

        if (favori == null)
        {
            _context.Favoriler.Add(new Favori
            {
                KullaniciId = kullaniciId,
                KursId = kursId,
                EklenmeTarihi = eklenmeTarihi
            });

            await _context.SaveChangesAsync();

            return;
        }

        if (favori.EklenmeTarihi == default)
        {
            favori.EklenmeTarihi = eklenmeTarihi;
            await _context.SaveChangesAsync();
        }
    }

    private async Task DegerlendirmeSenkronizeEtAsync(
        int kullaniciId,
        int kursId,
        byte? puan,
        string? yorumMetni,
        int gunOnce)
    {
        var degerlendirme = await _context.KursDegerlendirmeleri
            .FirstOrDefaultAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == kursId);

        if (!puan.HasValue)
        {
            if (degerlendirme != null)
            {
                _context.KursDegerlendirmeleri.Remove(degerlendirme);
                await _context.SaveChangesAsync();
            }

            return;
        }

        byte guvenliPuan = (byte)Math.Clamp((int)puan.Value, 1, 5);

        string? temizYorum = string.IsNullOrWhiteSpace(yorumMetni)
            ? null
            : yorumMetni.Trim();

        DateTime degerlendirmeTarihi = DateTime.Now.AddDays(
            -Math.Max(gunOnce - 5, 1)
        );

        if (degerlendirme == null)
        {
            _context.KursDegerlendirmeleri.Add(new KursDegerlendirmesi
            {
                KullaniciId = kullaniciId,
                KursId = kursId,
                Puan = guvenliPuan,
                YorumMetni = temizYorum,
                DegerlendirmeTarihi = degerlendirmeTarihi
            });

            await _context.SaveChangesAsync();

            return;
        }

        degerlendirme.Puan = guvenliPuan;
        degerlendirme.YorumMetni = temizYorum;

        if (degerlendirme.DegerlendirmeTarihi == default)
        {
            degerlendirme.DegerlendirmeTarihi = degerlendirmeTarihi;
        }

        await _context.SaveChangesAsync();
    }
}