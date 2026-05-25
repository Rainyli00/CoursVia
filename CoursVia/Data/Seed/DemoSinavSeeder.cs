using CoursVia.Models;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Data.Seed;

public class DemoSinavSeeder
{
    private readonly AppDbContext _context;

    public DemoSinavSeeder(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await SinavlariEkleVeyaGuncelleAsync();
            await SinavKatilimlariniVeSertifikalariEkleAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task SinavlariEkleVeyaGuncelleAsync()
    {
        var kursAdlari = DemoSinavSabitleri.Sinavlar
            .Select(x => x.KursAdi)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var kurslar = await _context.Kurslar
            .Where(x => kursAdlari.Contains(x.KursAdi))
            .ToListAsync();

        var kursSozluk = kurslar
            .ToDictionary(
                x => x.KursAdi,
                x => x,
                StringComparer.OrdinalIgnoreCase
            );

        foreach (var demoSinav in DemoSinavSabitleri.Sinavlar)
        {
            await SinavEkleVeyaGuncelleAsync(
                demoSinav,
                kursSozluk
            );
        }

        await _context.SaveChangesAsync();
    }

    private async Task SinavEkleVeyaGuncelleAsync(
        DemoSinavSabitleri.DemoSinavBilgisi demoSinav,
        Dictionary<string, Kurs> kursSozluk)
    {
        if (!kursSozluk.TryGetValue(demoSinav.KursAdi, out var kurs))
        {
            return;
        }

        if (kurs.DurumId == DemoSeedSabitleri.DurumTaslak)
        {
            return;
        }

        var sinav = await _context.Sinavlar
            .FirstOrDefaultAsync(x => x.KursId == kurs.KursId);

        if (sinav == null)
        {
            sinav = new Sinav
            {
                KursId = kurs.KursId,
                SinavAdi = demoSinav.SinavAdi,
                Aciklama = demoSinav.Aciklama,
                GecmeNotu = demoSinav.GecmeNotu,
                SureDakika = demoSinav.SureDakika,
                SoruSayisi = demoSinav.SoruSayisi,
                OlusturmaTarihi = DateTime.Now.AddDays(-Math.Max(demoSinav.GunOnce, 1))
            };

            _context.Sinavlar.Add(sinav);
            await _context.SaveChangesAsync();
        }
        else
        {
            sinav.SinavAdi = demoSinav.SinavAdi;
            sinav.Aciklama = demoSinav.Aciklama;
            sinav.GecmeNotu = demoSinav.GecmeNotu;
            sinav.SureDakika = demoSinav.SureDakika;
            sinav.SoruSayisi = demoSinav.SoruSayisi;

            if (sinav.OlusturmaTarihi == default)
            {
                sinav.OlusturmaTarihi = DateTime.Now.AddDays(-Math.Max(demoSinav.GunOnce, 1));
            }

            await _context.SaveChangesAsync();
        }

        await SorulariSenkronizeEtAsync(
            sinav.SinavId,
            kurs.KursId,
            demoSinav
        );
    }

    private async Task SorulariSenkronizeEtAsync(
        int sinavId,
        int kursId,
        DemoSinavSabitleri.DemoSinavBilgisi demoSinav)
    {
        var mevcutSorular = await _context.Sorular
            .Where(x => x.SinavId == sinavId)
            .ToListAsync();

        var hedefSoruMetinleri = demoSinav.Sorular
            .Select(x => x.SoruMetni)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pasifeAlinacakSorular = mevcutSorular
            .Where(x => !hedefSoruMetinleri.Contains(x.SoruMetni))
            .ToList();

        foreach (var soru in pasifeAlinacakSorular)
        {
            soru.AktifMi = false;
        }

        await _context.SaveChangesAsync();

        foreach (var demoSoru in demoSinav.Sorular)
        {
            await SoruEkleVeyaGuncelleAsync(
                sinavId,
                kursId,
                demoSoru
            );
        }

        await _context.SaveChangesAsync();
    }

    private async Task SoruEkleVeyaGuncelleAsync(
        int sinavId,
        int kursId,
        DemoSinavSabitleri.DemoSoruBilgisi demoSoru)
    {
        var soru = await _context.Sorular
            .FirstOrDefaultAsync(x =>
                x.SinavId == sinavId &&
                x.SoruMetni == demoSoru.SoruMetni);

        if (soru == null)
        {
            soru = new Soru
            {
                SinavId = sinavId,
                SoruMetni = demoSoru.SoruMetni,
                AktifMi = true
            };

            _context.Sorular.Add(soru);
            await _context.SaveChangesAsync();
        }
        else
        {
            soru.SoruMetni = demoSoru.SoruMetni;
            soru.AktifMi = true;

            await _context.SaveChangesAsync();
        }

        await SoruDersBaglantisiSenkronizeEtAsync(
            soru.SoruId,
            kursId,
            demoSoru.DersAdi
        );

        await SoruSecenekleriniSenkronizeEtAsync(
            soru.SoruId,
            demoSoru
        );
    }

    private async Task SoruDersBaglantisiSenkronizeEtAsync(
        int soruId,
        int kursId,
        string dersAdi)
    {
        var ders = await _context.Dersler
            .Where(x =>
                x.KursId == kursId &&
                x.AktifMi &&
                !x.SistemDersiMi &&
                x.DersAdi == dersAdi)
            .OrderBy(x => x.SiraNo)
            .FirstOrDefaultAsync();

        if (ders == null)
        {
            ders = await _context.Dersler
                .Where(x =>
                    x.KursId == kursId &&
                    x.AktifMi &&
                    !x.SistemDersiMi)
                .OrderBy(x => x.SiraNo)
                .FirstOrDefaultAsync();
        }

        if (ders == null)
        {
            return;
        }

        var mevcutBaglantilar = await _context.SoruDersleri
            .Where(x => x.SoruId == soruId)
            .ToListAsync();

        var silinecekBaglantilar = mevcutBaglantilar
            .Where(x => x.DersId != ders.DersId)
            .ToList();

        if (silinecekBaglantilar.Count > 0)
        {
            _context.SoruDersleri.RemoveRange(silinecekBaglantilar);
        }

        bool hedefBaglantiVarMi = mevcutBaglantilar
            .Any(x => x.DersId == ders.DersId);

        if (!hedefBaglantiVarMi)
        {
            _context.SoruDersleri.Add(new SoruDersi
            {
                SoruId = soruId,
                DersId = ders.DersId
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task SoruSecenekleriniSenkronizeEtAsync(
        int soruId,
        DemoSinavSabitleri.DemoSoruBilgisi demoSoru)
    {
        var mevcutSecenekler = await _context.SoruSecenekleri
            .Where(x => x.SoruId == soruId)
            .ToListAsync();

        var hedefSecenekMetinleri = demoSoru.Secenekler
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pasifeAlinacakSecenekler = mevcutSecenekler
            .Where(x => !hedefSecenekMetinleri.Contains(x.SecenekMetni))
            .ToList();

        foreach (var secenek in pasifeAlinacakSecenekler)
        {
            secenek.AktifMi = false;
            secenek.DogruMu = false;
        }

        for (int i = 0; i < demoSoru.Secenekler.Count; i++)
        {
            string secenekMetni = demoSoru.Secenekler[i];
            bool dogruMu = i + 1 == demoSoru.DogruSecenekNo;

            var secenek = mevcutSecenekler
                .FirstOrDefault(x =>
                    x.SecenekMetni.Equals(secenekMetni, StringComparison.OrdinalIgnoreCase));

            if (secenek == null)
            {
                _context.SoruSecenekleri.Add(new SoruSecenegi
                {
                    SoruId = soruId,
                    SecenekMetni = secenekMetni,
                    DogruMu = dogruMu,
                    AktifMi = true
                });

                continue;
            }

            secenek.SecenekMetni = secenekMetni;
            secenek.DogruMu = dogruMu;
            secenek.AktifMi = true;
        }

        await _context.SaveChangesAsync();
    }

    private async Task SinavKatilimlariniVeSertifikalariEkleAsync()
    {
        var aktifKayitlar = await _context.KursKayitlari
            .Include(x => x.Kullanici)
            .Include(x => x.Kurs)
            .Where(x => x.AktifMi)
            .ToListAsync();

        foreach (var kursKaydi in aktifKayitlar)
        {
            var sinav = await _context.Sinavlar
                .FirstOrDefaultAsync(x => x.KursId == kursKaydi.KursId);

            if (sinav == null)
            {
                kursKaydi.TamamlandiMi = false;
                kursKaydi.TamamlanmaTarihi = null;
                continue;
            }

            bool derslerinTamamiTamamlandiMi = await DerslerinTamamiTamamlandiMiAsync(
                kursKaydi.KursKayitId,
                kursKaydi.KursId
            );

            if (!derslerinTamamiTamamlandiMi)
            {
                await TamamlanmayanKaydiSenkronizeEtAsync(
                    kursKaydi,
                    sinav
                );

                continue;
            }

            int hedefPuan = DemoPuanGetir(
                kursKaydi.Kullanici.Eposta,
                kursKaydi.Kurs.KursAdi
            );

            var sinavKatilimi = await SinavKatilimiEkleVeyaGuncelleAsync(
                kursKaydi.KursKayitId,
                sinav.SinavId,
                sinav.SureDakika,
                sinav.GecmeNotu,
                hedefPuan
            );

            await OgrenciCevaplariniYenidenOlusturAsync(
                sinavKatilimi.SinavKatilimId,
                sinav.SinavId,
                sinav.SoruSayisi,
                hedefPuan
            );

            bool sinavdanGectiMi = hedefPuan >= sinav.GecmeNotu;

            kursKaydi.TamamlandiMi = sinavdanGectiMi;
            kursKaydi.TamamlanmaTarihi = sinavdanGectiMi
                ? sinavKatilimi.BitisTarihi ?? DateTime.Now.AddDays(-1)
                : null;

            await SertifikaSenkronizeEtAsync(
                kursKaydi.KullaniciId,
                kursKaydi.KursId,
                sinav.GecmeNotu,
                hedefPuan
            );
        }

        await _context.SaveChangesAsync();
    }

    private async Task<bool> DerslerinTamamiTamamlandiMiAsync(
        int kursKayitId,
        int kursId)
    {
        var aktifDersIdleri = await _context.Dersler
            .Where(x =>
                x.KursId == kursId &&
                x.AktifMi &&
                !x.SistemDersiMi)
            .Select(x => x.DersId)
            .ToListAsync();

        if (aktifDersIdleri.Count == 0)
        {
            return false;
        }

        int tamamlananDersSayisi = await _context.DersIlerlemeleri
            .CountAsync(x =>
                x.KursKayitId == kursKayitId &&
                aktifDersIdleri.Contains(x.DersId) &&
                x.TamamlandiMi);

        return tamamlananDersSayisi == aktifDersIdleri.Count;
    }

    private async Task TamamlanmayanKaydiSenkronizeEtAsync(
        KursKaydi kursKaydi,
        Sinav sinav)
    {
        kursKaydi.TamamlandiMi = false;
        kursKaydi.TamamlanmaTarihi = null;

        await SertifikaSenkronizeEtAsync(
            kursKaydi.KullaniciId,
            kursKaydi.KursId,
            sinav.GecmeNotu,
            0
        );

        await _context.SaveChangesAsync();
    }

    private async Task<SinavKatilimi> SinavKatilimiEkleVeyaGuncelleAsync(
        int kursKayitId,
        int sinavId,
        int sureDakika,
        int gecmeNotu,
        int hedefPuan)
    {
        var sinavKatilimi = await _context.SinavKatilimlari
            .FirstOrDefaultAsync(x =>
                x.KursKayitId == kursKayitId &&
                x.SinavId == sinavId);

        DateTime baslamaTarihi = DateTime.Now.AddDays(-2).AddMinutes(-sureDakika);
        DateTime bitisTarihi = DateTime.Now.AddDays(-2);

        bool gectiMi = hedefPuan >= gecmeNotu;

        if (sinavKatilimi == null)
        {
            sinavKatilimi = new SinavKatilimi
            {
                KursKayitId = kursKayitId,
                SinavId = sinavId,
                BaslamaTarihi = baslamaTarihi,
                BitisTarihi = bitisTarihi,
                AlinanPuan = hedefPuan,
                GectiMi = gectiMi
            };

            _context.SinavKatilimlari.Add(sinavKatilimi);
            await _context.SaveChangesAsync();

            return sinavKatilimi;
        }

        sinavKatilimi.BaslamaTarihi = baslamaTarihi;
        sinavKatilimi.BitisTarihi = bitisTarihi;
        sinavKatilimi.AlinanPuan = hedefPuan;
        sinavKatilimi.GectiMi = gectiMi;

        await _context.SaveChangesAsync();

        return sinavKatilimi;
    }

    private async Task OgrenciCevaplariniYenidenOlusturAsync(
        int sinavKatilimId,
        int sinavId,
        int soruSayisi,
        int hedefPuan)
    {
        var eskiCevaplar = await _context.OgrenciCevaplari
            .Where(x => x.SinavKatilimId == sinavKatilimId)
            .ToListAsync();

        if (eskiCevaplar.Count > 0)
        {
            _context.OgrenciCevaplari.RemoveRange(eskiCevaplar);
            await _context.SaveChangesAsync();
        }

        var sorular = await _context.Sorular
            .Include(x => x.SoruSecenekleri)
            .Where(x =>
                x.SinavId == sinavId &&
                x.AktifMi)
            .OrderBy(x => x.SoruId)
            .Take(soruSayisi)
            .ToListAsync();

        if (sorular.Count == 0)
        {
            return;
        }

        int dogruCevapSayisi = DogruCevapSayisiHesapla(
            sorular.Count,
            hedefPuan
        );

        for (int i = 0; i < sorular.Count; i++)
        {
            var soru = sorular[i];

            bool dogruMu = i < dogruCevapSayisi;

            var secenek = SecenekSec(
                soru.SoruSecenekleri.ToList(),
                dogruMu
            );

            if (secenek == null)
            {
                continue;
            }

            _context.OgrenciCevaplari.Add(new OgrenciCevabi
            {
                SinavKatilimId = sinavKatilimId,
                SoruId = soru.SoruId,
                SecenekId = secenek.SecenekId,
                DogruMu = dogruMu,
                VerilmeTarihi = DateTime.Now.AddDays(-2).AddMinutes(i + 1)
            });
        }

        await _context.SaveChangesAsync();
    }

    private static int DogruCevapSayisiHesapla(
        int toplamSoruSayisi,
        int hedefPuan)
    {
        if (toplamSoruSayisi <= 0)
        {
            return 0;
        }

        hedefPuan = Math.Clamp(hedefPuan, 0, 100);

        int dogruSayisi = (int)Math.Round(
            toplamSoruSayisi * (hedefPuan / 100.0),
            MidpointRounding.AwayFromZero
        );

        return Math.Clamp(dogruSayisi, 0, toplamSoruSayisi);
    }

    private static SoruSecenegi? SecenekSec(
        List<SoruSecenegi> secenekler,
        bool dogruSecenekIsteniyor)
    {
        var aktifSecenekler = secenekler
            .Where(x => x.AktifMi)
            .ToList();

        if (aktifSecenekler.Count == 0)
        {
            return null;
        }

        if (dogruSecenekIsteniyor)
        {
            return aktifSecenekler.FirstOrDefault(x => x.DogruMu)
                ?? aktifSecenekler.First();
        }

        return aktifSecenekler.FirstOrDefault(x => !x.DogruMu)
            ?? aktifSecenekler.First();
    }

    private async Task SertifikaSenkronizeEtAsync(
        int kullaniciId,
        int kursId,
        int gecmeNotu,
        int hedefPuan)
    {
        bool gectiMi = hedefPuan >= gecmeNotu;

        var sertifika = await _context.Sertifikalar
            .FirstOrDefaultAsync(x =>
                x.KullaniciId == kullaniciId &&
                x.KursId == kursId);

        if (!gectiMi)
        {
            if (sertifika != null &&
                sertifika.SertifikaKodu.StartsWith("CV-DEMO-", StringComparison.OrdinalIgnoreCase))
            {
                _context.Sertifikalar.Remove(sertifika);
                await _context.SaveChangesAsync();
            }

            return;
        }

        string sertifikaKodu = $"CV-DEMO-{kullaniciId:D3}-{kursId:D3}";

        if (sertifika == null)
        {
            _context.Sertifikalar.Add(new Sertifika
            {
                KullaniciId = kullaniciId,
                KursId = kursId,
                SertifikaKodu = sertifikaKodu,
                VerilmeTarihi = DateTime.Now.AddDays(-1)
            });

            await _context.SaveChangesAsync();

            return;
        }

        if (sertifika.SertifikaKodu.StartsWith("CV-DEMO-", StringComparison.OrdinalIgnoreCase))
        {
            sertifika.SertifikaKodu = sertifikaKodu;
        }

        if (sertifika.VerilmeTarihi == default)
        {
            sertifika.VerilmeTarihi = DateTime.Now.AddDays(-1);
        }

        await _context.SaveChangesAsync();
    }

    private static int DemoPuanGetir(
        string ogrenciEposta,
        string kursAdi)
    {
        return (ogrenciEposta, kursAdi) switch
        {
            ("ogrenci1@coursvia.com", "ASP.NET Core MVC ile Web Geliştirme") => 90,
            ("ogrenci1@coursvia.com", "Temel Finans Okuryazarlığı") => 80,

            ("ogrenci2@coursvia.com", "İngilizce Konuşma Pratiği") => 90,

            ("ogrenci3@coursvia.com", "JavaScript Temelleri") => 90,
            ("ogrenci3@coursvia.com", "Yapay Zeka Temelleri") => 80,

            ("ogrenci4@coursvia.com", "Dijital Pazarlamaya Giriş") => 80,

            ("ogrenci5@coursvia.com", "Etkili İletişim ve Sunum Teknikleri") => 90,
            ("ogrenci5@coursvia.com", "Canva ile Görsel Tasarım") => 90,

            ("ogrenci6@coursvia.com", "Dijital Pazarlamaya Giriş") => 80,

            // AI öneri demosu için özellikle başarısız öğrenci senaryosu.
            ("ogrenci7@coursvia.com", "Temel Matematik ve Problem Çözme") => 48,

            ("ogrenci8@coursvia.com", "Python ile Veri Analizi") => 90,

            ("ogrenci9@coursvia.com", "Etkili İletişim ve Sunum Teknikleri") => 90,

            ("ogrenci10@coursvia.com", "Canva ile Görsel Tasarım") => 90,

            _ => 80
        };
    }
}