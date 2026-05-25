namespace CoursVia.ViewModels.Mobile.Ogrenci;

public class MobileOgrenciDashboardResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public int KayitliKursSayisi { get; set; }

    public int DevamEdenKursSayisi { get; set; }

    public int TamamlananKursSayisi { get; set; }

    public int OrtalamaIlerlemeYuzdesi { get; set; }

    public int SertifikaSayisi { get; set; }

    public int OkunmamisBildirimSayisi { get; set; }

    public List<MobileOgrenciDashboardKursResponse> SonKurslar { get; set; } = new();
}

public class MobileOgrenciDashboardKursResponse
{
    public int KursKayitId { get; set; }

    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string? KapakGorselUrl { get; set; }

    public string EgitmenAdSoyad { get; set; } = string.Empty;

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public bool GuncelleniyorMu { get; set; }

    public bool DevamEdilebilirMi { get; set; }

    public int ToplamDersSayisi { get; set; }

    public int TamamlananDersSayisi { get; set; }

    public int IlerlemeYuzdesi { get; set; }

    public bool KursTamamlandiMi { get; set; }
}

public class MobileOgrenciKurslarimResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public string? Arama { get; set; }

    public int? KategoriId { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; } = 1;

    public int SayfaBasinaKayit { get; set; } = 10;

    public int ToplamSayfa { get; set; } = 1;

    public bool OncekiSayfaVar => Sayfa > 1;

    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;

    public List<MobileOgrenciKategoriSecenekResponse> Kategoriler { get; set; } = new();

    public List<MobileOgrenciKursItemResponse> Kurslar { get; set; } = new();
}

public class MobileOgrenciKategoriSecenekResponse
{
    public int KategoriId { get; set; }

    public string KategoriAdi { get; set; } = string.Empty;

    public int KayitSayisi { get; set; }
}

public class MobileOgrenciKursItemResponse
{
    public int KursKayitId { get; set; }

    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string? KapakGorselUrl { get; set; }

    public string EgitmenAdSoyad { get; set; } = string.Empty;

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public bool GuncelleniyorMu { get; set; }

    public bool DevamEdilebilirMi { get; set; }

    public List<string> Kategoriler { get; set; } = new();

    public DateTime KayitTarihi { get; set; }

    public int ToplamDersSayisi { get; set; }

    public int TamamlananDersSayisi { get; set; }

    public int IlerlemeYuzdesi { get; set; }

    public bool KursTamamlandiMi { get; set; }

    public DateTime? TamamlanmaTarihi { get; set; }

    public bool DegerlendirmeVarMi { get; set; }

    public int? KendiPuan { get; set; }

    public string? KendiYorumMetni { get; set; }
}

public class MobileOgrenciKursDetayResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public int KursKayitId { get; set; }

    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public string? KapakGorselUrl { get; set; }

    public string EgitmenAdSoyad { get; set; } = string.Empty;

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public bool GuncelleniyorMu { get; set; }

    public bool DevamEdilebilirMi { get; set; }

    public List<string> Kategoriler { get; set; } = new();

    public DateTime KayitTarihi { get; set; }

    public int ToplamDersSayisi { get; set; }

    public int TamamlananDersSayisi { get; set; }

    public int IlerlemeYuzdesi { get; set; }

    public bool KursTamamlandiMi { get; set; }

    public bool DegerlendirmeVarMi { get; set; }

    public int? KendiPuan { get; set; }

    public string? KendiYorumMetni { get; set; }

    public List<MobileOgrenciBolumItemResponse> Bolumler { get; set; } = new();
}

public class MobileOgrenciBolumItemResponse
{
    public int BolumId { get; set; }

    public string BolumAdi { get; set; } = string.Empty;

    public int SiraNo { get; set; }

    public int ToplamDersSayisi { get; set; }

    public int TamamlananDersSayisi { get; set; }

    public int IlerlemeYuzdesi { get; set; }

    public List<MobileOgrenciDersItemResponse> Dersler { get; set; } = new();
}

public class MobileOgrenciDersItemResponse
{
    public int DersId { get; set; }

    public string DersAdi { get; set; } = string.Empty;

    public int SiraNo { get; set; }

    public bool TamamlandiMi { get; set; }

    public bool MateryalVarMi { get; set; }

    public int MateryalSayisi { get; set; }

    public List<MobileOgrenciDersMateryalItemResponse> Materyaller { get; set; } = new();
}

// Öğrenci kayıtlı olduğu kurs detayında ders altında gösterilecek materyal modeli.
// Kurslarım detayında öğrenci kayıtlı olduğu için materyal adı, tipi ve URL dönebilir.
public class MobileOgrenciDersMateryalItemResponse
{
    public int MateryalId { get; set; }

    public string Baslik { get; set; } = string.Empty;

    public string MateryalTipAdi { get; set; } = string.Empty;
}

public class MobileOgrenciDegerlendirRequest
{
    public int Puan { get; set; }

    public string? YorumMetni { get; set; }
}

public class MobileOgrenciIslemResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;
}

public class MobileOgrenciSinavlarimResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public string? Arama { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; }

    public int SayfaBasinaKayit { get; set; }

    public int ToplamSayfa { get; set; }

    public List<MobileOgrenciSinavItemResponse> Sinavlar { get; set; } = new();
}

public class MobileOgrenciSinavItemResponse
{
    public int KursKayitId { get; set; }

    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string? KapakGorselUrl { get; set; }

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public bool GuncelleniyorMu { get; set; }

    public bool DevamEdilebilirMi { get; set; }

    public int? SinavId { get; set; }

    public string? SinavAdi { get; set; }

    public int? GecmeNotu { get; set; }

    public bool DerslerTamamlandiMi { get; set; }

    public int ToplamDersSayisi { get; set; }

    public int TamamlananDersSayisi { get; set; }

    public int GirisSayisi { get; set; }

    public int KalanHak { get; set; }

    public int? SonPuan { get; set; }

    public bool? SonucGectiMi { get; set; }

    public DateTime? SonSinavTarihi { get; set; }

    public string DurumMetni { get; set; } = string.Empty;
}

public class MobileOgrenciSertifikalarimResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public string? Arama { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; } = 1;

    public int SayfaBasinaKayit { get; set; } = 10;

    public int ToplamSayfa { get; set; } = 1;

    public bool OncekiSayfaVar => Sayfa > 1;

    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;

    public List<MobileOgrenciSertifikaItemResponse> Sertifikalar { get; set; } = new();
}

public class MobileOgrenciSertifikaItemResponse
{
    public int SertifikaId { get; set; }

    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string? KapakGorselUrl { get; set; }

    public string EgitmenAdSoyad { get; set; } = string.Empty;

    public string SertifikaKodu { get; set; } = string.Empty;

    public DateTime VerilmeTarihi { get; set; }
}
