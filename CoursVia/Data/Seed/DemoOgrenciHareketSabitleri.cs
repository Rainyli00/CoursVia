namespace CoursVia.Data.Seed;

public static class DemoOgrenciHareketSabitleri
{
    public static readonly IReadOnlyList<DemoOgrenciKursSenaryosu> KursSenaryolari =
    [
        // =========================================================
        // Ali Kaya - yazılım + finans, güçlü demo öğrenci
        // =========================================================
        new(
            OgrenciEposta: "ogrenci1@coursvia.com",
            KursAdi: "ASP.NET Core MVC ile Web Geliştirme",
            IlerlemeYuzdesi: 100,
            KayitGunOnce: 32,
            FavoriMi: true,
            Puan: 5,
            YorumMetni: "MVC, Entity Framework Core ve rol bazlı panel mantığı çok düzenli anlatılmış. Gerçek proje geliştirirken takip edilebilecek güçlü bir kurs."
        ),
        new(
            OgrenciEposta: "ogrenci1@coursvia.com",
            KursAdi: "JavaScript Temelleri",
            IlerlemeYuzdesi: 76,
            KayitGunOnce: 22,
            FavoriMi: true,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci1@coursvia.com",
            KursAdi: "Yapay Zeka Temelleri",
            IlerlemeYuzdesi: 44,
            KayitGunOnce: 13,
            FavoriMi: false,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci1@coursvia.com",
            KursAdi: "Temel Finans Okuryazarlığı",
            IlerlemeYuzdesi: 100,
            KayitGunOnce: 34,
            FavoriMi: false,
            Puan: 4,
            YorumMetni: "Finans terimleri sade anlatılmış. Bütçe ve tasarruf konuları günlük hayata uygun örneklerle ilerliyor."
        ),

        // =========================================================
        // Ece Yıldız - dil, iletişim, tasarım
        // =========================================================
        new(
            OgrenciEposta: "ogrenci2@coursvia.com",
            KursAdi: "İngilizce Konuşma Pratiği",
            IlerlemeYuzdesi: 100,
            KayitGunOnce: 29,
            FavoriMi: true,
            Puan: 5,
            YorumMetni: "Konuşma kalıpları, kısa diyaloglar ve telaffuz dersleri çok faydalıydı. Dersler akıcı ilerliyor."
        ),
        new(
            OgrenciEposta: "ogrenci2@coursvia.com",
            KursAdi: "Canva ile Görsel Tasarım",
            IlerlemeYuzdesi: 68,
            KayitGunOnce: 18,
            FavoriMi: true,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci2@coursvia.com",
            KursAdi: "Etkili İletişim ve Sunum Teknikleri",
            IlerlemeYuzdesi: 38,
            KayitGunOnce: 10,
            FavoriMi: false,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci2@coursvia.com",
            KursAdi: "Akademik Yazma Becerileri",
            IlerlemeYuzdesi: 52,
            KayitGunOnce: 16,
            FavoriMi: false,
            Puan: null,
            YorumMetni: null
        ),

        // =========================================================
        // Burak Şahin - yazılım/veri/AI ağırlıklı
        // =========================================================
        new(
            OgrenciEposta: "ogrenci3@coursvia.com",
            KursAdi: "JavaScript Temelleri",
            IlerlemeYuzdesi: 100,
            KayitGunOnce: 31,
            FavoriMi: true,
            Puan: 5,
            YorumMetni: "DOM işlemleri ve mini uygulamalar kısmı özellikle çok öğreticiydi. Örnekler gerçek kullanıma yakın."
        ),
        new(
            OgrenciEposta: "ogrenci3@coursvia.com",
            KursAdi: "Python ile Veri Analizi",
            IlerlemeYuzdesi: 83,
            KayitGunOnce: 24,
            FavoriMi: true,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci3@coursvia.com",
            KursAdi: "ASP.NET Core MVC ile Web Geliştirme",
            IlerlemeYuzdesi: 41,
            KayitGunOnce: 12,
            FavoriMi: false,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci3@coursvia.com",
            KursAdi: "Yapay Zeka Temelleri",
            IlerlemeYuzdesi: 100,
            KayitGunOnce: 35,
            FavoriMi: false,
            Puan: 4,
            YorumMetni: "Yapay zeka kavramlarını başlangıç seviyesinde iyi toparlıyor. Özellikle veri kalitesi bölümü anlaşılır."
        ),

        // =========================================================
        // Deniz Aksoy - matematik + veri + pazarlama
        // =========================================================
        new(
            OgrenciEposta: "ogrenci4@coursvia.com",
            KursAdi: "Temel Matematik ve Problem Çözme",
            IlerlemeYuzdesi: 64,
            KayitGunOnce: 19,
            FavoriMi: true,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci4@coursvia.com",
            KursAdi: "Python ile Veri Analizi",
            IlerlemeYuzdesi: 28,
            KayitGunOnce: 9,
            FavoriMi: false,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci4@coursvia.com",
            KursAdi: "Dijital Pazarlamaya Giriş",
            IlerlemeYuzdesi: 100,
            KayitGunOnce: 25,
            FavoriMi: true,
            Puan: 4,
            YorumMetni: "Pazarlama hunisi ve kampanya planlama dersleri sade ve uygulanabilir anlatılmış."
        ),

        // =========================================================
        // Melis Acar - iletişim + tasarım
        // =========================================================
        new(
            OgrenciEposta: "ogrenci5@coursvia.com",
            KursAdi: "Etkili İletişim ve Sunum Teknikleri",
            IlerlemeYuzdesi: 100,
            KayitGunOnce: 28,
            FavoriMi: true,
            Puan: 5,
            YorumMetni: "Sunum hazırlığı, beden dili ve sahne heyecanı bölümleri çok başarılıydı."
        ),
        new(
            OgrenciEposta: "ogrenci5@coursvia.com",
            KursAdi: "Canva ile Görsel Tasarım",
            IlerlemeYuzdesi: 100,
            KayitGunOnce: 24,
            FavoriMi: true,
            Puan: 5,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci5@coursvia.com",
            KursAdi: "İngilizce Konuşma Pratiği",
            IlerlemeYuzdesi: 48,
            KayitGunOnce: 12,
            FavoriMi: false,
            Puan: null,
            YorumMetni: null
        ),

        // =========================================================
        // Can Demir - finans, pazarlama, AI
        // =========================================================
        new(
            OgrenciEposta: "ogrenci6@coursvia.com",
            KursAdi: "Temel Finans Okuryazarlığı",
            IlerlemeYuzdesi: 77,
            KayitGunOnce: 20,
            FavoriMi: true,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci6@coursvia.com",
            KursAdi: "Dijital Pazarlamaya Giriş",
            IlerlemeYuzdesi: 100,
            KayitGunOnce: 30,
            FavoriMi: true,
            Puan: 4,
            YorumMetni: "Dijital kanallar ve performans ölçümü bölümleri sunum için bile kullanılabilecek kadar net."
        ),
        new(
            OgrenciEposta: "ogrenci6@coursvia.com",
            KursAdi: "Yapay Zeka Temelleri",
            IlerlemeYuzdesi: 22,
            KayitGunOnce: 7,
            FavoriMi: false,
            Puan: null,
            YorumMetni: null
        ),

        // =========================================================
        // Selin Koç - düşük ilerleme + tamamlanmış matematik
        // =========================================================
        new(
            OgrenciEposta: "ogrenci7@coursvia.com",
            KursAdi: "ASP.NET Core MVC ile Web Geliştirme",
            IlerlemeYuzdesi: 18,
            KayitGunOnce: 6,
            FavoriMi: true,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci7@coursvia.com",
            KursAdi: "JavaScript Temelleri",
            IlerlemeYuzdesi: 32,
            KayitGunOnce: 8,
            FavoriMi: false,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci7@coursvia.com",
            KursAdi: "Temel Matematik ve Problem Çözme",
            IlerlemeYuzdesi: 100,
            KayitGunOnce: 27,
            FavoriMi: false,
            Puan: 3,
            YorumMetni: "Problem çözme bölümü faydalıydı, bazı konularda daha fazla örnek olabilirdi."
        ),

        // =========================================================
        // Mert Özkan - veri + AI + pazarlama + akademik yazma
        // =========================================================
        new(
            OgrenciEposta: "ogrenci8@coursvia.com",
            KursAdi: "Python ile Veri Analizi",
            IlerlemeYuzdesi: 100,
            KayitGunOnce: 33,
            FavoriMi: true,
            Puan: 5,
            YorumMetni: "Veri temizleme ve gruplama konuları çok iyi anlatılmış. Mini analiz projesi faydalı."
        ),
        new(
            OgrenciEposta: "ogrenci8@coursvia.com",
            KursAdi: "Yapay Zeka Temelleri",
            IlerlemeYuzdesi: 69,
            KayitGunOnce: 15,
            FavoriMi: true,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci8@coursvia.com",
            KursAdi: "Dijital Pazarlamaya Giriş",
            IlerlemeYuzdesi: 51,
            KayitGunOnce: 12,
            FavoriMi: false,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci8@coursvia.com",
            KursAdi: "Akademik Yazma Becerileri",
            IlerlemeYuzdesi: 25,
            KayitGunOnce: 6,
            FavoriMi: false,
            Puan: null,
            YorumMetni: null
        ),

        // =========================================================
        // İrem Polat - dil ve iletişim
        // =========================================================
        new(
            OgrenciEposta: "ogrenci9@coursvia.com",
            KursAdi: "İngilizce Konuşma Pratiği",
            IlerlemeYuzdesi: 74,
            KayitGunOnce: 15,
            FavoriMi: true,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci9@coursvia.com",
            KursAdi: "Akademik Yazma Becerileri",
            IlerlemeYuzdesi: 60,
            KayitGunOnce: 20,
            FavoriMi: true,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci9@coursvia.com",
            KursAdi: "Etkili İletişim ve Sunum Teknikleri",
            IlerlemeYuzdesi: 100,
            KayitGunOnce: 35,
            FavoriMi: false,
            Puan: 4,
            YorumMetni: "Sunum planı, görsel destek kullanımı ve kapanış dersleri çok işime yaradı."
        ),

        // =========================================================
        // Oğuzhan Kılıç - geniş kayıtlı karma profil
        // =========================================================
        new(
            OgrenciEposta: "ogrenci10@coursvia.com",
            KursAdi: "ASP.NET Core MVC ile Web Geliştirme",
            IlerlemeYuzdesi: 86,
            KayitGunOnce: 24,
            FavoriMi: true,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci10@coursvia.com",
            KursAdi: "Temel Finans Okuryazarlığı",
            IlerlemeYuzdesi: 42,
            KayitGunOnce: 11,
            FavoriMi: false,
            Puan: null,
            YorumMetni: null
        ),
        new(
            OgrenciEposta: "ogrenci10@coursvia.com",
            KursAdi: "Canva ile Görsel Tasarım",
            IlerlemeYuzdesi: 100,
            KayitGunOnce: 30,
            FavoriMi: true,
            Puan: 5,
            YorumMetni: "Canva şablonları, sunum tasarımı ve marka kiti bölümleri çok anlaşılırdı."
        ),
        new(
            OgrenciEposta: "ogrenci10@coursvia.com",
            KursAdi: "Temel Matematik ve Problem Çözme",
            IlerlemeYuzdesi: 57,
            KayitGunOnce: 14,
            FavoriMi: false,
            Puan: null,
            YorumMetni: null
        )
    ];

    public sealed record DemoOgrenciKursSenaryosu(
        string OgrenciEposta,
        string KursAdi,
        int IlerlemeYuzdesi,
        int KayitGunOnce,
        bool FavoriMi,
        byte? Puan,
        string? YorumMetni
    );
}