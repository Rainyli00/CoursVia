namespace CoursVia.ViewModels.Home;

public class HomeIndexViewModel
{
    public int ToplamOnayliKursSayisi { get; set; }

    public int ToplamEgitmenSayisi { get; set; }

    public int ToplamOgrenciSayisi { get; set; }

    public int ToplamKategoriSayisi { get; set; }

    public List<HomeKursKartiViewModel> OneCikanKurslar { get; set; } = new();

    public List<HomeKursKartiViewModel> YeniKurslar { get; set; } = new();

    public List<HomeKategoriViewModel> Kategoriler { get; set; } = new();
}

public class HomeKursKartiViewModel
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

    public List<string> Kategoriler { get; set; } = new();
}

public class HomeKategoriViewModel
{
    public int KategoriId { get; set; }

    public string KategoriAdi { get; set; } = string.Empty;

    public int KursSayisi { get; set; }
}
