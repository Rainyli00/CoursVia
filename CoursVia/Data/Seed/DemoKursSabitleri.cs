namespace CoursVia.Data.Seed;

public static class DemoKursSabitleri
{
    public const string DemoVarsayilanVideoUrl = "https://www.youtube.com/watch?v=ysz5S6PUM-U";
    public const string DemoPdfUrl = "https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf";
    public const string DemoKodUrl = "https://gist.github.com/";

    private static class Video
    {
        public const string AspNetMvc = "https://www.youtube.com/watch?v=AopeJjkcRvU";
        public const string AspNetMvcFull = "https://www.youtube.com/watch?v=RWXKysImabs";
        public const string AspNetMvcProject = "https://www.youtube.com/watch?v=QtiM87MV27w";

        public const string JavaScriptFull = "https://www.youtube.com/watch?v=EerdGm-ehJQ";
        public const string JavaScriptDom = "https://www.youtube.com/watch?v=0ik6X4DJKCc";
        public const string JavaScriptDomFull = "https://www.youtube.com/watch?v=5fb2aPlgoys";

        public const string HtmlCss = "https://www.youtube.com/watch?v=p0bGHP-PXD4";
        public const string ResponsiveDesign = "https://www.youtube.com/watch?v=srvUrASNj0s";
        public const string ResponsiveWebsite = "https://www.youtube.com/watch?v=wGVOtu0DoKk";

        public const string PythonDataAnalysis = "https://www.youtube.com/watch?v=r-uOLxNrNk8";
        public const string PandasBasics = "https://www.youtube.com/watch?v=2uvysYbKdjM";
        public const string PandasFull = "https://www.youtube.com/watch?v=vmEHCJofslg";

        public const string AiBasics = "https://www.youtube.com/watch?v=VGFpV3Qj4as";
        public const string GoogleAiBasics = "https://www.youtube.com/watch?v=Yq0QkCxoTHM";
        public const string AiFullCourse = "https://www.youtube.com/watch?v=JMUxmLyrhSk";

        public const string MathWordProblem = "https://www.youtube.com/watch?v=7vHnwYKqtz8";
        public const string MathEquations = "https://www.youtube.com/watch?v=7pCnFxnzWKs";
        public const string MathFoundations = "https://www.youtube.com/watch?v=F0X5xY_2c-c";

        public const string EnglishDaily = "https://www.youtube.com/watch?v=henIVlCPVIY";
        public const string EnglishSpeakingPractice = "https://www.youtube.com/watch?v=vkyUojDJmWM";
        public const string EnglishLongPractice = "https://www.youtube.com/watch?v=BbEE2XYUoRk";

        public const string PublicSpeaking = "https://www.youtube.com/watch?v=i5mYphUoOCs";
        public const string PresentationSkills = "https://www.youtube.com/watch?v=N5t3NTix1hw";
        public const string SpeechPractice = "https://www.youtube.com/watch?v=d812a7qG9Kw";

        public const string AcademicWriting = "https://www.youtube.com/watch?v=gFXE9n7hrOI";
        public const string AcademicPapers = "https://www.youtube.com/watch?v=Yiy0BfxIBnU";
        public const string AcademicWritingFull = "https://www.youtube.com/watch?v=TgSPz0vtbuI";

        public const string FinanceBasics = "https://www.youtube.com/watch?v=vJabNEwZIuc";
        public const string Budgeting = "https://www.youtube.com/watch?v=D9NRbQOQvbk";
        public const string PersonalFinance = "https://www.youtube.com/watch?v=WiH2T933xn8";

        public const string DigitalMarketing = "https://www.youtube.com/watch?v=Trq6EeN8wkQ";
        public const string DigitalMarketingFull = "https://www.youtube.com/watch?v=BZLUEKnMfIY";
        public const string DigitalMarketingCourse = "https://www.youtube.com/watch?v=5altc8xTzBg";

        public const string BusinessModelCanvas = "https://www.youtube.com/watch?v=jQ8rt9DxQOY";
        public const string BusinessModelCanvasShort = "https://www.youtube.com/watch?v=QoAOzMTLP5s";
        public const string BusinessModelGuide = "https://www.youtube.com/watch?v=LbbGRgGKSyg";

        public const string CanvaBeginner = "https://www.youtube.com/watch?v=jzWxBuvwuwQ";
        public const string CanvaDesign = "https://www.youtube.com/watch?v=q4OWKoUUjdY";
        public const string CanvaFull = "https://www.youtube.com/watch?v=Dgha6qBtAwQ";

        public const string TimeManagement = "https://www.youtube.com/watch?v=PJOH-vhn3NE";
        public const string TimeManagementTips = "https://www.youtube.com/watch?v=iONDebHX9qk";
        public const string Productivity = "https://www.youtube.com/watch?v=zhhaEbaVVuQ";
    }

