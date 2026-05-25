namespace CoursVia.ViewModels.Mobile.Ogrenci;

public class MobileOgrenciKesfetResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public string? Arama { get; set; }

    public int? KategoriId { get; set; }

    // Desteklenen değerler:
    // guncel, puan-yuksek, populer, degerlendirme-cok, ad-az, ad-za
    public string Sirala { get; set; } = "guncel";

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; }

    public int SayfaBasinaKayit { get; set; }

    public int ToplamSayfa { get; set; }

    public List<MobileOgrenciKategoriSecenekResponse> Kategoriler { get; set; } = new();

    public List<MobileOgrenciKesfetKursItemResponse> Kurslar { get; set; } = new();
}

public class MobileOgrenciKesfetKursItemResponse
{
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

    public int ToplamBolumSayisi { get; set; }

    public int ToplamDersSayisi { get; set; }

    public int KayitliOgrenciSayisi { get; set; }

    public double OrtalamaPuan { get; set; }

    public int DegerlendirmeSayisi { get; set; }

    public bool KayitliMi { get; set; }

    public bool KayitOlabilirMi { get; set; }

    public bool KendiKursuMu { get; set; }
}

public class MobileOgrenciKesfetDetayResponse
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

    public bool GuncelleniyorMu { get; set; }

    public bool DevamEdilebilirMi { get; set; }

    public List<string> Kategoriler { get; set; } = new();

    public int ToplamBolumSayisi { get; set; }

    public int ToplamDersSayisi { get; set; }

    public int KayitliOgrenciSayisi { get; set; }

    public double OrtalamaPuan { get; set; }

    public int DegerlendirmeSayisi { get; set; }

    public bool SinavVarMi { get; set; }

    public int? GecmeNotu { get; set; }

    public bool KayitliMi { get; set; }

    public bool KayitOlabilirMi { get; set; }

    public bool KendiKursuMu { get; set; }

    public List<MobileOgrenciKesfetBolumResponse> Bolumler { get; set; } = new();
}

public class MobileOgrenciKesfetBolumResponse
{
    public int BolumId { get; set; }

    public string BolumAdi { get; set; } = string.Empty;

    public int SiraNo { get; set; }

    public int DersSayisi { get; set; }

    public List<MobileOgrenciKesfetDersResponse> Dersler { get; set; } = new();
}

public class MobileOgrenciKesfetDersResponse
{
    public int DersId { get; set; }

    public string DersAdi { get; set; } = string.Empty;

    public int SiraNo { get; set; }

    public bool MateryalVarMi { get; set; }

    public int MateryalSayisi { get; set; }
}
