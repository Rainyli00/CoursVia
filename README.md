# CoursVia

**CoursVia**, web ve mobil destekli, yapay zekâ özellikleriyle güçlendirilmiş çevrim içi eğitim platformudur. Sistem; öğrenci, eğitmen ve yönetici rollerine göre farklı paneller sunar. Kurs oluşturma, ders takibi, sınav yönetimi, sertifika doğrulama, bildirimler, eğitmen başvuru/onay süreçleri ve yapay zekâ destekli öneri üretimi gibi temel eğitim platformu işlevlerini tek bir yapı altında toplar.

Proje; ASP.NET Core MVC/Web API tabanlı backend, Expo React Native mobil uygulama ve Python/PyTorch ile geliştirilen MiniCoursViaLLM yapay zekâ modülünden oluşmaktadır.

---

## İçindekiler

* [Proje Hakkında](#proje-hakkında)
* [Temel Özellikler](#temel-özellikler)
* [Teknolojiler](#teknolojiler)
* [Proje Mimarisi](#proje-mimarisi)
* [Yapay Zekâ Modülü](#yapay-zekâ-modülü)
* [Kurulum](#kurulum)
* [Klasör Yapısı](#klasör-yapısı)
* [Ekran Görüntüleri](#ekran-görüntüleri)
* [Geliştirici](#geliştirici)

---

## Proje Hakkında

CoursVia, çevrim içi eğitim süreçlerini daha düzenli, erişilebilir ve akıllı hale getirmek amacıyla geliştirilmiştir. Platformda öğrenciler kurslara katılabilir, dersleri takip edebilir, sınavlara girebilir, sertifika kazanabilir ve yapay zekâ destekli çalışma önerileri alabilir.

Eğitmenler kurs oluşturabilir, ders ve sınav yönetimi yapabilir, öğrencilerinin performansını takip edebilir ve yapay zekâ destekli kurs analizi alabilir. Yönetici paneli ise kullanıcı, kurs, eğitmen başvurusu, yorum, bildirim ve sistem kayıtlarını yönetmek için kullanılır.

Bu proje yalnızca klasik bir eğitim platformu değildir. Aynı zamanda farklı yapay zekâ yaklaşımlarını aynı eğitim senaryolarında kullanabilen ve karşılaştırabilen bütünleşik bir sistemdir.

---

## Temel Özellikler

### Öğrenci Özellikleri

* Kurs keşfetme ve kurslara katılma
* Kayıtlı kursları görüntüleme
* Ders ilerleme takibi
* Sınav sonuçlarını görüntüleme
* Sertifika görüntüleme ve doğrulama
* Bildirimleri takip etme
* Yapay zekâ destekli çalışma önerileri alma

### Eğitmen Özellikleri

* Eğitmen başvurusu oluşturma
* Kurs oluşturma ve düzenleme
* Bölüm, ders ve materyal yönetimi
* Sınav ve soru yönetimi
* Öğrenci performansını takip etme
* Yapay zekâ destekli kurs analizi alma

### Yönetici Özellikleri

* Kullanıcı yönetimi
* Kurs onay süreçleri
* Eğitmen başvuru yönetimi
* Kategori yönetimi
* Yorum ve değerlendirme yönetimi
* Bildirim ve sistem log takibi
* Mobil admin paneli desteği

### Mobil Uygulama Özellikleri

* Öğrenci, eğitmen ve admin rol seçimi
* JWT tabanlı mobil oturum yönetimi
* Refresh token desteği
* Kurslar, bildirimler, sertifikalar ve AI önerileri ekranları
* QR kod ile sertifika doğrulama
* Expo Router tabanlı sayfa yapısı

---

## Teknolojiler

### Backend

* ASP.NET Core 8
* ASP.NET Core MVC
* ASP.NET Core Web API
* Entity Framework Core
* Microsoft SQL Server
* Cookie Authentication
* JWT Authentication
* BCrypt.Net
* QuestPDF
* QRCoder
* MailKit

### Mobil

* Expo
* React Native
* TypeScript
* Expo Router
* Axios
* Expo Secure Store
* Expo Camera

### Yapay Zekâ

* Python
* PyTorch
* Transformer tabanlı MiniCoursViaLLM
* BPE tokenizer
* Google Gemini API
* Local Gemma 3 12B Instruct
* LM Studio / OpenAI uyumlu local API yapısı

---

## Proje Mimarisi

CoursVia üç ana bileşenden oluşur:

1. **CoursVia Web & API**

   * Web arayüzleri
   * Rol bazlı paneller
   * Mobil uygulama için REST API endpointleri
   * Kimlik doğrulama ve yetkilendirme
   * Veritabanı işlemleri

2. **CoursViaMobile**

   * Expo React Native mobil uygulaması
   * Öğrenci, eğitmen ve admin mobil ekranları
   * JWT access token ve refresh token yönetimi
   * Backend API ile haberleşme

3. **CoursViaAI**

   * MiniCoursViaLLM modeli
   * Veri üretimi
   * Tokenizer eğitimi
   * Model eğitimi
   * Model değerlendirme
   * Backend tarafından çağrılabilen üretim scripti

---

## Yapay Zekâ Modülü

CoursVia içinde üç farklı yapay zekâ yaklaşımı kullanılmıştır:

| Model                | Kullanım Amacı                                                              |
| -------------------- | --------------------------------------------------------------------------- |
| MiniCoursViaLLM      | CoursVia senaryolarına özel öğrenci önerisi ve eğitmen kurs analizi üretimi |
| Gemma 3 12B Instruct | Yerel ortamda güçlü alternatif çıktı üretimi                                |
| Gemini API           | Bulut tabanlı güçlü dil modeli çıktısı üretimi                              |

MiniCoursViaLLM, genel amaçlı bir sohbet modeli olarak değil, CoursVia içindeki eğitim senaryolarına özel kontrollü öneriler üretmek amacıyla geliştirilmiştir.

### MiniCoursViaLLM Eğitim Bilgileri

* Veri seti: 40.000 sentetik örnek
* Eğitim / doğrulama / test ayrımı: %80 / %10 / %10
* Sözlük boyutu: 12.000 token
* Bağlam uzunluğu: 512 token
* Embedding boyutu: 512
* Attention head sayısı: 8
* Transformer katmanı: 8
* Dropout: 0,10
* Learning rate: 2.5e-4
* Eğitim iterasyonu: 4000
* En iyi validation loss: yaklaşık 0,3145

### Otomatik Kör Test Sonucu

MiniCoursViaLLM, test kümesinden rastgele seçilen 100 örnek üzerinde otomatik kör testten geçirilmiştir.

| Ölçüt                     |  Sonuç |
| ------------------------- | -----: |
| Test sayısı               |    100 |
| Doğrudan geçerli çıktı    |     85 |
| Başarı oranı              | %85,00 |
| Fallback gerektiren çıktı |     15 |
| Format hatası             |      0 |
| Sistem etiketi sızıntısı  |      0 |
| Kısa/boş çıktı            |      0 |
| Veri kopyalama hatası     |     15 |

Bu sonuç, modelin çıktı formatını ve güvenlik kurallarını genel olarak koruduğunu; ancak bazı durumlarda kurs kodu veya ders kodu gibi alanları birebir korumakta zorlandığını göstermektedir. Bu nedenle sistemde fallback ve çıktı güvenlik filtresi kullanılmıştır.

---

## Kurulum

### 1. Depoyu Klonlama

```bash
git clone https://github.com/kullanici-adi/CoursVia.git
cd CoursVia
```

---

## Backend Kurulumu

Backend projesi `CoursVia` klasörü altında yer alır.

```bash
cd CoursVia
dotnet restore
```

### Veritabanı Ayarı

`appsettings.json` içindeki bağlantı cümlesini kendi SQL Server ayarınıza göre düzenleyin:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=CoursViaDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Migration işlemleri için:

```bash
dotnet ef database update
```

Uygulamayı çalıştırmak için:

```bash
dotnet run
```

Varsayılan olarak backend şu adreste çalışır:

```text
http://localhost:5024
```

---

## Mobil Uygulama Kurulumu

Mobil uygulama `CoursViaMobile` klasörü altında yer alır.

```bash
cd CoursViaMobile
npm install
```

Backend API adresini `src/api/client.ts` dosyasından düzenleyin:

```ts
export const API_BASE_URL = "http://10.0.2.2:5024";
```

Android emülatör için:

```bash
npm run android
```

Expo başlatmak için:

```bash
npm start
```

Gerçek cihazda test yapılacaksa `10.0.2.2` yerine bilgisayarın yerel IPv4 adresi yazılmalıdır.

Örnek:

```ts
export const API_BASE_URL = "http://192.168.1.25:5024";
```

---

## Yapay Zekâ Modülü Kurulumu

Yapay zekâ modülü `CoursViaAI` klasörü altında yer alır.

```bash
cd CoursViaAI
python -m venv .venv
```

Windows için sanal ortamı etkinleştirme:

```bash
.venv\Scripts\activate
```

Gerekli paketleri yükleme:

```bash
pip install torch numpy tokenizers
```

Veri seti üretimi:

```bash
python data_generator.py
```

Tokenizer eğitimi:

```bash
python tokenizer_train.py
```

Model eğitimi:

```bash
python train.py
```

Model testi:

```bash
python evaluate.py
```

Backend tarafında MiniCoursViaLLM entegrasyonu için `appsettings.json` içindeki yollar kendi bilgisayarınıza göre düzenlenmelidir:

```json
"MiniCoursVia": {
  "PythonExePath": "C:\\Path\\To\\CoursViaAI\\.venv\\Scripts\\python.exe",
  "ScriptPath": "C:\\Path\\To\\CoursViaAI\\demo_generate_api.py",
  "WorkingDirectory": "C:\\Path\\To\\CoursViaAI",
  "TimeoutSeconds": 180
}
```

---

## Gemini ve Local Gemma Ayarları

Gemini API kullanılacaksa:

```json
"Gemini": {
  "ApiKey": "GEMINI_API_KEY",
  "Model": "gemini-3-flash-preview"
}
```

Local Gemma için LM Studio veya OpenAI uyumlu local endpoint kullanılabilir:

```json
"LocalGemma": {
  "BaseUrl": "http://localhost:1234/v1",
  "Model": "gemma-3-12b-it"
}
```

> Güvenlik notu: API anahtarları, e-posta şifreleri ve JWT secret değerleri doğrudan GitHub reposuna yüklenmemelidir. Gerçek projede environment variable veya user secrets kullanılması önerilir.

---

## Klasör Yapısı

```text
CoursVia/
│
├── CoursVia/                 # ASP.NET Core MVC + Web API projesi
│   ├── Controllers/          # Web ve mobil API controllerları
│   ├── Data/                 # DbContext ve seed dosyaları
│   ├── Migrations/           # Entity Framework migration dosyaları
│   ├── Models/               # Veritabanı modelleri
│   ├── Services/             # İş servisleri ve AI servisleri
│   ├── ViewModels/           # ViewModel sınıfları
│   └── Views/                # MVC Razor view dosyaları
│
├── CoursViaMobile/           # Expo React Native mobil uygulaması
│   ├── app/                  # Expo Router ekranları
│   ├── src/api/              # Axios API client
│   ├── src/auth/             # Mobil oturum ve token işlemleri
│   └── src/types/            # TypeScript API tipleri
│
├── CoursViaAI/               # MiniCoursViaLLM yapay zekâ modülü
│   ├── data/                 # Eğitim, doğrulama ve test verileri
│   ├── models/               # Eğitilmiş model ve tokenizer dosyaları
│   ├── config.py             # Model ve eğitim ayarları
│   ├── data_generator.py     # Sentetik veri üretimi
│   ├── model.py              # Transformer model mimarisi
│   ├── train.py              # Model eğitim scripti
│   ├── evaluate.py           # Model test scripti
│   └── generate.py           # Model çıktı üretim işlemleri
│
└── CoursVia.sln              # Visual Studio çözüm dosyası
```

---

## Ekran Görüntüleri

> Bu alana proje ekran görüntüleri eklenebilir.

```text
docs/screenshots/
├── web-home.png
├── ogrenci-panel.png
├── egitmen-panel.png
├── admin-panel.png
├── mobile-login.png
├── mobile-ogrenci.png
└── ai-oneri.png
```

Örnek kullanım:

```md
![CoursVia Ana Sayfa](docs/screenshots/web-home.png)
```

---

## Öne Çıkan Teknik Noktalar

* Web ve mobil uygulama aynı backend altyapısını kullanır.
* Web tarafında cookie authentication kullanılır.
* Mobil tarafta JWT access token ve refresh token yapısı vardır.
* Öğrenci, eğitmen ve admin rolleri ayrı panellerle yönetilir.
* AI önerileri hem web hem mobil tarafta görüntülenebilir.
* Sertifika doğrulama ve QR tabanlı kontrol desteği vardır.
* Yönetici işlemleri loglanabilir.
* AI çıktıları güvenlik filtresinden geçirilir.
* MiniCoursViaLLM, dış servise bağımlı olmadan özel öneri üretebilir.

---

## Lisans

Bu proje eğitim ve bitirme projesi kapsamında geliştirilmiştir. Lisans bilgisi daha sonra eklenecektir.
