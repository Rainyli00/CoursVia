namespace CoursVia.ViewModels.Mobile.Admin;

// Dashboard
// GET /api/mobile/admin/dashboard
public class MobileAdminDashboardResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public int ToplamKullaniciSayisi { get; set; }

    public int OnlineKullaniciSayisi { get; set; }

    public int BekleyenEgitmenBasvuruSayisi { get; set; }

    public int BekleyenKursOnaySayisi { get; set; }

    public int OkunmamisBildirimSayisi { get; set; }

    public List<MobileAdminLogItemResponse> SonLoglar { get; set; } = new();
}

// Kullanıcılar
// GET /api/mobile/admin/kullanicilar?arama=&rolId=&durumId=&sayfa=&sayfaBasinaKayit=
public class MobileAdminKullanicilarResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public string? Arama { get; set; }

    public int? RolId { get; set; }

    public int? DurumId { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; }

    public int SayfaBasinaKayit { get; set; }

    public int ToplamSayfa { get; set; }

    public List<MobileAdminSecenekResponse> Roller { get; set; } = new();

    public List<MobileAdminSecenekResponse> Durumlar { get; set; } = new();

    public List<MobileAdminKullaniciItemResponse> Kullanicilar { get; set; } = new();
}

public class MobileAdminKullaniciItemResponse
{
    public int KullaniciId { get; set; }

    public string AdSoyad { get; set; } = string.Empty;

    public string? ProfilFotoUrl { get; set; }

    public string Roller { get; set; } = string.Empty;

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public bool OnlineMi { get; set; }
}

// Kullanıcı detay
// GET /api/mobile/admin/kullanicilar/{kullaniciId}
public class MobileAdminKullaniciDetayResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public int KullaniciId { get; set; }

    public string AdSoyad { get; set; } = string.Empty;

    public string Eposta { get; set; } = string.Empty;

    public string? Telefon { get; set; }

    public string? ProfilFotoUrl { get; set; }

    public string Roller { get; set; } = string.Empty;

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public bool OnlineMi { get; set; }

    public DateTime KayitTarihi { get; set; }

    public DateTime? SonGirisTarihi { get; set; }

    public string? SonIpAdresi { get; set; }

    public int KayitliKursSayisi { get; set; }

    public int TamamlananKursSayisi { get; set; }

    public int SertifikaSayisi { get; set; }

    public int EgitmenKursSayisi { get; set; }

    // Kullanıcı eğitmense dolu gelir.
    public bool EgitmenProfiliVarMi { get; set; }

    public int? EgitmenProfilId { get; set; }

    public int? EgitmenDurumId { get; set; }

    public string? EgitmenDurumAdi { get; set; }

    public string? UzmanlikAlani { get; set; }

    public string? Biyografi { get; set; }

    public int? DeneyimYili { get; set; }

    public string? WebsiteUrl { get; set; }

    public List<string> Branslar { get; set; } = new();
}

// Kurslar
// GET /api/mobile/admin/kurslar?arama=&durumId=&kategoriId=&sirala=&sayfa=&sayfaBasinaKayit=
public class MobileAdminKurslarResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public string? Arama { get; set; }

    public int? DurumId { get; set; }

    public int? KategoriId { get; set; }

    // Desteklenen değerler:
    // guncel, eski, ad-az, ad-za, puan-yuksek, puan-dusuk, ogrenci-cok, ogrenci-az
    public string Sirala { get; set; } = "guncel";

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; }

    public int SayfaBasinaKayit { get; set; }

    public int ToplamSayfa { get; set; }

    public List<MobileAdminSecenekResponse> Durumlar { get; set; } = new();

    public List<MobileAdminSecenekResponse> Kategoriler { get; set; } = new();

    public List<MobileAdminKursItemResponse> Kurslar { get; set; } = new();
}

public class MobileAdminKursItemResponse
{
    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string? KapakGorselUrl { get; set; }

    public string EgitmenAdSoyad { get; set; } = string.Empty;

    public List<string> Kategoriler { get; set; } = new();

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public int OgrenciSayisi { get; set; }

    public int DersSayisi { get; set; }

    public int DegerlendirmeSayisi { get; set; }

    public double OrtalamaPuan { get; set; }

    public DateTime OlusturmaTarihi { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }
}

// Kurs detay
// GET /api/mobile/admin/kurslar/{kursId}
// Mobilde kurs onay/red işlemi yoktur, sadece görüntüleme vardır.
public class MobileAdminKursDetayResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public string? KapakGorselUrl { get; set; }

    public string EgitmenAdSoyad { get; set; } = string.Empty;

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public List<string> Kategoriler { get; set; } = new();

    public int OgrenciSayisi { get; set; }

    public int TamamlayanOgrenciSayisi { get; set; }

    public int BolumSayisi { get; set; }

    public int DersSayisi { get; set; }

    public int DegerlendirmeSayisi { get; set; }

    public double OrtalamaPuan { get; set; }

    public bool SinavVarMi { get; set; }

    public string? SinavAdi { get; set; }

    public int? SinavSoruSayisi { get; set; }

    public int? SinavSureDakika { get; set; }

    public int? SinavGecmeNotu { get; set; }

    public DateTime OlusturmaTarihi { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }

    public List<MobileAdminKursBolumItemResponse> Bolumler { get; set; } = new();
}

