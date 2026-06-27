<div align="center">

# 🎓 CoursVia

### Yapay Zekâ Destekli Web ve Mobil Çevrim İçi Eğitim Platformu

CoursVia; öğrenci, eğitmen ve yönetici rollerine sahip, web ve mobil destekli, yapay zekâ tabanlı öneri ve analiz özellikleri sunan modern bir çevrim içi eğitim platformudur.

<br/>

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-512BD4?style=for-the-badge\&logo=dotnet\&logoColor=white)
![React Native](https://img.shields.io/badge/React%20Native-Expo-61DAFB?style=for-the-badge\&logo=react\&logoColor=black)
![TypeScript](https://img.shields.io/badge/TypeScript-Mobile-3178C6?style=for-the-badge\&logo=typescript\&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-CC2927?style=for-the-badge\&logo=microsoftsqlserver\&logoColor=white)
![PyTorch](https://img.shields.io/badge/PyTorch-AI%20Module-EE4C2C?style=for-the-badge\&logo=pytorch\&logoColor=white)
![Gemini](https://img.shields.io/badge/Gemini%20API-AI-4285F4?style=for-the-badge\&logo=google\&logoColor=white)

</div>

---

## 📌 Proje Hakkında

**CoursVia**, çevrim içi eğitim süreçlerini daha erişilebilir, yönetilebilir ve akıllı hale getirmek amacıyla geliştirilmiş bütünleşik bir eğitim platformudur.

Platform; klasik kurs yönetimi işlevlerinin yanında yapay zekâ destekli öğrenci çalışma önerileri ve eğitmen kurs analizleri sunar. Böylece öğrenci yalnızca sınav sonucunu görmekle kalmaz, hangi konulara çalışması gerektiğine yönelik öneriler alır. Eğitmen ise kurs performansını analiz ederek içerik geliştirme sürecinde yapay zekâ desteğinden yararlanabilir.

CoursVia; **web uygulaması**, **mobil uygulama**, **REST API**, **SQL Server veritabanı** ve **MiniCoursViaLLM yapay zekâ modülü** olmak üzere birden fazla bileşenden oluşmaktadır.

---

## ✨ Öne Çıkan Özellikler

<table>
<tr>
<td width="50%">

### 👨‍🎓 Öğrenci Paneli

* Kurslara katılma
* Ders ilerleme takibi
* Sınavlara girme
* Sertifika görüntüleme
* Bildirimleri takip etme
* Yapay zekâ destekli çalışma önerileri alma

</td>
<td width="50%">

### 👨‍🏫 Eğitmen Paneli

* Kurs oluşturma
* Bölüm ve ders yönetimi
* Sınav ve soru yönetimi
* Öğrenci performansı takibi
* Kurs analizi görüntüleme
* Yapay zekâ destekli geliştirme önerileri alma

</td>
</tr>
<tr>
<td width="50%">

### 🛠️ Yönetici Paneli

* Kullanıcı yönetimi
* Kurs onay süreçleri
* Eğitmen başvuru yönetimi
* Kategori yönetimi
* Yorum ve değerlendirme yönetimi
* Sistem kayıtları ve bildirim yönetimi

</td>
<td width="50%">

### 📱 Mobil Uygulama

* Expo React Native mimarisi
* JWT tabanlı oturum yönetimi
* Refresh token desteği
* Rol bazlı mobil ekranlar
* Sertifika doğrulama
* AI önerilerini mobilde görüntüleme

</td>
</tr>
</table>

---

## 🧠 Yapay Zekâ Modülü

CoursVia içinde üç farklı yapay zekâ yaklaşımı kullanılmıştır:

| Model                    | Kullanım Amacı                                                              |
| ------------------------ | --------------------------------------------------------------------------- |
| **MiniCoursViaLLM**      | CoursVia senaryolarına özel öğrenci önerisi ve eğitmen kurs analizi üretimi |
| **Gemma 3 12B Instruct** | Yerel ortamda güçlü alternatif çıktı üretimi                                |
| **Google Gemini API**    | Bulut tabanlı güçlü dil modeli çıktısı üretimi                              |

### MiniCoursViaLLM

MiniCoursViaLLM, genel amaçlı sohbet modeli olarak değil, CoursVia içindeki eğitim senaryolarına özel öneri üretmek amacıyla geliştirilmiş küçük ölçekli Transformer tabanlı bir dil modelidir.

Model iki temel görev için kullanılır:

* Öğrenci sınav sonucuna göre çalışma önerisi üretme
* Eğitmen kurs verilerine göre kurs analizi oluşturma

---

## 📊 MiniCoursViaLLM Test Sonuçları

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

Bu sonuç, modelin format ve güvenlik kurallarını genel olarak koruduğunu; ancak bazı durumlarda kurs kodu veya ders kodu gibi alanları birebir korumakta zorlandığını göstermektedir. Bu nedenle sistemde güvenlik filtresi ve fallback mekanizması kullanılmıştır.

---

## 🏗️ Sistem Mimarisi

CoursVia genel olarak üç ana katmandan oluşur:

```text
┌─────────────────────────────┐
│        Web Uygulaması       │
│   ASP.NET Core MVC Views    │
└──────────────┬──────────────┘
               │
┌──────────────▼──────────────┐
│          Backend API        │
│ ASP.NET Core Web API + MVC  │
└──────────────┬──────────────┘
               │
┌──────────────▼──────────────┐
│        SQL Server DB        │
│ Entity Framework Core ORM   │
└──────────────┬──────────────┘
               │
┌──────────────▼──────────────┐
│      Yapay Zekâ Modülü      │
│ MiniCoursViaLLM / Gemini    │
└─────────────────────────────┘
```

Mobil uygulama ise backend API ile haberleşerek öğrenci, eğitmen ve yönetici işlemlerini mobil cihazlar üzerinden kullanılabilir hale getirir.

---

## 🧰 Kullanılan Teknolojiler

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
* Transformer mimarisi
* BPE tokenizer
* MiniCoursViaLLM
* Google Gemini API
* Gemma 3 12B Instruct
* LM Studio / OpenAI uyumlu local API

---

## 📁 Proje Yapısı

```text
CoursVia/
│
├── CoursVia/                 # ASP.NET Core MVC + Web API
│   ├── Controllers/          # Web ve API controllerları
│   ├── Data/                 # DbContext ve seed işlemleri
│   ├── Migrations/           # Entity Framework migration dosyaları
│   ├── Models/               # Veritabanı modelleri
│   ├── Services/             # İş servisleri ve AI servisleri
│   ├── ViewModels/           # ViewModel sınıfları
│   └── Views/                # Razor view dosyaları
│
├── CoursViaMobile/           # Expo React Native mobil uygulaması
│   ├── app/                  # Expo Router ekranları
│   ├── src/api/              # Axios API client
│   ├── src/auth/             # Token ve oturum yönetimi
│   └── src/types/            # TypeScript tipleri
│
├── CoursViaAI/               # MiniCoursViaLLM yapay zekâ modülü
│   ├── data/                 # Eğitim, doğrulama ve test verileri
│   ├── models/               # Eğitilmiş model dosyaları
│   ├── config.py             # Model eğitim ayarları
│   ├── data_generator.py     # Sentetik veri üretimi
│   ├── model.py              # Transformer model mimarisi
│   ├── train.py              # Eğitim scripti
│   ├── evaluate.py           # Test scripti
│   └── generate.py           # Çıktı üretim işlemleri
│
└── CoursVia.sln              # Visual Studio çözüm dosyası
```

---

## 🚀 Kurulum

### 1. Depoyu Klonlama

```bash
git clone https://github.com/kullanici-adi/CoursVia.git
cd CoursVia
```

---

## ⚙️ Backend Kurulumu

```bash
cd CoursVia
dotnet restore
dotnet ef database update
dotnet run
```

`appsettings.json` içindeki bağlantı cümlesini kendi SQL Server ayarına göre düzenle:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=CoursViaDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Varsayılan backend adresi:

```text
http://localhost:5024
```

---

## 📱 Mobil Kurulum

```bash
cd CoursViaMobile
npm install
npm start
```

Android emülatörde backend bağlantısı için:

```ts
export const API_BASE_URL = "http://10.0.2.2:5024";
```

Gerçek cihazda test yapılacaksa bilgisayarın yerel IPv4 adresi kullanılmalıdır:

```ts
export const API_BASE_URL = "http://192.168.1.25:5024";
```

---

## 🤖 Yapay Zekâ Modülü Kurulumu

```bash
cd CoursViaAI
python -m venv .venv
```

Windows için:

```bash
.venv\Scripts\activate
```

Paket kurulumu:

```bash
pip install torch numpy tokenizers
```

Veri üretimi:

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

---

## 🔐 Güvenlik Notu

API anahtarları, JWT secret değerleri, e-posta şifreleri, veritabanı bağlantı bilgileri ve kişisel erişim tokenları doğrudan GitHub reposuna yüklenmemelidir.

Önerilen kullanım:

```text
appsettings.Development.json
.env
dotnet user-secrets
environment variables
```

---

## 🧪 Örnek AI Girdisi

```text
[GOREV] OGRENCI_CALISMA_ONERISI
[KURS] Türkçe Dil Bilgisi
[SINAV_PUANI] 7
[GECME_PUANI] 50
[ZORLANILAN_BOLUM] Yazım Kuralları
[ZORLANILAN_DERSLER] Paragrafta Yardımcı Düşünce, Sözcükte Çok Anlamlılık
[YANIT]
```

Örnek çıktı:

```text
Öğrenci, Yazım Kuralları bölümünü tekrar etmeli ve özellikle Paragrafta Yardımcı Düşünce ile Sözcükte Çok Anlamlılık konularına yönelik ek çalışma yapmalıdır. Sınav puanı geçme puanının altında olduğu için konu tekrarı ve örnek soru çözümü önerilmektedir.
```

---

## 📄 Lisans

Bu proje eğitim ve bitirme projesi kapsamında geliştirilmiştir. Lisans bilgisi daha sonra eklenecektir.

---

<div align="center">

### ⭐ CoursVia

Eğitim yönetimi, mobil erişim ve yapay zekâ destekli öneri sistemlerini tek platformda birleştiren CoursVia projesi.

</div>
