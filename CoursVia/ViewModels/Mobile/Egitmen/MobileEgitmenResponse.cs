namespace CoursVia.ViewModels.Mobile.Egitmen;

// Eğitmen mobil dashboard endpointinden dönen sade response.
// GET /api/mobile/egitmen/dashboard
public class MobileEgitmenDashboardResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public int ToplamKursSayisi { get; set; }

    public int YayindakiKursSayisi { get; set; }

    public int ToplamOgrenciSayisi { get; set; }

    public int OkunmamisBildirimSayisi { get; set; }

    public List<MobileEgitmenDashboardKursItemResponse> SonKurslar { get; set; } = new();
}

// Dashboard son kurslar alanında kullanılacak sade kurs modeli.
// Sadece mobil dashboard ekranında gösterilen bilgiler döner.
public class MobileEgitmenDashboardKursItemResponse
{
    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string DurumAdi { get; set; } = string.Empty;

    public int OgrenciSayisi { get; set; }

    public int DersSayisi { get; set; }
}

// Eğitmen kurslarım endpointinden dönen response.
// GET /api/mobile/egitmen/kurslarim?arama=&durumId=&kategoriId=&sirala=&sayfa=&sayfaBasinaKayit=
public class MobileEgitmenKurslarimResponse
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

    public List<MobileEgitmenKategoriSecenekResponse> Kategoriler { get; set; } = new();

    public List<MobileEgitmenKursItemResponse> Kurslar { get; set; } = new();
}

// Eğitmen kurslarım kategori filtre seçeneği modeli.
// Sadece eğitmenin kendi kurslarında kullanılan kategoriler döner.
public class MobileEgitmenKategoriSecenekResponse
{
    public int KategoriId { get; set; }

    public string KategoriAdi { get; set; } = string.Empty;

    public int KayitSayisi { get; set; }
}

// Eğitmenin kurs listelerinde kullanılan kurs kartı modeli.
public class MobileEgitmenKursItemResponse
{
    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string? KapakGorselUrl { get; set; }

    public List<string> Kategoriler { get; set; } = new();

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public int OgrenciSayisi { get; set; }

    public int TamamlayanOgrenciSayisi { get; set; }

    public int DersSayisi { get; set; }

    public int DegerlendirmeSayisi { get; set; }

    public double OrtalamaPuan { get; set; }

    public DateTime OlusturmaTarihi { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }
}

// Eğitmen kurs detay endpointinden dönen response.
// GET /api/mobile/egitmen/kurslarim/{kursId}
public class MobileEgitmenKursDetayResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public string? KapakGorselUrl { get; set; }

    public List<string> Kategoriler { get; set; } = new();

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

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

    public List<MobileEgitmenBolumItemResponse> Bolumler { get; set; } = new();

    public List<MobileEgitmenKursYorumItemResponse> SonYorumlar { get; set; } = new();
}

// Eğitmen kurs detayında gösterilecek son yorum modeli.
// Detay ekranında sadece son 5 yorum için kullanılır.
public class MobileEgitmenKursYorumItemResponse
{
    public int DegerlendirmeId { get; set; }

    public int KullaniciId { get; set; }

    public string OgrenciAdSoyad { get; set; } = string.Empty;

    public int Puan { get; set; }

    public string YorumMetni { get; set; } = string.Empty;

    public DateTime DegerlendirmeTarihi { get; set; }
}

// Eğitmen kurs detayında gösterilecek bölüm modeli.
public class MobileEgitmenBolumItemResponse
{
    public int BolumId { get; set; }

    public string BolumAdi { get; set; } = string.Empty;

    public int SiraNo { get; set; }

    public int DersSayisi { get; set; }

    public List<MobileEgitmenDersItemResponse> Dersler { get; set; } = new();
}

// Eğitmen kurs detayında bölüm altındaki ders modeli.
// Ders altında materyal adı ve tipi döner. Materyal URL dönülmez.
public class MobileEgitmenDersItemResponse
{
    public int DersId { get; set; }

    public string DersAdi { get; set; } = string.Empty;

    public int SiraNo { get; set; }

    public bool AktifMi { get; set; }

    public bool MateryalVarMi { get; set; }

    public int MateryalSayisi { get; set; }

    public List<MobileEgitmenDersMateryalItemResponse> Materyaller { get; set; } = new();
}

// Eğitmen kurs detayında ders altında gösterilecek materyal modeli.
// Sadece materyal adı ve tipi döner.
public class MobileEgitmenDersMateryalItemResponse
{
    public int MateryalId { get; set; }

    public string Baslik { get; set; } = string.Empty;

    public string MateryalTipAdi { get; set; } = string.Empty;
}

// Eğitmen öğrencilerim endpointinden dönen response.
// GET /api/mobile/egitmen/ogrencilerim?arama=&kursId=&sayfa=&sayfaBasinaKayit=
public class MobileEgitmenOgrencilerimResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public string? Arama { get; set; }

    public int? KursId { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; }

    public int SayfaBasinaKayit { get; set; }

    public int ToplamSayfa { get; set; }

    public List<MobileEgitmenOgrenciItemResponse> Ogrenciler { get; set; } = new();
}

// Eğitmen öğrenci listelerinde kullanılan benzersiz öğrenci kartı modeli.
// Aynı öğrenci birden fazla kursa kayıtlı olsa bile tek kayıt döner.
public class MobileEgitmenOgrenciItemResponse
{
    public int KullaniciId { get; set; }

    public string OgrenciAdSoyad { get; set; } = string.Empty;

    public string? ProfilFotoUrl { get; set; }

    public int KayitliKursSayisi { get; set; }
}

// Eğitmen mobil basit işlem response modeli.
// Taslağa alma gibi POST işlemlerinde kullanılır.
public class MobileEgitmenIslemResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;
}