public class MobileAdminKursBolumItemResponse
{
    public int BolumId { get; set; }

    public string BolumAdi { get; set; } = string.Empty;

    public int SiraNo { get; set; }

    public int DersSayisi { get; set; }

    public List<MobileAdminKursDersItemResponse> Dersler { get; set; } = new();
}

public class MobileAdminKursDersItemResponse
{
    public int DersId { get; set; }

    public string DersAdi { get; set; } = string.Empty;

    public int SiraNo { get; set; }

    public bool AktifMi { get; set; }

    public bool MateryalVarMi { get; set; }

    public int MateryalSayisi { get; set; }

    public List<MobileAdminKursDersMateryalItemResponse> Materyaller { get; set; } = new();
}

public class MobileAdminKursDersMateryalItemResponse
{
    public int MateryalId { get; set; }

    public string Baslik { get; set; } = string.Empty;

    public string MateryalTipAdi { get; set; } = string.Empty;
}

// Eğitmen başvuruları
// GET /api/mobile/admin/egitmen-basvurulari?arama=&durumId=&sayfa=&sayfaBasinaKayit=
public class MobileAdminEgitmenBasvurulariResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public string? Arama { get; set; }

    public int? DurumId { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; }

    public int SayfaBasinaKayit { get; set; }

    public int ToplamSayfa { get; set; }

    public List<MobileAdminSecenekResponse> Durumlar { get; set; } = new();

    public List<MobileAdminEgitmenBasvuruItemResponse> Basvurular { get; set; } = new();
}

public class MobileAdminEgitmenBasvuruItemResponse
{
    public int EgitmenProfilId { get; set; }

    public int KullaniciId { get; set; }

    public string AdSoyad { get; set; } = string.Empty;

    public string Eposta { get; set; } = string.Empty;

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public DateTime? SonIslemTarihi { get; set; }
}

// Eğitmen başvuru detay
// GET /api/mobile/admin/egitmen-basvurulari/{egitmenProfilId}
public class MobileAdminEgitmenBasvuruDetayResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public int EgitmenProfilId { get; set; }

    public int KullaniciId { get; set; }

    public string AdSoyad { get; set; } = string.Empty;

    public string Eposta { get; set; } = string.Empty;

    public string? ProfilFotoUrl { get; set; }

    public string? Biyografi { get; set; }

    public string? UzmanlikAlani { get; set; }

    public int? DeneyimYili { get; set; }

    public string? WebsiteUrl { get; set; }

    public List<string> Branslar { get; set; } = new();

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public DateTime? SonIslemTarihi { get; set; }

    public string? Aciklama { get; set; }
}

// Eğitmen başvuru karar request
// POST /api/mobile/admin/egitmen-basvurulari/{egitmenProfilId}/onayla
// POST /api/mobile/admin/egitmen-basvurulari/{egitmenProfilId}/reddet
public class MobileAdminEgitmenBasvuruKararRequest
{
    public string? Aciklama { get; set; }
}

// Admin logları
// GET /api/mobile/admin/loglar?arama=&kategori=&sirala=&sayfa=&sayfaBasinaKayit=
public class MobileAdminLoglarResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public string? Arama { get; set; }

    public string Kategori { get; set; } = "tum";

    // yeni, eski
    public string Sirala { get; set; } = "yeni";

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; }

    public int SayfaBasinaKayit { get; set; }

    public int ToplamSayfa { get; set; }

    public List<MobileAdminLogKategoriResponse> Kategoriler { get; set; } = new();

    public List<MobileAdminLogItemResponse> Loglar { get; set; } = new();
}

public class MobileAdminLogItemResponse
{
    public int AdminLogId { get; set; }

    public string AdminAdSoyad { get; set; } = string.Empty;

    public string IslemTipi { get; set; } = string.Empty;

    public string Aciklama { get; set; } = string.Empty;

    public string? IpAdresi { get; set; }

    public DateTime IslemTarihi { get; set; }
}

public class MobileAdminLogKategoriResponse
{
    public string Kategori { get; set; } = string.Empty;

    public string KategoriAdi { get; set; } = string.Empty;
}

// Ortak seçenek modeli
public class MobileAdminSecenekResponse
{
    public int Id { get; set; }

    public string Ad { get; set; } = string.Empty;
}

// Ortak işlem response
public class MobileAdminIslemResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;
}

// Kullanıcının kayıtlı olduğu kurslar
// GET /api/mobile/admin/kullanicilar/{kullaniciId}/kurslar?arama=&sayfa=&sayfaBasinaKayit=
public class MobileAdminKullaniciKurslarResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public int KullaniciId { get; set; }

    public string AdSoyad { get; set; } = string.Empty;

    public string? Arama { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; }

    public int SayfaBasinaKayit { get; set; }

    public int ToplamSayfa { get; set; }

    public List<MobileAdminKullaniciKursItemResponse> Kurslar { get; set; } = new();
}

public class MobileAdminKullaniciKursItemResponse
{
    public int KursKayitId { get; set; }

    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string EgitmenAdSoyad { get; set; } = string.Empty;

    public DateTime KayitTarihi { get; set; }

    public bool AktifMi { get; set; }

    public bool TamamlandiMi { get; set; }
}