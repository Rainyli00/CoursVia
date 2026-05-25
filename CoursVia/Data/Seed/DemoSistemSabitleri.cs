namespace CoursVia.Data.Seed;

public static class DemoSistemSabitleri
{
    public static readonly IReadOnlyList<DemoBildirimBilgisi> Bildirimler =
    [
        new(
            KullaniciEposta: "admin1@coursvia.com",
            BildirimTipAdi: "Bilgilendirme",
            Baslik: "Demo sistem verileri hazırlandı",
            Mesaj: "CoursVia demo kullanıcıları, kursları, öğrenci ilerlemeleri, sınavlar ve sistem kayıtları başarıyla oluşturuldu.",
            GunOnce: 1,
            OkunduMu: false
        ),
        new(
            KullaniciEposta: "admin2@coursvia.com",
            BildirimTipAdi: "Uyarı",
            Baslik: "Onay bekleyen kurslar var",
            Mesaj: "HTML, CSS ve Modern Web Tasarımı ile Girişimcilik ve İş Modeli Geliştirme kursları admin onayı bekliyor.",
            GunOnce: 2,
            OkunduMu: false
        ),
        new(
            KullaniciEposta: "admin3@coursvia.com",
            BildirimTipAdi: "Bilgilendirme",
            Baslik: "Yeni değerlendirmeler eklendi",
            Mesaj: "Öğrenciler tarafından tamamlanan kurslara ait puan ve yorum kayıtları demo verisine eklendi.",
            GunOnce: 3,
            OkunduMu: true
        ),

        new(
            KullaniciEposta: "egitmen1@coursvia.com",
            BildirimTipAdi: "Bilgilendirme",
            Baslik: "Kurs performansınız güncellendi",
            Mesaj: "ASP.NET Core MVC ile Web Geliştirme kursunuzda öğrenci ilerleme ve değerlendirme kayıtları oluşturuldu.",
            GunOnce: 2,
            OkunduMu: false
        ),
        new(
            KullaniciEposta: "egitmen2@coursvia.com",
            BildirimTipAdi: "Bilgilendirme",
            Baslik: "AI analiz önerisi hazır",
            Mesaj: "Python ile Veri Analizi ve Yapay Zeka Temelleri kursları için demo AI analiz önerileri oluşturuldu.",
            GunOnce: 2,
            OkunduMu: false
        ),
        new(
            KullaniciEposta: "egitmen3@coursvia.com",
            BildirimTipAdi: "Uyarı",
            Baslik: "Kurs düzeltme süreci",
            Mesaj: "Akademik Yazma Becerileri kursu düzeltme isteniyor durumunda görünecek şekilde demo verisine eklendi.",
            GunOnce: 4,
            OkunduMu: true
        ),
        new(
            KullaniciEposta: "egitmen4@coursvia.com",
            BildirimTipAdi: "Bilgilendirme",
            Baslik: "Onay bekleyen kursunuz var",
            Mesaj: "Girişimcilik ve İş Modeli Geliştirme kursunuz admin onayı bekliyor.",
            GunOnce: 3,
            OkunduMu: false
        ),
        new(
            KullaniciEposta: "egitmen5@coursvia.com",
            BildirimTipAdi: "Bilgilendirme",
            Baslik: "Yeni öğrenci değerlendirmesi",
            Mesaj: "Canva ile Görsel Tasarım kursunuz için öğrencilerden yeni puan ve yorumlar eklendi.",
            GunOnce: 2,
            OkunduMu: false
        ),

        new(
            KullaniciEposta: "ogrenci1@coursvia.com",
            BildirimTipAdi: "Bilgilendirme",
            Baslik: "Sertifikan hazır",
            Mesaj: "ASP.NET Core MVC ile Web Geliştirme kursunu başarıyla tamamladın. Sertifikan sistemde oluşturuldu.",
            GunOnce: 1,
            OkunduMu: false
        ),
        new(
            KullaniciEposta: "ogrenci3@coursvia.com",
            BildirimTipAdi: "Bilgilendirme",
            Baslik: "Sınav başarın kaydedildi",
            Mesaj: "JavaScript Temelleri sınav sonucunuz başarıyla işlendi ve kurs ilerlemeniz güncellendi.",
            GunOnce: 1,
            OkunduMu: true
        ),
        new(
            KullaniciEposta: "ogrenci5@coursvia.com",
            BildirimTipAdi: "Bilgilendirme",
            Baslik: "Yeni sertifika oluşturuldu",
            Mesaj: "Canva ile Görsel Tasarım kursunu tamamladığın için sertifika kaydın oluşturuldu.",
            GunOnce: 1,
            OkunduMu: false
        ),
        new(
            KullaniciEposta: "ogrenci7@coursvia.com",
            BildirimTipAdi: "Uyarı",
            Baslik: "Sınav sonucu analiz edildi",
            Mesaj: "Temel Matematik ve Problem Çözme sınavında zorlandığın konular için AI çalışma önerisi oluşturuldu.",
            GunOnce: 1,
            OkunduMu: false
        ),
        new(
            KullaniciEposta: "ogrenci8@coursvia.com",
            BildirimTipAdi: "Bilgilendirme",
            Baslik: "Python kursu tamamlandı",
            Mesaj: "Python ile Veri Analizi kursunu başarıyla tamamladın. Sertifika kaydın oluşturuldu.",
            GunOnce: 1,
            OkunduMu: true
        )
    ];

