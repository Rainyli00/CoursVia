namespace CoursVia.Data.Seed;

public static class DemoSeedSabitleri
{
    public const string DemoSifre = "CoursVia123*";

    // Ortak durumlar
    public const int DurumAktif = 1;
    public const int DurumPasif = 2;

    // Kurs durumları
    public const int DurumTaslak = 3;
    public const int DurumOnayBekliyor = 4;
    public const int DurumYayinda = 5;
    public const int DurumReddedildi = 6;
    public const int DurumKursDuzeltmeIsteniyor = 7;

    // Eğitmen profili durumları
    // Eğitmen tarafında Düzeltme İsteniyor kullanılmayacak.
    public const int DurumEgitmenOnayBekliyor = 4;
    public const int DurumEgitmenReddedildi = 6;
    public const int DurumEgitmenOnaylandi = 8;

    public const int RolAdmin = 1;
    public const int RolEgitmen = 2;
    public const int RolOgrenci = 3;

    public const int MateryalTipDokuman = 1;
    public const int MateryalTipGorsel = 2;
    public const int MateryalTipSes = 3;
    public const int MateryalTipVideo = 4;
    public const int MateryalTipKod = 5;

    public const int BildirimTipBilgilendirme = 1;
    public const int BildirimTipUyari = 2;

    public const int OneriTipEgitmenKursAnalizi = 1;
    public const int OneriTipOgrenciCalismaOnerisi = 2;

    public const int IslemTipKullanici = 1;
    public const int IslemTipKursOnayi = 2;
    public const int IslemTipEgitmenBasvurusu = 3;
    public const int IslemTipKurs = 4;
    public const int IslemTipSistemKullanici = 5;

    public static readonly IReadOnlyList<DemoKullaniciBilgisi> Adminler =
    [
        new("Mert", "Yıldırım", "admin1@coursvia.com", "05000000001", RolAdmin),
        new("Ayşe", "Karakaya", "admin2@coursvia.com", "05000000002", RolAdmin),
        new("Emre", "Çetin", "admin3@coursvia.com", "05000000003", RolAdmin),
        new("Zeynep", "Aydın", "admin4@coursvia.com", "05000000004", RolAdmin),
        new("Kerem", "Özdemir", "admin5@coursvia.com", "05000000005", RolAdmin)
    ];

    public static readonly IReadOnlyList<DemoKullaniciBilgisi> Egitmenler =
    [
        new("Ahmet", "Yılmaz", "egitmen1@coursvia.com", "05000000101", RolEgitmen),
        new("Elif", "Demir", "egitmen2@coursvia.com", "05000000102", RolEgitmen),
        new("Zeynep", "Arslan", "egitmen3@coursvia.com", "05000000103", RolEgitmen),
        new("Murat", "Kaya", "egitmen4@coursvia.com", "05000000104", RolEgitmen),
        new("Ayşe", "Çelik", "egitmen5@coursvia.com", "05000000105", RolEgitmen)
    ];

    public static readonly IReadOnlyList<DemoKullaniciBilgisi> Ogrenciler =
    [
        new("Ali", "Kaya", "ogrenci1@coursvia.com", "05000000201", RolOgrenci),
        new("Ece", "Yıldız", "ogrenci2@coursvia.com", "05000000202", RolOgrenci),
        new("Burak", "Şahin", "ogrenci3@coursvia.com", "05000000203", RolOgrenci),
        new("Deniz", "Aksoy", "ogrenci4@coursvia.com", "05000000204", RolOgrenci),
        new("Melis", "Acar", "ogrenci5@coursvia.com", "05000000205", RolOgrenci),
        new("Can", "Demir", "ogrenci6@coursvia.com", "05000000206", RolOgrenci),
        new("Selin", "Koç", "ogrenci7@coursvia.com", "05000000207", RolOgrenci),
        new("Mert", "Özkan", "ogrenci8@coursvia.com", "05000000208", RolOgrenci),
        new("İrem", "Polat", "ogrenci9@coursvia.com", "05000000209", RolOgrenci),
        new("Oğuzhan", "Kılıç", "ogrenci10@coursvia.com", "05000000210", RolOgrenci)
    ];

    public static readonly IReadOnlyList<DemoEgitmenProfilBilgisi> EgitmenProfilleri =
    [
        new(
            "egitmen1@coursvia.com",
            "Web geliştirme ve yazılım mimarisi",
            "ASP.NET Core, JavaScript ve modern web uygulamaları üzerine eğitimler veren deneyimli yazılım eğitmeni.",
            7,
            "https://coursvia.com/egitmen/ahmet-yilmaz",
            ["Yazılım Geliştirme", "Web Tasarım"]
        ),

        new(
            "egitmen2@coursvia.com",
            "Veri bilimi, yapay zeka ve matematiksel analiz",
            "Veri analizi, yapay zeka temelleri ve matematiksel düşünme üzerine uygulamalı eğitimler hazırlar.",
            6,
            "https://coursvia.com/egitmen/elif-demir",
            ["Veri Bilimi", "Yapay Zeka", "Matematik"]
        ),

        new(
            "egitmen3@coursvia.com",
            "Dil eğitimi ve iletişim",
            "İngilizce konuşma pratiği, sunum teknikleri ve akademik yazma becerileri üzerine eğitimler verir.",
            8,
            "https://coursvia.com/egitmen/zeynep-arslan",
            ["Dil Eğitimi", "Kişisel Gelişim"]
        ),

        new(
            "egitmen4@coursvia.com",
            "Finans, pazarlama ve girişimcilik",
            "Finans okuryazarlığı, pazarlama stratejileri ve girişimcilik alanlarında eğitim içerikleri üretir.",
            9,
            "https://coursvia.com/egitmen/murat-kaya",
            ["Finans", "Pazarlama", "Girişimcilik"]
        ),

        new(
            "egitmen5@coursvia.com",
            "Tasarım ve kişisel gelişim",
            "Görsel tasarım, Canva kullanımı, zaman yönetimi ve üretkenlik konularında uygulamalı eğitimler verir.",
            5,
            "https://coursvia.com/egitmen/ayse-celik",
            ["Tasarım", "Kişisel Gelişim"]
        )
    ];

    public static List<DemoKullaniciBilgisi> TumKullanicilar()
    {
        return Adminler
            .Concat(Egitmenler)
            .Concat(Ogrenciler)
            .ToList();
    }

    public sealed record DemoKullaniciBilgisi(
        string Ad,
        string Soyad,
        string Eposta,
        string Telefon,
        int RolId
    );

    public sealed record DemoEgitmenProfilBilgisi(
        string Eposta,
        string UzmanlikAlani,
        string Biyografi,
        int DeneyimYili,
        string WebsiteUrl,
        string[] Branslar
    );
}