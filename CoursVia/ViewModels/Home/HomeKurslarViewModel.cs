namespace CoursVia.ViewModels.Home;

public class HomeKurslarViewModel
{
    public string? Arama { get; set; }

    public int? KategoriId { get; set; }

    public string Siralama { get; set; } = "oneCikan";

    public int Sayfa { get; set; } = 1;

    public int ToplamSayfa { get; set; } = 1;

    public int ToplamKayit { get; set; }

    public int SayfaBasinaKayit { get; set; } = 12;

    public bool OncekiSayfaVar => Sayfa > 1;

    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;

    public List<HomeKursListeItemViewModel> Kurslar { get; set; } = new();

    public List<HomeKategoriViewModel> Kategoriler { get; set; } = new();
}

public class HomeKursListeItemViewModel
{
    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public string? KapakGorselUrl { get; set; }

    public string EgitmenAdSoyad { get; set; } = string.Empty;

    public DateTime OlusturmaTarihi { get; set; }

    public double OrtalamaPuan { get; set; }

    public int DegerlendirmeSayisi { get; set; }

    public int YorumSayisi { get; set; }

    public int OgrenciSayisi { get; set; }

    public int BolumSayisi { get; set; }

    public int DersSayisi { get; set; }

    public List<string> Kategoriler { get; set; } = new();
}