    public static readonly IReadOnlyList<DemoKursOnayBilgisi> KursOnaylari =
    [
        new(
            KursAdi: "ASP.NET Core MVC ile Web Geliştirme",
            AdminEposta: "admin1@coursvia.com",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            Aciklama: "Demo veri: Kurs içeriği, bölümler, dersler ve sınav yapısı uygun bulundu. Yayına alındı.",
            GunOnce: 24
        ),
        new(
            KursAdi: "JavaScript Temelleri",
            AdminEposta: "admin2@coursvia.com",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            Aciklama: "Demo veri: JavaScript temel akışı ve uygulama dersleri yeterli görüldü. Kurs onaylandı.",
            GunOnce: 22
        ),
        new(
            KursAdi: "HTML, CSS ve Modern Web Tasarımı",
            AdminEposta: "admin3@coursvia.com",
            DurumId: DemoSeedSabitleri.DurumOnayBekliyor,
            Aciklama: "Demo veri: Kurs admin onay sürecinde bekletiliyor.",
            GunOnce: 3
        ),
        new(
            KursAdi: "Python ile Veri Analizi",
            AdminEposta: "admin1@coursvia.com",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            Aciklama: "Demo veri: Veri analizi kursu yayın standartlarına uygun bulundu.",
            GunOnce: 20
        ),
        new(
            KursAdi: "Yapay Zeka Temelleri",
            AdminEposta: "admin2@coursvia.com",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            Aciklama: "Demo veri: Yapay zeka temel kavramları ve etik bölümü yeterli görüldü. Kurs onaylandı.",
            GunOnce: 19
        ),
        new(
            KursAdi: "Temel Matematik ve Problem Çözme",
            AdminEposta: "admin4@coursvia.com",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            Aciklama: "Demo veri: Matematik kursu öğrenme hedefleriyle uyumlu bulundu. Yayına alındı.",
            GunOnce: 18
        ),
        new(
            KursAdi: "İngilizce Konuşma Pratiği",
            AdminEposta: "admin2@coursvia.com",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            Aciklama: "Demo veri: Dil eğitimi kursu onaylandı.",
            GunOnce: 17
        ),
        new(
            KursAdi: "Etkili İletişim ve Sunum Teknikleri",
            AdminEposta: "admin5@coursvia.com",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            Aciklama: "Demo veri: Kişisel gelişim kursu yayın için uygun bulundu.",
            GunOnce: 16
        ),
        new(
            KursAdi: "Akademik Yazma Becerileri",
            AdminEposta: "admin3@coursvia.com",
            DurumId: DemoSeedSabitleri.DurumKursDuzeltmeIsteniyor,
            Aciklama: "Demo veri: Kaynak kullanımı ve akademik yazım örnekleri güçlendirilmelidir.",
            GunOnce: 5
        ),
        new(
            KursAdi: "Temel Finans Okuryazarlığı",
            AdminEposta: "admin4@coursvia.com",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            Aciklama: "Demo veri: Finans okuryazarlığı kursu onaylandı.",
            GunOnce: 15
        ),
        new(
            KursAdi: "Dijital Pazarlamaya Giriş",
            AdminEposta: "admin5@coursvia.com",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            Aciklama: "Demo veri: Dijital pazarlama kursu yayına alındı.",
            GunOnce: 14
        ),
        new(
            KursAdi: "Girişimcilik ve İş Modeli Geliştirme",
            AdminEposta: "admin1@coursvia.com",
            DurumId: DemoSeedSabitleri.DurumOnayBekliyor,
            Aciklama: "Demo veri: Kurs admin onay sürecinde bekliyor.",
            GunOnce: 4
        ),
        new(
            KursAdi: "Canva ile Görsel Tasarım",
            AdminEposta: "admin2@coursvia.com",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            Aciklama: "Demo veri: Tasarım kursu yayın için uygun bulundu.",
            GunOnce: 13
        )
    ];