    public static readonly IReadOnlyList<DemoKursBilgisi> Kurslar = new List<DemoKursBilgisi>
    {
        new DemoKursBilgisi(
            EgitmenEposta: "egitmen1@coursvia.com",
            KursAdi: "ASP.NET Core MVC ile Web Geliştirme",
            Aciklama: "ASP.NET Core MVC, Entity Framework Core ve modern arayüz yapısıyla profesyonel web uygulamaları geliştirmeyi öğren.",
            KategoriAdi: "Yazılım Geliştirme",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            GunOnce: 38,
            KapakGorselUrl: "https://images.unsplash.com/photo-1498050108023-c5249f4df085?auto=format&fit=crop&w=1200&q=80",
            Bolumler: new List<DemoBolumBilgisi>
            {
                new DemoBolumBilgisi(
                    "MVC Temelleri",
                    new List<DemoDersBilgisi>
                    {
                        D("ASP.NET Core MVC Nedir?", "MVC mimarisi, controller, view ve model kavramlarını temel seviyede öğren.", Video.AspNetMvc),
                        D("Proje Yapısını Tanıma", "Controllers, Views, Models, ViewModels ve wwwroot klasörlerinin görevlerini incele.", Video.AspNetMvc),
                        D("Routing ve Action Mantığı", "URL yönlendirme, controller action yapısı ve parametre alma mantığını öğren.", Video.AspNetMvcFull),
                        D("Layout ve Partial View Kullanımı", "Ortak sayfa yapısı, layout dosyası ve tekrar kullanılabilir partial view mantığını kavra.", Video.AspNetMvcFull),
                        D("Formdan Veri Alma", "Razor form yapısı, input name kullanımı ve post action ile veri alma sürecini öğren.", Video.AspNetMvcProject)
                    }
                ),
                new DemoBolumBilgisi(
                    "Veritabanı İşlemleri",
                    new List<DemoDersBilgisi>
                    {
                        D("Entity Framework Core Giriş", "DbContext, DbSet, migration ve entity ilişkilerinin temel mantığını öğren.", Video.AspNetMvcFull),
                        D("Migration Oluşturma", "Model değişikliklerini migration ile veritabanına yansıtmayı öğren.", Video.AspNetMvcFull),
                        D("CRUD İşlemleri", "Listeleme, ekleme, güncelleme ve silme işlemlerini MVC yapısında uygula.", Video.AspNetMvcProject),
                        D("Include ve İlişkili Veri Çekme", "Navigation property ve Include kullanarak ilişkili verileri güvenli şekilde çek.", Video.AspNetMvcProject),
                        D("ViewModel Kullanımı", "Form verilerini güvenli ve düzenli biçimde taşımak için ViewModel yapısını kullan.", Video.AspNetMvc)
                    }
                ),
                new DemoBolumBilgisi(
                    "Panel ve Yetkilendirme",
                    new List<DemoDersBilgisi>
                    {
                        D("Cookie Authentication", "Cookie tabanlı giriş yapısı ve kullanıcı claim bilgisinin nasıl taşındığını öğren.", Video.AspNetMvcFull),
                        D("Rol Bazlı Erişim", "Admin, eğitmen ve öğrenci gibi rollere göre erişim kontrolü yap.", Video.AspNetMvcFull),
                        D("Panel Yönlendirme Mantığı", "Giriş yapan kullanıcının rolüne göre doğru panele yönlendirilmesini sağla.", Video.AspNetMvcProject),
                        D("Yetkisiz Erişim Sayfası", "Kullanıcının yetkisi olmayan sayfalara erişimini engelleyip uygun ekrana yönlendir.", Video.AspNetMvcProject),
                        D("Final Mini Proje", "Öğrenilen konularla basit bir yönetim paneli akışı oluştur.", Video.AspNetMvcProject)
                    }
                )
            }
        ),

        new DemoKursBilgisi(
            EgitmenEposta: "egitmen1@coursvia.com",
            KursAdi: "JavaScript Temelleri",
            Aciklama: "JavaScript dilinin temel yapılarını, DOM işlemlerini ve etkileşimli web sayfası geliştirmeyi öğren.",
            KategoriAdi: "Yazılım Geliştirme",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            GunOnce: 34,
            KapakGorselUrl: "https://images.unsplash.com/photo-1627398242454-45a1465c2479?auto=format&fit=crop&w=1200&q=80",
            Bolumler: new List<DemoBolumBilgisi>
            {
                new DemoBolumBilgisi(
                    "JavaScript'e Giriş",
                    new List<DemoDersBilgisi>
                    {
                        D("Değişkenler ve Veri Tipleri", "let, const, string, number, boolean ve temel veri yapılarıyla çalış.", Video.JavaScriptFull),
                        D("Operatörler", "Aritmetik, karşılaştırma ve mantıksal operatörleri örneklerle öğren.", Video.JavaScriptFull),
                        D("Koşullar ve Döngüler", "if, switch, for ve while yapılarıyla karar ve tekrar akışlarını yönet.", Video.JavaScriptFull),
                        D("Fonksiyonlar", "Parametre, return değeri ve fonksiyon parçalama mantığını öğren.", Video.JavaScriptFull),
                        D("Dizi ve Obje Kullanımı", "Array ve object yapılarıyla düzenli veri saklama mantığını kavra.", Video.JavaScriptFull)
                    }
                ),
                new DemoBolumBilgisi(
                    "DOM İşlemleri",
                    new List<DemoDersBilgisi>
                    {
                        D("Element Seçme", "querySelector, getElementById ve class seçicileriyle sayfa elemanlarını yakala.", Video.JavaScriptDom),
                        D("İçerik Değiştirme", "innerText, innerHTML ve classList ile sayfa içeriğini dinamik olarak değiştir.", Video.JavaScriptDom),
                        D("Event Yönetimi", "click, submit, input gibi olayları dinleyerek etkileşimli sayfalar oluştur.", Video.JavaScriptDomFull),
                        D("Dinamik İçerik Üretme", "JavaScript ile HTML içeriği üretme ve sayfaya ekleme işlemlerini uygula.", Video.JavaScriptDomFull),
                        D("Form Kontrolü", "Form alanlarını kontrol ederek kullanıcıya anlık geri bildirim göster.", Video.JavaScriptDomFull)
                    }
                ),
                new DemoBolumBilgisi(
                    "Mini Uygulamalar",
                    new List<DemoDersBilgisi>
                    {
                        D("Sayaç Uygulaması", "Temel değişken ve event bilgisiyle sayaç uygulaması geliştir.", Video.JavaScriptFull),
                        D("Yapılacaklar Listesi", "Listeye eleman ekleme, silme ve tamamlandı işaretleme mantığını uygula.", Video.JavaScriptDomFull),
                        D("Form Doğrulama", "Kullanıcıdan gelen form verilerini kontrol et ve hata mesajı göster.", Video.JavaScriptDomFull),
                        D("Basit Filtreleme", "Liste elemanlarını arama kutusuna göre filtrelemeyi öğren.", Video.JavaScriptDom),
                        D("LocalStorage Kullanımı", "Tarayıcıda küçük verileri saklayarak uygulamayı kalıcı hale getir.", Video.JavaScriptFull)
                    }
                )
            }
        ),

        new DemoKursBilgisi(
            EgitmenEposta: "egitmen1@coursvia.com",
            KursAdi: "HTML, CSS ve Modern Web Tasarımı",
            Aciklama: "Temel HTML etiketleri, CSS düzen teknikleri ve modern responsive arayüz tasarımını öğren.",
            KategoriAdi: "Web Tasarım",
            DurumId: DemoSeedSabitleri.DurumOnayBekliyor,
            GunOnce: 12,
            KapakGorselUrl: "https://images.unsplash.com/photo-1517180102446-f3ece451e9d8?auto=format&fit=crop&w=1200&q=80",
            Bolumler: new List<DemoBolumBilgisi>
            {
                new DemoBolumBilgisi(
                    "HTML Temelleri",
                    new List<DemoDersBilgisi>
                    {
                        D("HTML Sayfa Yapısı", "HTML belgesinin temel yapısını ve sık kullanılan etiketleri öğren.", Video.HtmlCss),
                        D("Metin ve Liste Etiketleri", "Başlık, paragraf, liste ve bağlantı etiketleriyle içerik oluştur.", Video.HtmlCss),
                        D("Görsel ve Bağlantılar", "Image ve anchor etiketlerini doğru kullanarak içerik zenginleştir.", Video.HtmlCss),
                        D("Tablo Kullanımı", "Tablo, satır ve sütun yapısıyla düzenli veri göstermeyi öğren.", Video.HtmlCss),
                        D("Form Elemanları", "Input, select, textarea ve button elemanlarını doğru şekilde kullan.", Video.HtmlCss)
                    }
                ),
                new DemoBolumBilgisi(
                    "CSS ile Tasarım",
                    new List<DemoDersBilgisi>
                    {
                        D("CSS Bağlama Yöntemleri", "Inline, internal ve external CSS kullanım farklarını öğren.", Video.HtmlCss),
                        D("Box Model", "Margin, padding, border ve width ilişkisini örneklerle öğren.", Video.HtmlCss),
                        D("Flexbox Düzeni", "Satır ve sütun düzenlerini Flexbox ile profesyonel şekilde oluştur.", Video.ResponsiveWebsite),
                        D("Grid Temelleri", "Grid sistemiyle daha kontrollü sayfa yerleşimleri hazırla.", Video.ResponsiveWebsite),
                        D("Responsive Tasarım", "Farklı ekran boyutlarına uyumlu sayfa tasarımları hazırla.", Video.ResponsiveDesign)
                    }
                ),
                new DemoBolumBilgisi(
                    "Modern Arayüz Yaklaşımı",
                    new List<DemoDersBilgisi>
                    {
                        D("Renk ve Tipografi", "Renk paleti ve yazı tipi seçimiyle tutarlı tasarım oluştur.", Video.HtmlCss),
                        D("Kart Tasarımı", "Modern web sitelerinde kullanılan kart bileşenlerini tasarla.", Video.ResponsiveWebsite),
                        D("Navbar ve Footer", "Sayfa gezinme ve alt bilgi alanlarını profesyonel şekilde hazırla.", Video.ResponsiveWebsite),
                        D("Buton ve Form Tasarımı", "Kullanıcı dostu buton ve form bileşenleri oluştur.", Video.HtmlCss),
                        D("Mini Landing Page", "Öğrenilen tekniklerle tek sayfalık modern bir tanıtım sayfası hazırla.", Video.ResponsiveWebsite)
                    }
                )
            }
        ),

        new DemoKursBilgisi(
            EgitmenEposta: "egitmen2@coursvia.com",
            KursAdi: "Python ile Veri Analizi",
            Aciklama: "Python kullanarak veri okuma, temizleme, analiz etme ve raporlama becerileri kazan.",
            KategoriAdi: "Veri Bilimi",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            GunOnce: 31,
            KapakGorselUrl: "https://images.unsplash.com/photo-1526379095098-d400fd0bf935?auto=format&fit=crop&w=1200&q=80",
            Bolumler: new List<DemoBolumBilgisi>
            {
                new DemoBolumBilgisi(
                    "Python Temelleri",
                    new List<DemoDersBilgisi>
                    {
                        D("Python Söz Dizimi", "Değişken, veri tipi, çıktı alma ve temel komutlarla Python'a giriş yap.", Video.PythonDataAnalysis),
                        D("Koşullar ve Döngüler", "if, for ve while yapılarıyla akış kontrolü oluştur.", Video.PythonDataAnalysis),
                        D("Listeler ve Sözlükler", "Veri koleksiyonlarını kullanarak çoklu değerleri düzenli şekilde yönet.", Video.PythonDataAnalysis),
                        D("Fonksiyonlarla Kod Düzeni", "Tekrar eden işlemleri fonksiyonlara ayırarak okunabilir kod yaz.", Video.PythonDataAnalysis),
                        D("Dosya Okuma Mantığı", "Temel dosya okuma ve veri alma yaklaşımını öğren.", Video.PythonDataAnalysis)
                    }
                ),
                new DemoBolumBilgisi(
                    "Veri Hazırlama",
                    new List<DemoDersBilgisi>
                    {
                        D("Veri Okuma", "CSV ve tablo verilerini okuyarak analiz için hazırlık yap.", Video.PandasBasics),
                        D("Veri Temizleme", "Eksik, hatalı ve tekrar eden verileri temizleme yöntemlerini öğren.", Video.PandasFull),
                        D("Veri Tiplerini Düzenleme", "Tarih, sayı ve metin alanlarını analiz için uygun hale getir.", Video.PandasFull),
                        D("Filtreleme İşlemleri", "Belirli koşullara göre veri seçme ve ayırma mantığını uygula.", Video.PandasBasics),
                        D("Basit Raporlama", "Analiz sonuçlarını anlaşılır özetler ve tablolar halinde sun.", Video.PandasFull)
                    }
                ),
                new DemoBolumBilgisi(
                    "Analiz Mantığı",
                    new List<DemoDersBilgisi>
                    {
                        D("Gruplama ve Filtreleme", "Veri setinde anlamlı kırılımlar ve filtreler oluştur.", Video.PandasFull),
                        D("Ortalama ve Toplam Analizi", "Temel istatistiksel özetler üzerinden veriyi yorumla.", Video.PandasFull),
                        D("Kategorik Veri Analizi", "Kategori bazlı dağılım ve karşılaştırmalar yap.", Video.PandasBasics),
                        D("Sonuç Yorumlama", "Elde edilen sayıların iş kararlarına nasıl dönüştüğünü yorumla.", Video.PythonDataAnalysis),
                        D("Mini Analiz Projesi", "Küçük bir veri seti üzerinde baştan sona analiz sürecini uygula.", Video.PythonDataAnalysis)
                    }
                )
            }
        ),

        new DemoKursBilgisi(
            EgitmenEposta: "egitmen2@coursvia.com",
            KursAdi: "Yapay Zeka Temelleri",
            Aciklama: "Yapay zeka kavramlarını, makine öğrenmesi mantığını ve günlük kullanım alanlarını temel seviyede öğren.",
            KategoriAdi: "Yapay Zeka",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            GunOnce: 28,
            KapakGorselUrl: "https://images.unsplash.com/photo-1677442136019-21780ecad995?auto=format&fit=crop&w=1200&q=80",
            Bolumler: new List<DemoBolumBilgisi>
            {
                new DemoBolumBilgisi(
                    "Yapay Zekaya Giriş",
                    new List<DemoDersBilgisi>
                    {
                        D("Yapay Zeka Nedir?", "Yapay zekanın temel kavramlarını ve günlük hayattaki kullanım alanlarını öğren.", Video.AiBasics),
                        D("Makine Öğrenmesi Mantığı", "Veri, model, eğitim, tahmin ve doğruluk kavramlarını temel seviyede anla.", Video.AiFullCourse),
                        D("Derin Öğrenme Kavramı", "Yapay sinir ağları ve derin öğrenmenin temel yaklaşımını tanı.", Video.AiFullCourse),
                        D("Veri Kalitesi", "Model başarısında doğru ve temiz verinin neden önemli olduğunu öğren.", Video.GoogleAiBasics),
                        D("Model Başarısı Nasıl Ölçülür?", "Doğruluk, hata oranı ve değerlendirme kavramlarını temel seviyede kavra.", Video.AiFullCourse)
                    }
                ),
                new DemoBolumBilgisi(
                    "Uygulama Alanları",
                    new List<DemoDersBilgisi>
                    {
                        D("Eğitimde Yapay Zeka", "Kişiselleştirilmiş öğrenme ve öneri sistemlerinin eğitimdeki rolünü incele.", Video.GoogleAiBasics),
                        D("Sağlıkta Yapay Zeka", "Tanı, analiz ve karar destek alanlarında yapay zeka kullanımını tanı.", Video.AiBasics),
                        D("İş Süreçlerinde Yapay Zeka", "Otomasyon, raporlama ve karar destek sistemlerinde yapay zekanın yerini öğren.", Video.GoogleAiBasics),
                        D("Görüntü ve Ses İşleme", "Görsel tanıma ve ses işleme gibi alanlarda temel kullanım örneklerini incele.", Video.AiFullCourse),
                        D("Etik ve Güvenlik", "Yapay zeka kullanımında veri gizliliği, önyargı ve sorumluluk konularını kavra.", Video.GoogleAiBasics)
                    }
                ),
                new DemoBolumBilgisi(
                    "AI Okuryazarlığı",
                    new List<DemoDersBilgisi>
                    {
                        D("Prompt Mantığı", "Yapay zeka araçlarından doğru çıktı almak için istek yazma mantığını öğren.", Video.GoogleAiBasics),
                        D("Doğru Araç Seçimi", "Farklı AI araçlarını ihtiyaçlara göre değerlendirme yaklaşımını öğren.", Video.AiBasics),
                        D("Çıktı Kontrolü", "AI çıktılarının doğruluğunu kontrol etme alışkanlığı kazan.", Video.GoogleAiBasics),
                        D("Veri Gizliliği", "AI araçlarına bilgi verirken dikkat edilmesi gereken güvenlik noktalarını öğren.", Video.AiBasics)
                    }
                )
            }
        ),

        new DemoKursBilgisi(
            EgitmenEposta: "egitmen2@coursvia.com",
            KursAdi: "Temel Matematik ve Problem Çözme",
            Aciklama: "Günlük yaşam ve sınav hazırlığında ihtiyaç duyulan temel matematik ve problem çözme becerilerini geliştir.",
            KategoriAdi: "Matematik",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            GunOnce: 26,
            KapakGorselUrl: "https://images.unsplash.com/photo-1635070041078-e363dbe005cb?auto=format&fit=crop&w=1200&q=80",
            Bolumler: new List<DemoBolumBilgisi>
            {
                new DemoBolumBilgisi(
                    "Sayılar ve İşlemler",
                    new List<DemoDersBilgisi>
                    {
                        D("Temel İşlem Becerileri", "Dört işlem, işlem önceliği ve temel hesaplama alışkanlıklarını öğren.", Video.MathFoundations),
                        D("Kesirler ve Ondalık Sayılar", "Kesirli ve ondalık ifadelerle işlem yapma becerisini geliştir.", Video.MathFoundations),
                        D("Oran ve Orantı", "Oran, orantı ve günlük yaşam problemlerini çözmeyi öğren.", Video.MathWordProblem),
                        D("Yüzde Hesapları", "İndirim, artış ve oran değişimi gibi yüzde problemlerini çöz.", Video.MathWordProblem),
                        D("Sayı Problemleri", "Sayılar üzerinden kurulan temel problem tiplerini öğren.", Video.MathWordProblem)
                    }
                ),
                new DemoBolumBilgisi(
                    "Denklem ve Problem Çözme",
                    new List<DemoDersBilgisi>
                    {
                        D("Problem Okuma Teknikleri", "Soruda verilen ve istenen bilgiyi ayırma yöntemlerini öğren.", Video.MathWordProblem),
                        D("Denklem Kurma", "Sözel problemleri matematiksel denkleme dönüştürme becerisi kazan.", Video.MathEquations),
                        D("Birinci Dereceden Denklemler", "Temel denklem çözme adımlarını örneklerle uygula.", Video.MathEquations),
                        D("Yaş ve İşçi Problemleri", "Sık kullanılan problem türlerinde denklem kurma mantığını geliştir.", Video.MathWordProblem),
                        D("Sonuç Kontrolü", "Bulunan sonucun probleme uygun olup olmadığını kontrol et.", Video.MathWordProblem)
                    }
                ),
                new DemoBolumBilgisi(
                    "Grafik ve Mantık",
                    new List<DemoDersBilgisi>
                    {
                        D("Tablo Okuma", "Tablo içindeki bilgileri yorumlama ve sonuç çıkarma becerisi kazan.", Video.MathFoundations),
                        D("Grafik Yorumlama", "Sütun, çizgi ve daire grafiklerini temel seviyede yorumla.", Video.MathFoundations),
                        D("Mantık Soruları", "Örüntü, sıralama ve ilişki kurma sorularını çöz.", Video.MathWordProblem),
                        D("Karma Problemler", "Birden fazla konuyu içeren problemleri adım adım çöz.", Video.MathWordProblem)
                    }
                )
            }
        ),

        new DemoKursBilgisi(
            EgitmenEposta: "egitmen3@coursvia.com",
            KursAdi: "İngilizce Konuşma Pratiği",
            Aciklama: "Günlük konuşma kalıpları, telaffuz ve akıcı konuşma becerileriyle İngilizce pratiğini geliştir.",
            KategoriAdi: "Dil Eğitimi",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            GunOnce: 24,
            KapakGorselUrl: "https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?auto=format&fit=crop&w=1200&q=80",
            Bolumler: new List<DemoBolumBilgisi>
            {
                new DemoBolumBilgisi(
                    "Günlük Konuşma",
                    new List<DemoDersBilgisi>
                    {
                        D("Tanışma ve Selamlaşma", "Günlük hayatta sık kullanılan tanışma ve selamlaşma ifadelerini öğren.", Video.EnglishDaily),
                        D("Kendini Tanıtma", "Kişisel bilgilerini doğal ve akıcı şekilde anlatmayı öğren.", Video.EnglishSpeakingPractice),
                        D("Kısa Diyaloglar", "Basit günlük konuşma senaryolarıyla pratik yap.", Video.EnglishDaily),
                        D("Soru Sorma Kalıpları", "Günlük konuşmada ihtiyaç duyulan temel soru kalıplarını öğren.", Video.EnglishSpeakingPractice),
                        D("Restoran ve Alışveriş Diyalogları", "Sık karşılaşılan sosyal durumlarda kullanılacak ifadeleri öğren.", Video.EnglishDaily)
                    }
                ),
                new DemoBolumBilgisi(
                    "Akıcılık ve Telaffuz",
                    new List<DemoDersBilgisi>
                    {
                        D("Telaffuz İpuçları", "Sık yapılan telaffuz hatalarını ve doğru ses çıkarma yöntemlerini öğren.", Video.EnglishLongPractice),
                        D("Vurgu ve Tonlama", "Cümle içi vurgu ve doğal konuşma tonlamasını geliştir.", Video.EnglishLongPractice),
                        D("Konuşma Akışı", "Daha doğal ve kesintisiz konuşma için cümle bağlama tekniklerini uygula.", Video.EnglishSpeakingPractice),
                        D("Dinleme ve Yanıt Verme", "Karşı tarafı anlayıp uygun şekilde cevap verme becerisini geliştir.", Video.EnglishDaily),
                        D("Kısa Sunum Pratiği", "Basit konularda kısa ve anlaşılır İngilizce sunum yapmayı dene.", Video.EnglishSpeakingPractice)
                    }
                )
            }
        ),

        new DemoKursBilgisi(
            EgitmenEposta: "egitmen3@coursvia.com",
            KursAdi: "Etkili İletişim ve Sunum Teknikleri",
            Aciklama: "Topluluk önünde konuşma, sunum hazırlama ve etkili iletişim becerilerini geliştir.",
            KategoriAdi: "Kişisel Gelişim",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            GunOnce: 22,
            KapakGorselUrl: "https://images.unsplash.com/photo-1557804506-669a67965ba0?auto=format&fit=crop&w=1200&q=80",
            Bolumler: new List<DemoBolumBilgisi>
            {
                new DemoBolumBilgisi(
                    "İletişim Temelleri",
                    new List<DemoDersBilgisi>
                    {
                        D("Etkili Dinleme", "İletişimde aktif dinlemenin önemini ve temel uygulama yöntemlerini öğren.", Video.PublicSpeaking),
                        D("Beden Dili", "Duruş, göz teması, mimik ve jest kullanımını doğru şekilde yönet.", Video.PublicSpeaking),
                        D("Doğru Mesaj Verme", "Net, anlaşılır ve hedefe uygun mesaj oluşturma tekniklerini öğren.", Video.PresentationSkills),
                        D("Empati Kurma", "Karşı tarafın bakış açısını anlayarak daha sağlıklı iletişim kur.", Video.PublicSpeaking),
                        D("Geri Bildirim Verme", "Yapıcı ve anlaşılır geri bildirim verme yaklaşımını geliştir.", Video.PresentationSkills)
                    }
                ),
                new DemoBolumBilgisi(
                    "Sunum Hazırlığı",
                    new List<DemoDersBilgisi>
                    {
                        D("Sunum Planı", "Giriş, gelişme ve sonuç yapısına sahip etkili sunum planı hazırla.", Video.PresentationSkills),
                        D("Sahne Heyecanı", "Sunum kaygısını azaltmak için hazırlık ve nefes kontrolü tekniklerini öğren.", Video.PublicSpeaking),
                        D("Görsel Destek Kullanımı", "Slayt ve görsel materyalleri sunumun mesajını destekleyecek şekilde kullan.", Video.PresentationSkills),
                        D("Dinleyiciyle Etkileşim", "Soru sorma, göz teması ve örneklerle dinleyiciyi sunuma dahil et.", Video.SpeechPractice),
                        D("Sunum Sonu Kapanış", "Sunumu güçlü bir özet ve net kapanış cümlesiyle bitir.", Video.SpeechPractice)
                    }
                )
            }
        ),

        new DemoKursBilgisi(
            EgitmenEposta: "egitmen3@coursvia.com",
            KursAdi: "Akademik Yazma Becerileri",
            Aciklama: "Akademik metin yazma, kaynak kullanımı ve düzenli metin oluşturma becerilerini geliştir.",
            KategoriAdi: "Dil Eğitimi",
            DurumId: DemoSeedSabitleri.DurumKursDuzeltmeIsteniyor,
            GunOnce: 11,
            KapakGorselUrl: "https://images.unsplash.com/photo-1455390582262-044cdead277a?auto=format&fit=crop&w=1200&q=80",
            Bolumler: new List<DemoBolumBilgisi>
            {
                new DemoBolumBilgisi(
                    "Akademik Metin Yapısı",
                    new List<DemoDersBilgisi>
                    {
                        D("Akademik Yazıya Giriş", "Akademik yazının amacı, dili ve temel beklentilerini öğren.", Video.AcademicWriting),
                        D("Paragraf Oluşturma", "Giriş, açıklama ve sonuç cümlelerinden oluşan düzenli paragraf yapısı kur.", Video.AcademicWriting),
                        D("Akademik Dil Kullanımı", "Resmi, nesnel ve kaynak destekli anlatım biçimini öğren.", Video.AcademicPapers),
                        D("Tez Cümlesi Yazma", "Metnin ana fikrini net ve güçlü biçimde ifade etmeyi öğren.", Video.AcademicWritingFull),
                        D("Geçiş İfadeleri", "Paragraflar arasında anlamlı ve akıcı bağlantılar kur.", Video.AcademicWriting)
                    }
                ),
                new DemoBolumBilgisi(
                    "Kaynak Kullanımı",
                    new List<DemoDersBilgisi>
                    {
                        D("Kaynak Seçimi", "Güvenilir ve akademik kaynakları ayırt etme becerisi kazan.", Video.AcademicPapers),
                        D("Alıntı ve Atıf", "Kaynaklardan yararlanırken alıntı ve atıf mantığını doğru uygula.", Video.AcademicPapers),
                        D("Özetleme ve Parafraz", "Kaynak metni kendi cümlelerinle akademik üslupla aktarmayı öğren.", Video.AcademicWritingFull),
                        D("Kaynakça Hazırlama", "Kullanılan kaynakları düzenli kaynakça formatında listele.", Video.AcademicWritingFull),
                        D("Metin Kontrolü", "Yazım, anlatım ve tutarlılık açısından metni kontrol et.", Video.AcademicWriting)
                    }
                )
            }
        ),

        new DemoKursBilgisi(
            EgitmenEposta: "egitmen4@coursvia.com",
            KursAdi: "Temel Finans Okuryazarlığı",
            Aciklama: "Gelir-gider yönetimi, bütçe planlama, tasarruf ve temel yatırım kavramlarını öğren.",
            KategoriAdi: "Finans",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            GunOnce: 20,
            KapakGorselUrl: "https://images.unsplash.com/photo-1554224155-6726b3ff858f?auto=format&fit=crop&w=1200&q=80",
            Bolumler: new List<DemoBolumBilgisi>
            {
                new DemoBolumBilgisi(
                    "Finansa Giriş",
                    new List<DemoDersBilgisi>
                    {
                        D("Bütçe Nedir?", "Gelir ve giderleri takip ederek kişisel bütçe oluşturma mantığını öğren.", Video.Budgeting),
                        D("Gelir ve Gider Takibi", "Aylık gelir ve giderleri sınıflandırarak finansal tablo oluştur.", Video.Budgeting),
                        D("Tasarruf Alışkanlığı", "Düzenli birikim ve harcama kontrolü için uygulanabilir yöntemleri öğren.", Video.FinanceBasics),
                        D("Borç Yönetimi", "Borçlanma, ödeme planı ve finansal risk yönetimi konularını tanı.", Video.PersonalFinance),
                        D("Acil Durum Fonu", "Beklenmeyen giderler için güvenli bir finansal hazırlık yapmayı öğren.", Video.FinanceBasics)
                    }
                ),
                new DemoBolumBilgisi(
                    "Yatırım Temelleri",
                    new List<DemoDersBilgisi>
                    {
                        D("Risk ve Getiri", "Yatırım kararlarında risk ve getiri dengesini temel seviyede öğren.", Video.FinanceBasics),
                        D("Temel Finansal Terimler", "Faiz, enflasyon, vade, birikim ve getiri gibi kavramları öğren.", Video.PersonalFinance),
                        D("Yatırım Araçlarını Tanıma", "Farklı yatırım araçlarının temel özelliklerini karşılaştır.", Video.FinanceBasics),
                        D("Uzun Vadeli Planlama", "Kısa vadeli kararlar ile uzun vadeli finansal hedefleri ayır.", Video.PersonalFinance),
                        D("Finansal Hedef Belirleme", "Gerçekçi ve ölçülebilir finansal hedefler oluştur.", Video.Budgeting)
                    }
                )
            }
        ),

        new DemoKursBilgisi(
            EgitmenEposta: "egitmen4@coursvia.com",
            KursAdi: "Dijital Pazarlamaya Giriş",
            Aciklama: "Marka bilinirliği, hedef kitle, reklam kanalları ve dijital pazarlama stratejilerini öğren.",
            KategoriAdi: "Pazarlama",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            GunOnce: 18,
            KapakGorselUrl: "https://images.unsplash.com/photo-1460925895917-afdab827c52f?auto=format&fit=crop&w=1200&q=80",
            Bolumler: new List<DemoBolumBilgisi>
            {
                new DemoBolumBilgisi(
                    "Pazarlama Temelleri",
                    new List<DemoDersBilgisi>
                    {
                        D("Hedef Kitle Belirleme", "Doğru kitleye doğru mesajı ulaştırmak için hedef kitle analizi yap.", Video.DigitalMarketing),
                        D("Marka Konumlandırma", "Markayı rakiplerden ayıran güçlü değer önerisini belirle.", Video.DigitalMarketing),
                        D("Dijital Kanallar", "Web, sosyal medya, arama motoru ve e-posta kanallarını tanı.", Video.DigitalMarketingFull),
                        D("Müşteri Yolculuğu", "Kullanıcının markayla ilk temasından satın alma kararına kadar olan süreci öğren.", Video.DigitalMarketingCourse),
                        D("Pazarlama Hunisi", "Farkındalık, ilgi, karar ve aksiyon aşamalarını temel seviyede kavra.", Video.DigitalMarketingFull)
                    }
                ),
                new DemoBolumBilgisi(
                    "Kampanya Planlama",
                    new List<DemoDersBilgisi>
                    {
                        D("İçerik Planı", "Haftalık ve aylık içerik planı oluşturarak pazarlama sürecini düzenle.", Video.DigitalMarketing),
                        D("Reklam Mesajı Hazırlama", "Kısa, net ve hedef kitleye uygun reklam mesajları oluştur.", Video.DigitalMarketingFull),
                        D("Sosyal Medya Yayın Takvimi", "Platformlara göre içerik sıklığı ve yayın planı hazırla.", Video.DigitalMarketingCourse),
                        D("Performans Ölçümü", "Erişim, tıklama ve dönüşüm gibi temel metriklerle başarıyı değerlendir.", Video.DigitalMarketingFull),
                        D("Kampanya İyileştirme", "Veriye göre reklam ve içerik performansını geliştirme yollarını öğren.", Video.DigitalMarketingCourse)
                    }
                )
            }
        ),

        new DemoKursBilgisi(
            EgitmenEposta: "egitmen4@coursvia.com",
            KursAdi: "Girişimcilik ve İş Modeli Geliştirme",
            Aciklama: "Bir iş fikrini analiz etmeyi, iş modeli oluşturmayı ve temel girişimcilik adımlarını öğren.",
            KategoriAdi: "Girişimcilik",
            DurumId: DemoSeedSabitleri.DurumOnayBekliyor,
            GunOnce: 9,
            KapakGorselUrl: "https://images.unsplash.com/photo-1556761175-b413da4baf72?auto=format&fit=crop&w=1200&q=80",
            Bolumler: new List<DemoBolumBilgisi>
            {
                new DemoBolumBilgisi(
                    "Girişimcilik Temelleri",
                    new List<DemoDersBilgisi>
                    {
                        D("İş Fikri Bulma", "Problem, çözüm ve hedef kitle ilişkisiyle iş fikri geliştirme mantığını öğren.", Video.BusinessModelGuide),
                        D("Problem Analizi", "Çözülmek istenen problemi gerçek ihtiyaçlar üzerinden değerlendirmeyi öğren.", Video.BusinessModelCanvas),
                        D("Değer Önerisi", "Ürün veya hizmetin kullanıcıya sunduğu temel faydayı netleştir.", Video.BusinessModelCanvasShort),
                        D("Pazar Araştırması", "Rakip, hedef kitle ve pazar ihtiyacını analiz etme yöntemlerini öğren.", Video.BusinessModelCanvas),
                        D("İlk Müşteri Profili", "Ürün veya hizmetten en erken fayda sağlayacak müşteri tipini tanımla.", Video.BusinessModelGuide)
                    }
                ),
                new DemoBolumBilgisi(
                    "İş Modeli",
                    new List<DemoDersBilgisi>
                    {
                        D("Gelir Modeli", "Ürün veya hizmetten gelir elde etme yollarını karşılaştır.", Video.BusinessModelCanvas),
                        D("Müşteri Segmentleri", "Farklı kullanıcı gruplarını ve ihtiyaçlarını belirle.", Video.BusinessModelCanvas),
                        D("Maliyet Kalemleri", "Bir girişimin temel gider kalemlerini ve kaynak ihtiyaçlarını tanı.", Video.BusinessModelCanvas),
                        D("Dağıtım Kanalları", "Ürün veya hizmetin müşteriye nasıl ulaştırılacağını planla.", Video.BusinessModelCanvasShort),
                        D("Mini İş Modeli Taslağı", "Öğrenilen konularla basit bir iş modeli taslağı hazırla.", Video.BusinessModelGuide)
                    }
                )
            }
        ),

        new DemoKursBilgisi(
            EgitmenEposta: "egitmen5@coursvia.com",
            KursAdi: "Canva ile Görsel Tasarım",
            Aciklama: "Canva kullanarak sosyal medya görselleri, sunumlar ve temel tasarım çalışmaları hazırlamayı öğren.",
            KategoriAdi: "Tasarım",
            DurumId: DemoSeedSabitleri.DurumYayinda,
            GunOnce: 16,
            KapakGorselUrl: "https://images.unsplash.com/photo-1561070791-2526d30994b5?auto=format&fit=crop&w=1200&q=80",
            Bolumler: new List<DemoBolumBilgisi>
            {
                new DemoBolumBilgisi(
                    "Tasarım Temelleri",
                    new List<DemoDersBilgisi>
                    {
                        D("Renk ve Tipografi", "Renk uyumu, kontrast ve yazı tipi seçimiyle daha temiz tasarımlar hazırla.", Video.CanvaDesign),
                        D("Düzen ve Hiyerarşi", "Görselde dikkat yönlendirme ve düzenli kompozisyon oluşturma mantığını öğren.", Video.CanvaDesign),
                        D("Boşluk Kullanımı", "Tasarımda nefes alanı bırakarak daha okunabilir görseller oluştur.", Video.CanvaBeginner),
                        D("Şablon Kullanımı", "Canva şablonlarını ihtiyaca göre düzenleyip profesyonel çıktılar üret.", Video.CanvaBeginner),
                        D("Marka Uyumu", "Renk, logo ve yazı tipi kullanımıyla tutarlı marka görünümü oluştur.", Video.CanvaFull)
                    }
                ),
                new DemoBolumBilgisi(
                    "Uygulamalı Tasarımlar",
                    new List<DemoDersBilgisi>
                    {
                        D("Sosyal Medya Görseli", "Gönderi ve hikaye formatlarına uygun sosyal medya görselleri hazırla.", Video.CanvaBeginner),
                        D("Sunum Tasarımı", "Başlık, görsel, boşluk ve vurgu kullanarak etkili slaytlar oluştur.", Video.CanvaFull),
                        D("Afiş Tasarımı", "Etkinlik ve duyuru amaçlı afiş tasarımı hazırlama sürecini öğren.", Video.CanvaDesign),
                        D("Mini Marka Kiti", "Basit renk paleti ve görsel şablonlardan oluşan marka kiti hazırla.", Video.CanvaFull),
                        D("Tasarım Kontrol Listesi", "Yayınlamadan önce görseli kontrol etmek için temel kontrol listesi oluştur.", Video.CanvaBeginner)
                    }
                )
            }
        ),

        new DemoKursBilgisi(
            EgitmenEposta: "egitmen5@coursvia.com",
            KursAdi: "Zaman Yönetimi ve Verimli Çalışma",
            Aciklama: "Zaman planlama, önceliklendirme ve odaklanma yöntemleriyle daha verimli çalışma alışkanlıkları kazan.",
            KategoriAdi: "Kişisel Gelişim",
            DurumId: DemoSeedSabitleri.DurumTaslak,
            GunOnce: 7,
            KapakGorselUrl: "https://images.unsplash.com/photo-1506784983877-45594efa4cbe?auto=format&fit=crop&w=1200&q=80",
            Bolumler: new List<DemoBolumBilgisi>
            {
                new DemoBolumBilgisi(
                    "Zaman Yönetimi",
                    new List<DemoDersBilgisi>
                    {
                        D("Öncelik Belirleme", "Önemli ve acil işleri ayırarak doğru önceliklendirme yap.", Video.TimeManagement),
                        D("Planlama Alışkanlığı", "Günlük ve haftalık plan oluşturma alışkanlığını geliştir.", Video.TimeManagement),
                        D("Hedefleri Parçalara Ayırma", "Büyük hedefleri uygulanabilir küçük adımlara bölmeyi öğren.", Video.TimeManagementTips),
                        D("Zaman Tuzakları", "Günü bölen dikkat dağıtıcı alışkanlıkları fark et.", Video.Productivity),
                        D("Takvim Kullanımı", "Görevleri takvim ve yapılacaklar listesiyle düzenleme yaklaşımını öğren.", Video.TimeManagementTips)
                    }
                ),
                new DemoBolumBilgisi(
                    "Verimli Çalışma",
                    new List<DemoDersBilgisi>
                    {
                        D("Odaklanma Teknikleri", "Dikkat dağınıklığını azaltmak için uygulanabilir yöntemler öğren.", Video.Productivity),
                        D("Erteleme ile Mücadele", "Başlama direncini azaltan küçük adımlı çalışma yaklaşımını öğren.", Video.Productivity),
                        D("Çalışma Ortamı Düzeni", "Verimli çalışma için fiziksel ve dijital ortamı düzenle.", Video.TimeManagementTips),
                        D("Enerji Yönetimi", "Günün farklı zamanlarında enerji seviyesine göre iş planlama mantığını kavra.", Video.TimeManagement),
                        D("Kişisel Verimlilik Planı", "Kendi çalışma alışkanlıklarına uygun basit bir verimlilik planı oluştur.", Video.TimeManagementTips)
                    }
                )
            }
        )
    };

    private static DemoDersBilgisi D(
        string dersAdi,
        string aciklama,
        string videoUrl)
    {
        return new DemoDersBilgisi(
            DersAdi: dersAdi,
            Aciklama: aciklama,
            VideoUrl: videoUrl
        );
    }

    public sealed record DemoKursBilgisi(
        string EgitmenEposta,
        string KursAdi,
        string Aciklama,
        string KategoriAdi,
        int DurumId,
        int GunOnce,
        string KapakGorselUrl,
        List<DemoBolumBilgisi> Bolumler
    );

    public sealed record DemoBolumBilgisi(
        string BolumAdi,
        List<DemoDersBilgisi> Dersler
    );

    public sealed record DemoDersBilgisi(
        string DersAdi,
        string Aciklama,
        string VideoUrl
    );
}