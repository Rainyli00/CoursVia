namespace CoursVia.ViewModels.Ogrenci;

public class KursKesfetViewModel
{
    public string? Arama { get; set; }

    public int? KategoriId { get; set; }

    public string? Siralama { get; set; }

    public int Sayfa { get; set; } = 1;
    public int ToplamSayfa { get; set; }
    public int ToplamKayit { get; set; }
    public int SayfaBasinaKayit { get; set; } = 6;

    public bool OncekiSayfaVar => Sayfa > 1;
    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;

    public List<KursKesfetKategoriViewModel> Kategoriler { get; set; } = new();

    public List<KursKesfetListeItemViewModel> Kurslar { get; set; } = new();
}

public class KursKesfetKategoriViewModel
{
    public int KategoriId { get; set; }

    public string KategoriAdi { get; set; } = null!;
}