    public static readonly IReadOnlyList<DemoAdminLogBilgisi> AdminLoglari =
    [
        new("admin1@coursvia.com", "Sistem", "Demo veri seti başlatıldı: temel tablolar, kullanıcılar ve kategoriler oluşturuldu.", "127.0.0.1", 8),
        new("admin1@coursvia.com", "Kullanıcı İşlemleri", "5 admin, 5 eğitmen ve 10 öğrenci demo kullanıcısı oluşturuldu.", "127.0.0.1", 7),
        new("admin2@coursvia.com", "Kurs", "Demo kurs içerikleri, bölümler, dersler ve materyaller oluşturuldu.", "127.0.0.1", 6),
        new("admin3@coursvia.com", "Kurs Onayları", "HTML, CSS ve Modern Web Tasarımı kursu onay bekliyor durumuna alındı.", "127.0.0.1", 5),
        new("admin4@coursvia.com", "Kurs Onayları", "Temel Matematik ve Problem Çözme kursu yayınlandı.", "127.0.0.1", 5),
        new("admin5@coursvia.com", "Kurs Onayları", "Dijital Pazarlamaya Giriş kursu yayınlandı.", "127.0.0.1", 4),
        new("admin3@coursvia.com", "Kurs Onayları", "Akademik Yazma Becerileri kursu için düzeltme istendi.", "127.0.0.1", 4),
        new("admin2@coursvia.com", "Eğitmen Başvuruları", "Demo eğitmen profilleri onaylandı ve eğitmen rolüyle eşleştirildi.", "127.0.0.1", 3),
        new("admin1@coursvia.com", "Sistem", "Sınavlar, sorular, sınav katılımları ve sertifika kayıtları oluşturuldu.", "127.0.0.1", 2),
        new("admin2@coursvia.com", "Sistem", "Bildirimler, AI önerileri ve admin log demo kayıtları oluşturuldu.", "127.0.0.1", 1)
    ];

