namespace CoursVia.Data.Seed;

public static class DemoSinavSabitleri
{
    public static readonly IReadOnlyList<DemoSinavBilgisi> Sinavlar =
    [
        new(
            KursAdi: "ASP.NET Core MVC ile Web Geliştirme",
            SinavAdi: "ASP.NET Core MVC Final Sınavı",
            Aciklama: "MVC mimarisi, Entity Framework Core, ViewModel, authentication ve rol bazlı yetkilendirme konularını ölçen kapsamlı final sınavı.",
            GecmeNotu: 70,
            SureDakika: 45,
            SoruSayisi: 20,
            GunOnce: 25,
            Sorular:
            [
                S("ASP.NET Core MVC yapısında Controller temel olarak hangi görevi üstlenir?", "ASP.NET Core MVC Nedir?", "Kullanıcı isteğini karşılar, gerekli işlemi yapar ve uygun sonucu döndürür.", "Veritabanı tablolarını fiziksel olarak oluşturur.", "Sadece CSS dosyalarını yönetir.", "Uygulamanın NuGet paketlerini otomatik günceller."),
                S("MVC mimarisinde View katmanının temel amacı nedir?", "ASP.NET Core MVC Nedir?", "Kullanıcıya gösterilecek arayüzü üretmek.", "Veritabanı bağlantı cümlesini saklamak.", "Şifreleri hashlemek.", "Migration dosyalarını silmek."),
                S("MVC yapısında Model genellikle neyi temsil eder?", "ASP.NET Core MVC Nedir?", "Uygulamanın veri yapısını ve iş nesnelerini.", "Tarayıcı geçmişini.", "Sadece JavaScript eventlerini.", "CSS değişkenlerini."),
                S("Controllers klasörü genellikle hangi dosyaları içerir?", "Proje Yapısını Tanıma", "Action metodlarını içeren controller sınıflarını.", "Sadece profil fotoğraflarını.", "Sadece migration geçmişini.", "Sadece CSS çıktılarını."),
                S("wwwroot klasörünün temel amacı nedir?", "Proje Yapısını Tanıma", "Statik dosyaları barındırmak.", "Entity sınıflarını saklamak.", "Veritabanı ilişkilerini kurmak.", "Controller actionlarını çalıştırmak."),
                S("Routing mekanizması neyi belirler?", "Routing ve Action Mantığı", "URL isteğinin hangi controller/action tarafından karşılanacağını.", "Şifre hash algoritmasını.", "Veritabanı dosya boyutunu.", "Mail sunucusunun hızını."),
                S("Bir action metodunun IActionResult dönmesi ne sağlar?", "Routing ve Action Mantığı", "View, Redirect, Json gibi farklı sonuçların döndürülebilmesini.", "Sadece string döndürmeyi zorunlu kılar.", "Migration oluşturur.", "CSS sınıfı üretir."),
                S("Layout dosyası hangi amaçla kullanılır?", "Layout ve Partial View Kullanımı", "Sayfalar arasında ortak tasarım iskeletini paylaşmak için.", "Veritabanını yedeklemek için.", "Sadece şifre yenilemek için.", "Ders videosunu oynatmak için."),
                S("Partial View genellikle ne için tercih edilir?", "Layout ve Partial View Kullanımı", "Tekrar kullanılabilir arayüz parçalarını ayrı dosyada yönetmek için.", "Veritabanı bağlantısını hızlandırmak için.", "JWT token üretmek için.", "Kullanıcı şifresini değiştirmek için."),
                S("Formdan POST ile veri alırken input name değerleri neden önemlidir?", "Formdan Veri Alma", "Model binding işleminin doğru alanlarla eşleşebilmesi için.", "CSS renklerini belirlemek için.", "Tarayıcı geçmişini silmek için.", "NuGet paketlerini yüklemek için."),
                S("Entity Framework Core'da DbContext ne için kullanılır?", "Entity Framework Core Giriş", "Veritabanı ile uygulama arasındaki çalışma birimini temsil etmek için.", "Sadece HTML formlarını doğrulamak için.", "CSS sınıflarını otomatik üretmek için.", "Tarayıcı çerezlerini temizlemek için."),
                S("DbSet neyi temsil eder?", "Entity Framework Core Giriş", "Bir entity için veritabanı tablosu gibi sorgulanabilir koleksiyonu.", "Sadece route listesini.", "Sadece view dosyasını.", "Sadece cookie ayarını."),
                S("Migration oluşturmanın temel amacı nedir?", "Migration Oluşturma", "Model değişikliklerini veritabanı şemasına kontrollü şekilde yansıtmak.", "Kullanıcı girişini otomatik yapmak.", "Resim dosyalarını sıkıştırmak.", "Razor View dosyalarını silmek."),
                S("CRUD işlemleri hangi temel işlemleri kapsar?", "CRUD İşlemleri", "Create, Read, Update, Delete.", "Copy, Reset, Undo, Design.", "Cookie, Route, Upload, Download.", "Class, Razor, User, Domain."),
                S("ViewModel kullanmanın en önemli faydalarından biri nedir?", "ViewModel Kullanımı", "View ile Controller arasında sadece ihtiyaç duyulan veriyi taşımak.", "Veritabanını tamamen silmek.", "Tüm controllerları otomatik oluşturmak.", "CSS dosyalarını JavaScript'e çevirmek."),
                S("Include metodu genellikle hangi durumda kullanılır?", "Include ve İlişkili Veri Çekme", "İlişkili navigation property verilerini sorguya dahil etmek için.", "Şifreyi düz metin olarak saklamak için.", "Kullanıcıyı sistemden çıkarmak için.", "Dosya uzantısını değiştirmek için."),
                S("Cookie Authentication yapısında claim bilgileri ne için kullanılır?", "Cookie Authentication", "Kullanıcıya ait kimlik ve rol gibi bilgileri taşımak için.", "Veritabanı tablolarını çoğaltmak için.", "Video oynatıcının hızını ayarlamak için.", "Migration geçmişini temizlemek için."),
                S("Rol bazlı erişim kontrolünün amacı nedir?", "Rol Bazlı Erişim", "Kullanıcının rolüne göre yetkili olduğu alanlara erişmesini sağlamak.", "Her kullanıcının her sayfaya erişmesini sağlamak.", "Sadece admin girişini kapatmak.", "Veritabanındaki tüm rolleri silmek."),
                S("Panel yönlendirme mantığında aktif rol bilgisi neden önemlidir?", "Panel Yönlendirme Mantığı", "Kullanıcıyı doğru panele göndermek için.", "CSS dosyasını küçültmek için.", "Veritabanı portunu değiştirmek için.", "Resim dosyası oluşturmak için."),
                S("Yetkisiz erişim sayfası hangi durumda gösterilir?", "Yetkisiz Erişim Sayfası", "Kullanıcının yetkisi olmayan bir sayfaya erişmeye çalışması durumunda.", "Kullanıcı doğru şifre girdiğinde.", "Sunucu başarılı çalıştığında.", "Migration başarıyla tamamlandığında.")
            ]
        ),

        new(
            KursAdi: "JavaScript Temelleri",
            SinavAdi: "JavaScript Temelleri Final Sınavı",
            Aciklama: "Değişkenler, operatörler, fonksiyonlar, DOM işlemleri, event yönetimi ve mini uygulama mantığını ölçen kapsamlı sınav.",
            GecmeNotu: 70,
            SureDakika: 40,
            SoruSayisi: 20,
            GunOnce: 23,
            Sorular:
            [
                S("JavaScript'te const ile tanımlanan bir değişken için aşağıdakilerden hangisi doğrudur?", "Değişkenler ve Veri Tipleri", "Değeri sonradan aynı scope içinde yeniden atanamaz.", "Mutlaka sayı olmak zorundadır.", "Sadece HTML içinde kullanılabilir.", "Tarayıcıda çalışmaz."),
                S("let ile var arasındaki önemli farklardan biri nedir?", "Değişkenler ve Veri Tipleri", "let blok kapsamına daha uygun davranır.", "let sadece CSS içinde çalışır.", "var sadece sayı tutar.", "İkisi de değişken tanımlamaz."),
                S("boolean veri tipi hangi değerleri ifade eder?", "Değişkenler ve Veri Tipleri", "true veya false.", "Sadece metin.", "Sadece dizi.", "Sadece tarih."),
                S("=== operatörü neyi kontrol eder?", "Operatörler", "Değer ve tip eşitliğini.", "Sadece değer eşitliğini.", "Sadece değişken adını.", "Sadece dizinin uzunluğunu."),
                S("&& operatörü ne zaman true döner?", "Operatörler", "İki koşul da true olduğunda.", "İki koşuldan biri false olduğunda.", "Her zaman true döner.", "Sadece stringlerde çalışır."),
                S("if yapısı hangi amaçla kullanılır?", "Koşullar ve Döngüler", "Belirli bir koşula göre karar vermek için.", "CSS dosyası bağlamak için.", "Veritabanı oluşturmak için.", "HTML etiketlerini silmek için."),
                S("for döngüsü genellikle ne için kullanılır?", "Koşullar ve Döngüler", "Belirli sayıda tekrar eden işlemleri yapmak için.", "Sadece değişken tanımlamak için.", "Tarayıcıyı kapatmak için.", "Formu otomatik göndermek için."),
                S("Bir fonksiyonun return ifadesi ne işe yarar?", "Fonksiyonlar", "Fonksiyonun dışarı değer döndürmesini sağlar.", "CSS dosyasını yeniler.", "HTML sayfasını otomatik kapatır.", "Tarayıcı geçmişini temizler."),
                S("Fonksiyonlara parametre verilmesinin amacı nedir?", "Fonksiyonlar", "Fonksiyonun farklı verilerle çalışabilmesini sağlamak.", "Fonksiyonu tamamen silmek.", "Tarayıcıyı yenilemek.", "CSS rengini değiştirmek."),
                S("Array yapısı hangi durumda kullanışlıdır?", "Dizi ve Obje Kullanımı", "Birden fazla değeri sıralı şekilde saklamak için.", "Sadece tek bir boolean değer saklamak için.", "HTML dosyasını derlemek için.", "Şifre hashlemek için."),
                S("Object yapısı genellikle ne için kullanılır?", "Dizi ve Obje Kullanımı", "Bir varlığa ait ilişkili özellikleri saklamak için.", "Sadece sayfa yenilemek için.", "CSS dosyasını silmek için.", "Sunucuya migration atmak için."),
                S("querySelector hangi amaçla kullanılır?", "Element Seçme", "CSS seçicisiyle sayfadan eleman seçmek için.", "Veritabanından kayıt silmek için.", "Sunucuya migration atmak için.", "Dosya yüklemek için."),
                S("getElementById hangi durumda kullanılır?", "Element Seçme", "Belirli id değerine sahip HTML elemanını seçmek için.", "Veritabanı bağlantısı açmak için.", "Cookie oluşturmak için.", "Dizi sıralamak için."),
                S("innerText genellikle neyi değiştirir?", "İçerik Değiştirme", "Bir elemanın metin içeriğini.", "Veritabanı şemasını.", "Tarayıcı portunu.", "Sunucu işletim sistemini."),
                S("classList.add() genellikle ne için kullanılır?", "İçerik Değiştirme", "Bir HTML elemanına CSS sınıfı eklemek için.", "Diziye sayı eklemek için.", "Veritabanına kullanıcı eklemek için.", "Sayfayı tamamen silmek için."),
                S("addEventListener metodunun görevi nedir?", "Event Yönetimi", "Bir olaya karşı çalışacak fonksiyon tanımlamak.", "Bir array'i alfabetik sıralamak.", "Veritabanı bağlantısını açmak.", "Cookie oluşturmak."),
                S("submit event'i genellikle nerede kullanılır?", "Event Yönetimi", "Form gönderimini yakalamak için.", "Video dosyası kesmek için.", "SQL Server kurmak için.", "CSS rengi belirlemek için."),
                S("Dinamik içerik üretirken createElement ne işe yarar?", "Dinamik İçerik Üretme", "JavaScript ile yeni HTML elemanı oluşturmak için.", "Veritabanı tablosu oluşturmak için.", "Sunucu portunu değiştirmek için.", "CSS dosyasını silmek için."),
                S("Form doğrulama yapılmasının amacı nedir?", "Form Doğrulama", "Kullanıcıdan gelen verinin uygun olup olmadığını kontrol etmek.", "CSS dosyasını küçültmek.", "Sunucu saatini değiştirmek.", "NuGet paketini güncellemek."),
                S("LocalStorage ne için kullanılır?", "LocalStorage Kullanımı", "Tarayıcıda küçük verileri kalıcı olarak saklamak için.", "SQL Server yedeği almak için.", "Controller üretmek için.", "Sunucu tarafında mail göndermek için.")
            ]
        ),

        new(
            KursAdi: "HTML, CSS ve Modern Web Tasarımı",
            SinavAdi: "HTML, CSS ve Modern Web Tasarımı Final Sınavı",
            Aciklama: "HTML temel yapısı, form elemanları, CSS düzen teknikleri, responsive tasarım ve modern arayüz yaklaşımını ölçen sınav.",
            GecmeNotu: 70,
            SureDakika: 30,
            SoruSayisi: 10,
            GunOnce: 10,
            Sorular:
            [
                S("HTML belgesinde temel sayfa iskeleti hangi bölümlerden oluşur?", "HTML Sayfa Yapısı", "html, head ve body bölümlerinden.", "Sadece footer bölümünden.", "Sadece script etiketinden.", "Sadece table etiketinden."),
                S("Başlık etiketi olan h1 genellikle ne için kullanılır?", "Metin ve Liste Etiketleri", "Sayfanın en önemli başlığını göstermek için.", "Veritabanı bağlantısı kurmak için.", "CSS dosyasını silmek için.", "Form göndermek için."),
                S("img etiketi hangi amaçla kullanılır?", "Görsel ve Bağlantılar", "Sayfaya görsel eklemek için.", "Kullanıcı şifresini hashlemek için.", "Tabloyu silmek için.", "Sunucu başlatmak için."),
                S("a etiketi genellikle ne işe yarar?", "Görsel ve Bağlantılar", "Bağlantı oluşturmak için.", "Veritabanı oluşturmak için.", "CSS sınıfı silmek için.", "Tarayıcıyı kapatmak için."),
                S("HTML form elemanları ne için kullanılır?", "Form Elemanları", "Kullanıcıdan veri almak için.", "Sunucuyu kapatmak için.", "Resim kırpmak için.", "Migration üretmek için."),
                S("CSS Box Model hangi kavramları içerir?", "Box Model", "Margin, padding, border ve content alanlarını.", "Sadece veritabanı tablolarını.", "Sadece JavaScript fonksiyonlarını.", "Sadece mail ayarlarını."),
                S("Flexbox hangi amaçla kullanılır?", "Flexbox Düzeni", "Elemanları esnek şekilde hizalamak ve yerleştirmek için.", "Şifre sıfırlamak için.", "Migration silmek için.", "Video dosyası oynatmak için."),
                S("Grid sistemi hangi durumda avantaj sağlar?", "Grid Temelleri", "İki boyutlu sayfa düzenleri oluştururken.", "Sadece metin kopyalarken.", "Cookie temizlerken.", "Mail gönderirken."),
                S("Responsive tasarım neyi amaçlar?", "Responsive Tasarım", "Sayfanın farklı ekran boyutlarına uyum sağlamasını.", "Sadece masaüstünde çalışmasını.", "Tüm yazıları gizlemeyi.", "Veritabanını küçültmeyi."),
                S("Modern arayüzde renk ve tipografi neden önemlidir?", "Renk ve Tipografi", "Okunabilirlik ve görsel tutarlılık sağlamak için.", "Sunucu portunu değiştirmek için.", "Kullanıcı rolünü silmek için.", "Controller adını değiştirmek için.")
            ]
        ),

        new(
            KursAdi: "Python ile Veri Analizi",
            SinavAdi: "Python ile Veri Analizi Final Sınavı",
            Aciklama: "Python temelleri, veri okuma, veri temizleme, filtreleme, gruplama ve analiz yorumlama becerilerini ölçen kapsamlı sınav.",
            GecmeNotu: 70,
            SureDakika: 45,
            SoruSayisi: 20,
            GunOnce: 21,
            Sorular:
            [
                S("Python'da değişken tanımlarken hangisi doğrudur?", "Python Söz Dizimi", "Değişken adı yazılıp değer atanabilir.", "Her değişken mutlaka int olmak zorundadır.", "Değişkenler sadece HTML içinde tanımlanır.", "Python değişken desteklemez."),
                S("Python'da print fonksiyonu ne işe yarar?", "Python Söz Dizimi", "Ekrana çıktı vermek için kullanılır.", "Dosya silmek için kullanılır.", "Veritabanı migration almak için kullanılır.", "CSS yazmak için kullanılır."),
                S("if yapısı Python'da ne için kullanılır?", "Koşullar ve Döngüler", "Koşula göre farklı kod çalıştırmak için.", "Sadece liste oluşturmak için.", "Sadece dosya okumak için.", "Sadece yorum satırı yazmak için."),
                S("for döngüsü hangi durumda kullanılır?", "Koşullar ve Döngüler", "Bir koleksiyon üzerinde tekrar işlem yapmak için.", "Sadece tek satır yazmak için.", "CSS sınıfı oluşturmak için.", "Veritabanı bağlantısı kapatmak için."),
                S("Python'da liste yapısı hangi amaçla kullanılır?", "Listeler ve Sözlükler", "Birden fazla değeri sıralı biçimde saklamak için.", "Sadece tek karakter saklamak için.", "HTML sayfası üretmek için.", "Veritabanı migration almak için."),
                S("Sözlük yapısında veriler hangi mantıkla tutulur?", "Listeler ve Sözlükler", "Anahtar-değer çiftleriyle.", "Sadece index numarasıyla.", "Sadece tarih formatında.", "Sadece boolean olarak."),
                S("Fonksiyon kullanmanın temel faydası nedir?", "Fonksiyonlarla Kod Düzeni", "Tekrar eden işlemleri düzenli ve yeniden kullanılabilir hale getirmek.", "Dosyaları otomatik silmek.", "CSS dosyası oluşturmak.", "Veritabanı portunu değiştirmek."),
                S("Fonksiyonda parametre ne işe yarar?", "Fonksiyonlarla Kod Düzeni", "Fonksiyonun dışarıdan veri almasını sağlar.", "Fonksiyonu tamamen kapatır.", "Sadece yorum satırı üretir.", "Python'u HTML'e çevirir."),
                S("Dosya okuma işlemi neden kullanılır?", "Dosya Okuma Mantığı", "Dış kaynaktaki veriyi programa almak için.", "Ekran parlaklığını değiştirmek için.", "Sunucu IP adresini silmek için.", "CSS dosyasını renklendirmek için."),
                S("CSV dosyası genellikle ne tür veri içerir?", "Veri Okuma", "Satır ve sütun mantığında ayrılmış tablo verisi.", "Sadece video dosyası.", "Sadece şifre hashleri.", "Sadece görsel piksel verisi."),
                S("Veri temizleme sürecinde aşağıdakilerden hangisi yapılabilir?", "Veri Temizleme", "Eksik veya hatalı değerleri düzenlemek.", "Tüm verileri rastgele silmek.", "HTML etiketlerini renklendirmek.", "Sunucu IP adresini değiştirmek."),
                S("Tekrar eden kayıtlar analizde neden sorun olabilir?", "Veri Temizleme", "Sonuçları olduğundan farklı gösterebilir.", "CSS dosyasını büyütür.", "Video süresini artırır.", "Sunucu fanını yavaşlatır."),
                S("Veri tiplerini düzenlemek neden önemlidir?", "Veri Tiplerini Düzenleme", "Analiz işlemlerinin doğru yapılabilmesi için.", "Tablo adını süslemek için.", "Dosya rengini değiştirmek için.", "Kullanıcı rolünü gizlemek için."),
                S("Filtreleme işlemi ne işe yarar?", "Filtreleme İşlemleri", "Belirli koşullara uyan kayıtları seçmek.", "Dosyanın uzantısını değiştirmek.", "Kullanıcı rolünü silmek.", "CSS sınıfı eklemek."),
                S("Basit raporlama hangi amaca hizmet eder?", "Basit Raporlama", "Analiz sonuçlarını anlaşılır şekilde sunmak.", "Sunucuyu kapatmak.", "Ders videosunu silmek.", "Veritabanını bozmak."),
                S("Gruplama işlemi hangi analiz ihtiyacında kullanılır?", "Gruplama ve Filtreleme", "Veriyi kategori bazlı özetlemek için.", "Kod satırlarını gizlemek için.", "Şifre doğrulamak için.", "Sunucu başlatmak için."),
                S("Ortalama değeri neyi gösterir?", "Ortalama ve Toplam Analizi", "Bir veri grubunun genel merkez eğilimini.", "Veritabanı şifresini.", "CSS genişliğini.", "Tarayıcı geçmişini."),
                S("Toplam analizi hangi durumda işe yarar?", "Ortalama ve Toplam Analizi", "Gelir, satış veya adet gibi değerleri özetlemek için.", "Sadece tabloyu gizlemek için.", "Python'u kapatmak için.", "HTML dosyasını küçültmek için."),
                S("Kategorik veri analizi neyi inceler?", "Kategorik Veri Analizi", "Sınıflara ayrılmış verilerin dağılımını.", "Sadece dosya boyutunu.", "Sadece video süresini.", "Sadece IP adresini."),
                S("Analiz sonucunu yorumlamak neden önemlidir?", "Sonuç Yorumlama", "Sayısal çıktıları anlamlı kararlara dönüştürmek için.", "Kodun rengini değiştirmek için.", "Migration dosyasını küçültmek için.", "HTML sayfasını kapatmak için.")
            ]
        ),

        new(
            KursAdi: "Yapay Zeka Temelleri",
            SinavAdi: "Yapay Zeka Temelleri Final Sınavı",
            Aciklama: "Yapay zeka, makine öğrenmesi, veri kalitesi, etik, güvenlik ve AI okuryazarlığı konularını ölçen kapsamlı sınav.",
            GecmeNotu: 70,
            SureDakika: 40,
            SoruSayisi: 20,
            GunOnce: 20,
            Sorular:
            [
                S("Yapay zekanın temel amacı aşağıdakilerden hangisidir?", "Yapay Zeka Nedir?", "İnsan benzeri karar verme veya tahmin süreçlerini desteklemek.", "Sadece CSS dosyası üretmek.", "Veritabanını otomatik silmek.", "Sunucu fan hızını artırmak."),
                S("Yapay zeka günlük hayatta nerede kullanılabilir?", "Yapay Zeka Nedir?", "Öneri sistemleri ve otomatik analizlerde.", "Sadece elektrik prizlerinde.", "Sadece kağıt defterlerde.", "Sadece masa renginde."),
                S("Makine öğrenmesinde model ne yapar?", "Makine Öğrenmesi Mantığı", "Veriden örüntü öğrenerek tahmin veya sınıflandırma yapar.", "Sadece HTML etiketi yazar.", "Kullanıcı şifresini düz metin saklar.", "Dosya uzantısını değiştirir."),
                S("Makine öğrenmesinde eğitim verisi neden kullanılır?", "Makine Öğrenmesi Mantığı", "Modelin örüntü öğrenebilmesi için.", "Sunucuyu kapatmak için.", "CSS dosyası oluşturmak için.", "HTML başlığını büyütmek için."),
                S("Derin öğrenme hangi yapıyla sık ilişkilidir?", "Derin Öğrenme Kavramı", "Yapay sinir ağlarıyla.", "Sadece tablo satırlarıyla.", "Sadece HTML formlarıyla.", "Sadece cookie temizleme ile."),
                S("Derin öğrenme genellikle hangi tür verilerde kullanılabilir?", "Derin Öğrenme Kavramı", "Görüntü, ses ve büyük veri problemlerinde.", "Sadece kalem renginde.", "Sadece klasör adında.", "Sadece margin değerinde."),
                S("Veri kalitesi neden önemlidir?", "Veri Kalitesi", "Modelin daha güvenilir sonuç üretmesine katkı sağlar.", "CSS rengini artırır.", "Kullanıcı rolünü kaldırır.", "Sunucu saatini değiştirir."),
                S("Eksik veya hatalı veri model sonucunu nasıl etkileyebilir?", "Veri Kalitesi", "Sonuçların güvenilirliğini azaltabilir.", "Modeli her zaman kusursuz yapar.", "Sadece ekranda renk değiştirir.", "Veritabanını otomatik yedekler."),
                S("Model başarısını ölçmek neden gereklidir?", "Model Başarısı Nasıl Ölçülür?", "Modelin ne kadar doğru çalıştığını anlamak için.", "Video URL'sini değiştirmek için.", "Controller adını silmek için.", "Profil fotoğrafı kesmek için."),
                S("Doğruluk oranı genel olarak neyi ifade eder?", "Model Başarısı Nasıl Ölçülür?", "Modelin doğru tahminlerinin oranını.", "CSS dosya sayısını.", "Kullanıcı adının uzunluğunu.", "Sunucu disk rengini."),
                S("Eğitimde yapay zeka hangi amaçla kullanılabilir?", "Eğitimde Yapay Zeka", "Öğrenciye kişiselleştirilmiş öneriler sunmak için.", "Tüm dersleri otomatik silmek için.", "Sadece buton rengi seçmek için.", "Veritabanı portu değiştirmek için."),
                S("Sağlıkta yapay zeka hangi alanda destek sağlayabilir?", "Sağlıkta Yapay Zeka", "Tanı ve analiz süreçlerinde karar desteği sağlayabilir.", "Hastane duvar rengini seçer.", "Sadece yemek listesi oluşturur.", "Tüm kayıtları gizler."),
                S("İş süreçlerinde yapay zeka nasıl kullanılabilir?", "İş Süreçlerinde Yapay Zeka", "Raporlama, otomasyon ve karar destek süreçlerinde.", "Sadece sandalye taşımak için.", "Sadece CSS yazmak için.", "Sadece monitör kapatmak için."),
                S("Görüntü işleme hangi probleme örnektir?", "Görüntü ve Ses İşleme", "Resimdeki nesneyi tanıma.", "Sadece tablo silme.", "Sadece şifre değiştirme.", "Sadece buton boyama."),
                S("AI kullanımında etik neden önemlidir?", "Etik ve Güvenlik", "Önyargı, gizlilik ve sorumluluk risklerini azaltmak için.", "Sayfa margin değerini artırmak için.", "Ders videosunu sessize almak için.", "Kategorileri alfabetik silmek için."),
                S("Veri gizliliği AI kullanımında neden önemlidir?", "Veri Gizliliği", "Kişisel ve hassas bilgilerin korunması için.", "Tüm verileri herkese açmak için.", "Sadece font seçmek için.", "Sunucu adını değiştirmek için."),
                S("Prompt yazarken temel hedef nedir?", "Prompt Mantığı", "Modelden istenen çıktıyı açık ve bağlamlı şekilde tarif etmek.", "SQL tablosunu doğrudan silmek.", "CSS dosyasını şifrelemek.", "Tarayıcı çerezini kapatmak."),
                S("Doğru AI aracı seçerken ne dikkate alınmalıdır?", "Doğru Araç Seçimi", "İhtiyaç, görev tipi ve güvenilirlik.", "Sadece logo rengi.", "Sadece dosya adı.", "Sadece monitör markası."),
                S("AI çıktısı neden kontrol edilmelidir?", "Çıktı Kontrolü", "Yanlış, eksik veya uydurma bilgi içerebileceği için.", "Mutlaka kusursuz olduğu için.", "Sadece görsel olduğu için.", "Sadece veritabanı kaydı olduğu için."),
                S("AI araçlarına veri verirken nelere dikkat edilmelidir?", "Veri Gizliliği", "Gizli ve kişisel verilerin korunmasına.", "CSS class sayısına.", "HTML başlık boyutuna.", "Sunucu logosuna.")
            ]
        ),

        new(
            KursAdi: "Temel Matematik ve Problem Çözme",
            SinavAdi: "Temel Matematik Final Sınavı",
            Aciklama: "Temel işlemler, kesirler, oran-orantı, yüzde, denklem, problem çözme, tablo ve grafik yorumlama becerilerini ölçen kapsamlı sınav.",
            GecmeNotu: 70,
            SureDakika: 45,
            SoruSayisi: 20,
            GunOnce: 19,
            Sorular:
            [
                S("İşlem önceliğinde çarpma ve bölme hangi işlemlerden önce yapılır?", "Temel İşlem Becerileri", "Toplama ve çıkarmadan önce.", "Her zaman toplama işleminden sonra.", "Sadece parantez yoksa en sonda.", "Hiçbir zaman öncelikli değildir."),
                S("Parantez içindeki işlemler için hangisi doğrudur?", "Temel İşlem Becerileri", "Öncelikli olarak yapılır.", "Her zaman en sona bırakılır.", "Hiç dikkate alınmaz.", "Sadece bölmede kullanılır."),
                S("1/2 ile 1/4 toplamı kaçtır?", "Kesirler ve Ondalık Sayılar", "3/4", "2/6", "1/8", "4/2"),
                S("0,5 sayısı kesir olarak nasıl yazılabilir?", "Kesirler ve Ondalık Sayılar", "1/2", "1/5", "5/1", "2/1"),
                S("2 kalem 10 TL ise 6 kalem kaç TL olur?", "Oran ve Orantı", "30", "20", "40", "60"),
                S("Orantı problemlerinde temel amaç nedir?", "Oran ve Orantı", "İki çokluk arasındaki ilişkiyi kullanmak.", "Sadece sayıları toplamak.", "Tüm değerleri silmek.", "Soruyu okumadan cevaplamak."),
                S("Bir ürün 100 TL iken yüzde 20 indirim yapılırsa yeni fiyat kaç TL olur?", "Yüzde Hesapları", "80", "90", "120", "70"),
                S("200 sayısının yüzde 10'u kaçtır?", "Yüzde Hesapları", "20", "10", "30", "40"),
                S("Sayı problemlerinde bilinmeyen değer için genellikle ne kullanılır?", "Sayı Problemleri", "Değişken.", "Renk.", "Tablo başlığı.", "Dosya adı."),
                S("Ardışık sayı problemlerinde sayılar nasıl ifade edilebilir?", "Sayı Problemleri", "x, x+1, x+2 şeklinde.", "x, x, x şeklinde.", "Sadece 0 olarak.", "Sadece yüzdeyle."),
                S("Bir problemde önce ne belirlenmelidir?", "Problem Okuma Teknikleri", "Verilen ve istenen bilgiler.", "Kalemin rengi.", "Sayfanın kenarlığı.", "Sorunun fontu."),
                S("Problem çözümünde plan yapmak neden önemlidir?", "Problem Okuma Teknikleri", "Çözümü sistemli hale getirmek için.", "Cevabı rastgele seçmek için.", "Süreyi tamamen yok saymak için.", "Soru kökünü silmek için."),
                S("Sözel problemi denkleme çevirmek neden önemlidir?", "Denklem Kurma", "Problemi sistemli ve çözülebilir hale getirmek için.", "Sonucu rastgele seçmek için.", "Grafiği silmek için.", "Soruyu uzatmak için."),
                S("x + 5 = 12 denkleminde x kaçtır?", "Birinci Dereceden Denklemler", "7", "5", "12", "17"),
                S("2x = 18 denkleminde x kaçtır?", "Birinci Dereceden Denklemler", "9", "18", "20", "2"),
                S("Yaş problemlerinde genellikle hangi yöntem kullanılır?", "Yaş ve İşçi Problemleri", "Bilinmeyenleri değişkenle ifade etmek.", "Tüm sayıları yok saymak.", "Sadece tahmin etmek.", "Soruyu okumadan cevaplamak."),
                S("İşçi problemlerinde iş yapma hızı neyi ifade eder?", "Yaş ve İşçi Problemleri", "Birim zamanda yapılan iş miktarını.", "Kişinin yaşını.", "Ürünün fiyatını.", "Tablonun rengini."),
                S("Tablo okuma sorularında temel amaç nedir?", "Tablo Okuma", "Verilen bilgileri düzenli biçimde yorumlamak.", "Tabloyu silmek.", "Başlığı değiştirmek.", "Renk seçmek."),
                S("Grafik yorumlama sorularında neye dikkat edilmelidir?", "Grafik Yorumlama", "Eksen, değer ve başlık bilgilerine.", "Sadece font rengine.", "Sadece soru numarasına.", "Sadece sayfa boşluğuna."),
                S("Sonuç kontrolü neden yapılır?", "Sonuç Kontrolü", "Bulunan cevabın problem şartlarına uygun olup olmadığını görmek için.", "Cevabı daha karmaşık yapmak için.", "Soruyu silmek için.", "Sadece süre doldurmak için.")
            ]
        ),

        new(
            KursAdi: "İngilizce Konuşma Pratiği",
            SinavAdi: "İngilizce Konuşma Pratiği Final Sınavı",
            Aciklama: "Günlük konuşma, tanışma, soru sorma, restoran-alışveriş diyalogları ve telaffuz farkındalığını ölçen sınav.",
            GecmeNotu: 70,
            SureDakika: 25,
            SoruSayisi: 10,
            GunOnce: 18,
            Sorular:
            [
                S("Tanışma sırasında 'Nice to meet you' ifadesinin anlamı nedir?", "Tanışma ve Selamlaşma", "Tanıştığıma memnun oldum.", "Günaydın.", "Nerelisin?", "Saat kaç?"),
                S("'My name is Ali' cümlesi hangi amaçla kullanılır?", "Kendini Tanıtma", "İsim söylemek için.", "Adres sormak için.", "Sipariş vermek için.", "Teşekkür etmek için."),
                S("'How are you?' sorusuna uygun cevap hangisidir?", "Kısa Diyaloglar", "I'm fine, thank you.", "It is a book.", "Blue.", "At school yesterday."),
                S("'Where are you from?' sorusu neyi sorar?", "Soru Sorma Kalıpları", "Nereli olduğunu.", "Yaşını.", "Mesleğini.", "Telefon numarasını."),
                S("Restoranda sipariş verirken hangi ifade kullanılabilir?", "Restoran ve Alışveriş Diyalogları", "I would like a coffee.", "My name is Monday.", "It rains yesterday.", "I am from table."),
                S("Alışverişte fiyat sormak için hangi ifade kullanılabilir?", "Restoran ve Alışveriş Diyalogları", "How much is this?", "Where is my name?", "I am rain.", "Blue yesterday."),
                S("Telaffuz çalışmasının amacı nedir?", "Telaffuz İpuçları", "Kelimeleri daha anlaşılır söylemek.", "Cümleleri tamamen ezberlemek.", "Sadece yazı yazmak.", "Dil bilgisini silmek."),
                S("Vurgu ve tonlama konuşmada ne sağlar?", "Vurgu ve Tonlama", "Daha doğal ve anlaşılır konuşma sağlar.", "Tüm kelimeleri anlamsız yapar.", "Sadece yazı boyutunu artırır.", "Dinlemeyi gereksiz kılar."),
                S("Konuşma akışında bağlaçlar ne sağlar?", "Konuşma Akışı", "Cümleler arasında daha doğal bağlantı kurar.", "Her cümleyi anlamsız yapar.", "Sadece kelime sayısını azaltır.", "Dinlemeyi imkansızlaştırır."),
                S("Dinleme ve yanıt verme becerisi neden önemlidir?", "Dinleme ve Yanıt Verme", "Karşı tarafı anlayıp uygun cevap vermek için.", "Sadece sessiz kalmak için.", "Sadece yazı yazmak için.", "Konu değiştirmek için.")
            ]
        ),

        new(
            KursAdi: "Etkili İletişim ve Sunum Teknikleri",
            SinavAdi: "Etkili İletişim Final Sınavı",
            Aciklama: "Dinleme, beden dili, mesaj verme, empati, geri bildirim, sunum planı ve sahne yönetimi konularını ölçen sınav.",
            GecmeNotu: 70,
            SureDakika: 25,
            SoruSayisi: 10,
            GunOnce: 16,
            Sorular:
            [
                S("Etkili dinleme neyi gerektirir?", "Etkili Dinleme", "Karşı tarafı dikkatle anlamaya çalışmayı.", "Sürekli söz kesmeyi.", "Telefonla ilgilenmeyi.", "Hiç tepki vermemeyi."),
                S("Beden dili iletişimde neyi etkiler?", "Beden Dili", "Mesajın nasıl algılandığını.", "Sadece dosya boyutunu.", "Sunucu hızını.", "Veritabanı adını."),
                S("Doğru mesaj verme için hangisi önemlidir?", "Doğru Mesaj Verme", "Net ve anlaşılır ifade kullanmak.", "Konuyu sürekli değiştirmek.", "Belirsiz konuşmak.", "Dinleyiciyi yok saymak."),
                S("Empati kurmak ne demektir?", "Empati Kurma", "Karşı tarafın bakış açısını anlamaya çalışmak.", "Her zaman tartışmak.", "Hiç dinlememek.", "Sadece emir vermek."),
                S("Yapıcı geri bildirim nasıl olmalıdır?", "Geri Bildirim Verme", "Somut, saygılı ve geliştirici.", "Kırıcı ve belirsiz.", "Kişiyi hedef alan.", "Tamamen ilgisiz."),
                S("Sunum planı neden hazırlanır?", "Sunum Planı", "Anlatımı düzenli ve anlaşılır hale getirmek için.", "Sunumu rastgele yapmak için.", "Süreyi tamamen yok saymak için.", "Konuyu dağıtmak için."),
                S("Sahne heyecanını azaltmak için ne yapılabilir?", "Sahne Heyecanı", "Hazırlık ve pratik yapmak.", "Hiç hazırlanmamak.", "Dinleyiciden kaçmak.", "Sunumu iptal etmek."),
                S("Görsel destek kullanımı sunumda ne sağlar?", "Görsel Destek Kullanımı", "Ana mesajın daha anlaşılır olmasını destekler.", "Sunumu her zaman bozar.", "Konuyu tamamen gizler.", "Dinleyiciyi yok sayar."),
                S("Dinleyiciyle etkileşim neden önemlidir?", "Dinleyiciyle Etkileşim", "Sunumu daha canlı ve katılımcı hale getirir.", "Sunumu tamamen durdurmak için.", "Konuşmacıyı susturmak için.", "Başlığı silmek için."),
                S("Sunum kapanışı nasıl olmalıdır?", "Sunum Sonu Kapanış", "Ana mesajı özetleyen net bir kapanışla.", "Konudan bağımsız şekilde.", "Aniden ve açıklamasız.", "Dinleyiciyi suçlayarak.")
            ]
        ),

        new(
            KursAdi: "Akademik Yazma Becerileri",
            SinavAdi: "Akademik Yazma Becerileri Final Sınavı",
            Aciklama: "Akademik metin yapısı, paragraf oluşturma, akademik dil, kaynak kullanımı, atıf ve metin kontrolünü ölçen sınav.",
            GecmeNotu: 70,
            SureDakika: 30,
            SoruSayisi: 10,
            GunOnce: 9,
            Sorular:
            [
                S("Akademik yazının temel özelliklerinden biri nedir?", "Akademik Yazıya Giriş", "Nesnel, düzenli ve kaynak destekli olması.", "Tamamen günlük konuşma diliyle yazılması.", "Kaynak kullanılmaması.", "Paragraf yapısının önemsenmemesi."),
                S("Güçlü bir paragraf genellikle ne içerir?", "Paragraf Oluşturma", "Ana fikir, açıklama ve destekleyici cümleler.", "Sadece tek kelime.", "Rastgele emoji kullanımı.", "Kaynakçasız görseller."),
                S("Akademik dil kullanımında hangi ifade daha uygundur?", "Akademik Dil Kullanımı", "Nesnel ve açık ifadeler kullanmak.", "Argo ifadeleri yoğun kullanmak.", "Kanıtsız kesin yargılar vermek.", "Konu dışına çıkmak."),
                S("Tez cümlesi neyi ifade eder?", "Tez Cümlesi Yazma", "Metnin ana savını veya temel fikrini.", "Kaynakça listesini.", "Sayfa numarasını.", "Yazı tipi boyutunu."),
                S("Geçiş ifadeleri ne işe yarar?", "Geçiş İfadeleri", "Paragraflar ve fikirler arasında akıcılık sağlar.", "Metni tamamen böler.", "Kaynakları siler.", "Başlığı gizler."),
                S("Akademik kaynak seçerken neye dikkat edilmelidir?", "Kaynak Seçimi", "Güvenilirlik ve konu ile ilgililiğe.", "Sadece renkli olmasına.", "Kaynağın kısa adına.", "Rastgele seçilmesine."),
                S("Alıntı ve atıf neden kullanılır?", "Alıntı ve Atıf", "Yararlanılan kaynağı belirtmek ve akademik dürüstlük sağlamak için.", "Metni uzatmak için.", "Kaynağı gizlemek için.", "Paragrafı silmek için."),
                S("Parafraz ne demektir?", "Özetleme ve Parafraz", "Kaynak fikrini kendi cümleleriyle yeniden ifade etmek.", "Metni aynen kopyalamak.", "Kaynak göstermemek.", "Sadece başlığı değiştirmek."),
                S("Kaynakça hazırlamanın amacı nedir?", "Kaynakça Hazırlama", "Kullanılan kaynakları düzenli biçimde göstermek.", "Metindeki tüm kaynakları saklamak.", "Paragrafları renklendirmek.", "Sınav süresini artırmak."),
                S("Metin kontrolünde hangi unsur incelenir?", "Metin Kontrolü", "Yazım, anlatım, tutarlılık ve kaynak kullanımı.", "Sadece dosya adı.", "Sadece monitör parlaklığı.", "Sadece sayfa arka planı.")
            ]
        ),

        new(
            KursAdi: "Temel Finans Okuryazarlığı",
            SinavAdi: "Temel Finans Final Sınavı",
            Aciklama: "Bütçe, gelir-gider takibi, tasarruf, borç yönetimi, acil durum fonu ve temel yatırım kavramlarını ölçen sınav.",
            GecmeNotu: 70,
            SureDakika: 25,
            SoruSayisi: 10,
            GunOnce: 15,
            Sorular:
            [
                S("Bütçe oluşturmanın temel amacı nedir?", "Bütçe Nedir?", "Gelir ve giderleri planlamak.", "Tüm geliri rastgele harcamak.", "Borçları gizlemek.", "Tasarrufu imkansız kılmak."),
                S("Gelir-gider takibi ne sağlar?", "Gelir ve Gider Takibi", "Paranın nereye gittiğini görmeyi.", "Harcamaları görünmez yapmayı.", "Geliri otomatik artırmayı.", "Borçları silmeyi."),
                S("Tasarruf alışkanlığı neden önemlidir?", "Tasarruf Alışkanlığı", "Finansal güvenlik ve hedefler için.", "Gereksiz harcamayı artırmak için.", "Bütçeyi bozmak için.", "Geliri azaltmak için."),
                S("Borç yönetiminde öncelik nedir?", "Borç Yönetimi", "Ödeme planını düzenli takip etmek.", "Borçları yok saymak.", "Daha fazla kontrolsüz borçlanmak.", "Geliri takip etmemek."),
                S("Acil durum fonu ne içindir?", "Acil Durum Fonu", "Beklenmeyen giderlere hazırlık için.", "Sadece keyfi alışveriş için.", "Borç artırmak için.", "Geliri saklamak için."),
                S("Risk ve getiri arasında nasıl bir ilişki vardır?", "Risk ve Getiri", "Genellikle getiri beklentisi arttıkça risk de artabilir.", "Risk her zaman sıfırdır.", "Getiri hiçbir zaman değişmez.", "Risk sadece bütçede vardır."),
                S("Enflasyon neyi ifade eder?", "Temel Finansal Terimler", "Genel fiyat düzeyindeki artışı.", "Gelirin kesin artacağını.", "Borçların otomatik silineceğini.", "Paranın hiç değer kaybetmeyeceğini."),
                S("Yatırım araçlarını tanımak neden önemlidir?", "Yatırım Araçlarını Tanıma", "Risk ve özellikleri karşılaştırabilmek için.", "Rastgele karar vermek için.", "Bütçeyi yok saymak için.", "Tasarrufu durdurmak için."),
                S("Uzun vadeli planlama ne sağlar?", "Uzun Vadeli Planlama", "Finansal hedeflere daha düzenli ilerlemeyi.", "Tüm geliri hemen harcamayı.", "Borçları artırmayı.", "Geliri takip etmemeyi."),
                S("Finansal hedef belirleme nasıl olmalıdır?", "Finansal Hedef Belirleme", "Gerçekçi ve ölçülebilir şekilde.", "Belirsiz ve takip edilemez.", "Sadece sözlü ve unutulabilir.", "Tamamen rastgele.")
            ]
        ),

        new(
            KursAdi: "Dijital Pazarlamaya Giriş",
            SinavAdi: "Dijital Pazarlama Final Sınavı",
            Aciklama: "Hedef kitle, marka konumlandırma, dijital kanallar, müşteri yolculuğu, kampanya planlama ve performans ölçümü konularını ölçen sınav.",
            GecmeNotu: 70,
            SureDakika: 25,
            SoruSayisi: 10,
            GunOnce: 14,
            Sorular:
            [
                S("Hedef kitle belirlemek neden önemlidir?", "Hedef Kitle Belirleme", "Doğru kişilere doğru mesajı ulaştırmak için.", "Tüm kullanıcılara aynı mesajı zorla göstermek için.", "Markayı gizlemek için.", "Kampanyayı ölçmemek için."),
                S("Marka konumlandırma neyi ifade eder?", "Marka Konumlandırma", "Markanın hedef kitle zihnindeki yerini.", "Sadece logo dosya adını.", "Sunucu konumunu.", "Veritabanı şifresini."),
                S("Dijital kanallara örnek hangisidir?", "Dijital Kanallar", "Sosyal medya ve web sitesi.", "Sadece fiziksel pano.", "Sadece kağıt broşür.", "Sadece el yazısı not."),
                S("Müşteri yolculuğu neyi inceler?", "Müşteri Yolculuğu", "Kullanıcının markayla temas sürecini.", "Sadece ürün rengini.", "Sunucu işlemcisini.", "Şirket binasının katını."),
                S("Pazarlama hunisi hangi aşamaları içerir?", "Pazarlama Hunisi", "Farkındalık, ilgi, karar ve aksiyon.", "Sadece ödeme alma.", "Sadece stok sayımı.", "Sadece çalışan girişi."),
                S("İçerik planı ne işe yarar?", "İçerik Planı", "Yayınları düzenli ve stratejik hale getirir.", "Kampanyayı ölçülemez yapar.", "Markayı gizler.", "Tüm içerikleri siler."),
                S("Reklam mesajı nasıl olmalıdır?", "Reklam Mesajı Hazırlama", "Kısa, net ve hedef kitleye uygun.", "Karmaşık ve ilgisiz.", "Tamamen belirsiz.", "Ürünü hiç anlatmayan."),
                S("Sosyal medya yayın takvimi ne sağlar?", "Sosyal Medya Yayın Takvimi", "Düzenli ve planlı paylaşım yapmayı.", "Tüm paylaşımları silmeyi.", "Markayı gizlemeyi.", "Hedef kitleyi yok saymayı."),
                S("Performans ölçümünde hangi metrik kullanılabilir?", "Performans Ölçümü", "Tıklama ve dönüşüm oranı.", "Sadece masa yüksekliği.", "Sadece logo rengi.", "Sadece dosya adı."),
                S("Kampanya iyileştirme neye dayanmalıdır?", "Kampanya İyileştirme", "Veri ve performans sonuçlarına.", "Sadece tahmine.", "Sadece rastgele değişikliğe.", "Ölçüm yapmamaya.")
            ]
        ),

        new(
            KursAdi: "Girişimcilik ve İş Modeli Geliştirme",
            SinavAdi: "Girişimcilik ve İş Modeli Final Sınavı",
            Aciklama: "İş fikri, problem analizi, değer önerisi, pazar araştırması, müşteri segmentleri ve iş modeli mantığını ölçen sınav.",
            GecmeNotu: 70,
            SureDakika: 30,
            SoruSayisi: 10,
            GunOnce: 8,
            Sorular:
            [
                S("İş fikri geliştirirken ilk odaklanılması gereken konulardan biri nedir?", "İş Fikri Bulma", "Gerçek bir problemi ve ihtiyacı belirlemek.", "Sadece logo tasarlamak.", "Rastgele ürün seçmek.", "Müşteriyi hiç düşünmemek."),
                S("Problem analizi neden yapılır?", "Problem Analizi", "Çözülmek istenen ihtiyacın gerçekliğini anlamak için.", "Sorunu gizlemek için.", "Ürünü rastgele büyütmek için.", "Pazarı yok saymak için."),
                S("Değer önerisi neyi açıklar?", "Değer Önerisi", "Ürün veya hizmetin kullanıcıya sunduğu temel faydayı.", "Sadece şirket adresini.", "Sadece vergi numarasını.", "Sadece dosya adını."),
                S("Pazar araştırması ne sağlar?", "Pazar Araştırması", "Hedef kitle, rakipler ve ihtiyaç hakkında bilgi sağlar.", "Tüm riskleri yok eder.", "Müşteriyi gereksiz kılar.", "Geliri otomatik artırır."),
                S("İlk müşteri profili ne için belirlenir?", "İlk Müşteri Profili", "Üründen en erken fayda sağlayacak kullanıcıyı tanımak için.", "Ürünü saklamak için.", "Fiyatı rastgele seçmek için.", "Kategoriyi silmek için."),
                S("Gelir modeli neyi ifade eder?", "Gelir Modeli", "İşin nasıl gelir elde edeceğini.", "Sadece logo rengini.", "Sadece ofis yerini.", "Sadece çalışan sayısını."),
                S("Müşteri segmentleri neyi gösterir?", "Müşteri Segmentleri", "Farklı kullanıcı gruplarını ve ihtiyaçlarını.", "Sadece tek dosya adını.", "Sadece veritabanı portunu.", "Sadece ürün rengini."),
                S("Maliyet kalemleri neden önemlidir?", "Maliyet Kalemleri", "İşin giderlerini ve kaynak ihtiyacını görmek için.", "Giderleri gizlemek için.", "Fiyatı tamamen yok saymak için.", "Satışı durdurmak için."),
                S("Dağıtım kanalı neyi ifade eder?", "Dağıtım Kanalları", "Ürün veya hizmetin müşteriye nasıl ulaştırılacağını.", "Sadece ödeme şifresini.", "Sadece yazı tipini.", "Sadece tablo rengini."),
                S("Mini iş modeli taslağı ne amaçla hazırlanır?", "Mini İş Modeli Taslağı", "İş fikrinin temel parçalarını düzenli görmek için.", "Fikri tamamen gizlemek için.", "Pazarı yok saymak için.", "Müşteri ihtiyacını silmek için.")
            ]
        ),

        new(
            KursAdi: "Canva ile Görsel Tasarım",
            SinavAdi: "Canva ile Görsel Tasarım Final Sınavı",
            Aciklama: "Renk, tipografi, düzen, boşluk kullanımı, şablon, marka uyumu ve uygulamalı tasarım konularını ölçen sınav.",
            GecmeNotu: 70,
            SureDakika: 25,
            SoruSayisi: 10,
            GunOnce: 13,
            Sorular:
            [
                S("Renk uyumu tasarımda ne sağlar?", "Renk ve Tipografi", "Görsel bütünlük ve okunabilirlik sağlar.", "Tasarımı tamamen bozmak için kullanılır.", "Sadece dosya boyutunu artırır.", "Metni görünmez yapar."),
                S("Tipografi neyle ilgilidir?", "Renk ve Tipografi", "Yazı tipi, punto, aralık ve okunabilirlikle.", "Sadece video süresiyle.", "Sadece SQL sorgusuyla.", "Sadece tarayıcı geçmişiyle."),
                S("Düzen ve hiyerarşi ne işe yarar?", "Düzen ve Hiyerarşi", "Gözün önemli bilgilere doğru yönlenmesini sağlar.", "Tüm öğeleri rastgele dağıtır.", "Metni siler.", "Rengi otomatik kapatır."),
                S("Boşluk kullanımı neden önemlidir?", "Boşluk Kullanımı", "Tasarımın daha temiz ve okunabilir görünmesini sağlar.", "Tasarımı kalabalıklaştırmak için.", "Sadece dosya adını değiştirmek için.", "Renkleri yok etmek için."),
                S("Canva şablonları ne amaçla kullanılabilir?", "Şablon Kullanımı", "Hazır tasarımları ihtiyaca göre düzenlemek için.", "Veritabanı tablosu oluşturmak için.", "Şifre sıfırlamak için.", "Sunucu kurmak için."),
                S("Marka uyumu neyi kapsar?", "Marka Uyumu", "Renk, logo, tipografi ve görsel dil tutarlılığını.", "Sadece internet hızını.", "Sadece klasör adını.", "Sadece sınav süresini."),
                S("Sosyal medya görselinde dikkat edilmesi gerekenlerden biri nedir?", "Sosyal Medya Görseli", "Mesajın hızlı ve net anlaşılması.", "Metnin tamamen görünmez olması.", "Tüm renklerin rastgele seçilmesi.", "Görselin amaçsız olması."),
                S("Sunum tasarımında dikkat edilmesi gerekenlerden biri nedir?", "Sunum Tasarımı", "Az metin, net başlık ve görsel denge.", "Her slayta çok uzun metin yazmak.", "Renkleri rastgele seçmek.", "Başlık kullanmamak."),
                S("Mini marka kiti ne içerir?", "Mini Marka Kiti", "Renk, yazı tipi ve temel görsel kullanım kuralları.", "Sadece rastgele fotoğraf.", "Sadece video süresi.", "Sadece veritabanı id değeri."),
                S("Tasarım kontrol listesi ne işe yarar?", "Tasarım Kontrol Listesi", "Yayın öncesi hataları ve eksikleri kontrol etmeye yarar.", "Dosyayı bozmak için.", "Metni rastgele silmek için.", "Tasarımı görünmez yapmak için.")
            ]
        )
    ];

    private static DemoSoruBilgisi S(
        string soruMetni,
        string dersAdi,
        string dogruSecenek,
        string yanlisSecenek1,
        string yanlisSecenek2,
        string yanlisSecenek3)
    {
        return new DemoSoruBilgisi(
            SoruMetni: soruMetni,
            DersAdi: dersAdi,
            Secenekler:
            [
                dogruSecenek,
                yanlisSecenek1,
                yanlisSecenek2,
                yanlisSecenek3
            ],
            DogruSecenekNo: 1
        );
    }

    public sealed record DemoSinavBilgisi(
        string KursAdi,
        string SinavAdi,
        string? Aciklama,
        int GecmeNotu,
        int SureDakika,
        int SoruSayisi,
        int GunOnce,
        IReadOnlyList<DemoSoruBilgisi> Sorular
    );

    public sealed record DemoSoruBilgisi(
        string SoruMetni,
        string DersAdi,
        IReadOnlyList<string> Secenekler,
        int DogruSecenekNo
    );
}