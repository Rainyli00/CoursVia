namespace CoursVia.ViewModels.Ogrenci;

public class OgrenciKurslarimViewModel
{
    public int ToplamKursSayisi { get; set; }

    public int DevamEdenKursSayisi { get; set; }

    public int TamamlananKursSayisi { get; set; }

    public int OrtalamaIlerlemeYuzdesi { get; set; }
    public string? Arama { get; set; }
    public string Durum { get; set; } = "tum";
    public int? KategoriId { get; set; }
    public string Siralama { get; set; } = "guncel";

    public int Sayfa { get; set; } = 1;
    public int ToplamSayfa { get; set; }
    public int ToplamKayit { get; set; }
    public int SayfaBasinaKayit { get; set; } = 5;

    public bool OncekiSayfaVar => Sayfa > 1;
    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;

    public List<OgrenciKursOzetViewModel> Kurslar { get; set; } = new();
    public List<OgrenciKursKategoriViewModel> Kategoriler { get; set; } = new();
}

public class OgrenciKursKategoriViewModel
{
    public int KategoriId { get; set; }

    public string KategoriAdi { get; set; } = string.Empty;
}