    public static readonly IReadOnlyList<DemoOneriBilgisi> Oneriler =
    [
        new(
            KullaniciEposta: "egitmen1@coursvia.com",
            OneriTipAdi: "Eğitmen Kurs Analizi",
            KursAdi: "ASP.NET Core MVC ile Web Geliştirme",
            OneriMetni:
@"[DEMO] Genel Kurs Yorumu:
ASP.NET Core MVC ile Web Geliştirme kursunda öğrencilerin büyük bölümü temel MVC yapısını başarıyla tamamlamış görünüyor. Özellikle Controller, ViewModel ve rol bazlı erişim dersleri kursun güçlü taraflarıdır.

Zorlanılan Alan:
Öğrencilerin Entity Framework Core ilişkileri, Include kullanımı ve migration mantığında daha fazla örneğe ihtiyaç duyduğu görülüyor.

Geliştirme Önerisi:
Veritabanı ilişkileri bölümüne ek bir mini proje akışı eklenebilir. Öğrencilere kurs, bölüm, ders ve kayıt ilişkilerini gösteren küçük bir örnek senaryo sunmak kurs başarısını artırabilir."
        ),
        new(
            KullaniciEposta: "egitmen2@coursvia.com",
            OneriTipAdi: "Eğitmen Kurs Analizi",
            KursAdi: "Python ile Veri Analizi",
            OneriMetni:
@"[DEMO] Genel Kurs Yorumu:
Python ile Veri Analizi kursunda veri okuma, temizleme ve gruplama dersleri öğrenciler tarafından güçlü şekilde ilerletilmiş görünüyor.

Zorlanılan Alan:
Öğrenciler veri tiplerini düzenleme ve kategorik veri analizi derslerinde daha fazla örnek ihtiyacı gösterebilir.

Geliştirme Önerisi:
Gerçekçi bir CSV veri seti üzerinden baştan sona analiz yapılan ek bir uygulama dersi eklenmesi önerilir."
        ),
        new(
            KullaniciEposta: "egitmen2@coursvia.com",
            OneriTipAdi: "Eğitmen Kurs Analizi",
            KursAdi: "Yapay Zeka Temelleri",
            OneriMetni:
@"[DEMO] Genel Kurs Yorumu:
Yapay Zeka Temelleri kursu kavramsal anlatım açısından güçlü. Veri kalitesi, etik ve prompt mantığı bölümleri öğrencinin AI okuryazarlığını destekliyor.

Zorlanılan Alan:
Model başarısı ölçümü ve veri gizliliği konuları daha somut senaryolarla desteklenebilir.

Geliştirme Önerisi:
Yanlış AI çıktısı tespiti, veri gizliliği riski ve güvenli prompt örnekleriyle kısa vaka dersleri eklenebilir."
        ),
        new(
            KullaniciEposta: "egitmen5@coursvia.com",
            OneriTipAdi: "Eğitmen Kurs Analizi",
            KursAdi: "Canva ile Görsel Tasarım",
            OneriMetni:
@"[DEMO] Genel Kurs Yorumu:
Canva ile Görsel Tasarım kursu öğrenciler tarafından yüksek tamamlanma ve güçlü değerlendirme alan bir kurs gibi konumlandırıldı.

Zorlanılan Alan:
Öğrencilerin marka uyumu ve mini marka kiti konularında daha fazla örnek görmesi faydalı olabilir.

Geliştirme Önerisi:
Aynı marka için sosyal medya gönderisi, sunum kapağı ve afiş tasarımını kapsayan bütünleşik bir uygulama dersi eklenebilir."
        ),
        new(
            KullaniciEposta: "ogrenci7@coursvia.com",
            OneriTipAdi: "Öğrenci Çalışma Önerisi",
            KursAdi: "Temel Matematik ve Problem Çözme",
            OneriMetni:
@"[DEMO] Genel Durum:
Temel Matematik ve Problem Çözme sınavında geçme notunun altında kaldın. Sistem verilerine göre yanlışların özellikle problem okuma, denklem kurma ve yüzde hesapları çevresinde yoğunlaşıyor.

Tekrar Etmen Gereken Dersler:
Öncelikle Problem Okuma Teknikleri, Denklem Kurma ve Birinci Dereceden Denklemler derslerini tekrar etmelisin. Bu dersler sınavdaki yanlış cevapların ana konularıyla doğrudan ilişkili.

Çalışma Önerisi:
Kurs içindeki ilgili ders videolarını ve kaynaklarını tekrar gözden geçir. Ardından aynı konulara ait örnek soruları çözerek yanlış yaptığın soru tiplerini yeniden değerlendir."
        )
    ];

    public sealed record DemoBildirimBilgisi(
        string KullaniciEposta,
        string BildirimTipAdi,
        string Baslik,
        string Mesaj,
        int GunOnce,
        bool OkunduMu
    );

    public sealed record DemoKursOnayBilgisi(
        string KursAdi,
        string AdminEposta,
        int DurumId,
        string Aciklama,
        int GunOnce
    );

    public sealed record DemoAdminLogBilgisi(
        string AdminEposta,
        string IslemTipAdi,
        string Aciklama,
        string IpAdresi,
        int GunOnce
    );

    public sealed record DemoOneriBilgisi(
        string KullaniciEposta,
        string OneriTipAdi,
        string KursAdi,
        string OneriMetni
    );
}