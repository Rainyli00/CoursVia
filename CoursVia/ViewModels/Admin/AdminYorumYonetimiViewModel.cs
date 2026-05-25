namespace CoursVia.ViewModels.Admin;

public class AdminYorumYonetimiViewModel
{
    public string? Arama { get; set; }

    public int? Puan { get; set; }

    public string Siralama { get; set; } = "yeni";

    public List<AdminYorumListeItemViewModel> Yorumlar { get; set; } = new();

    public int ToplamYorumSayisi { get; set; }

    public int BugunkuYorumSayisi { get; set; }

    public double OrtalamaPuan { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; } = 1;

    public int ToplamSayfa { get; set; } = 1;

    public int SayfaBasinaKayit { get; set; } = 12;

    public bool OncekiSayfaVar => Sayfa > 1;

    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;
}

public class AdminYorumListeItemViewModel
{
    public int DegerlendirmeId { get; set; }

    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string EgitmenAdSoyad { get; set; } = string.Empty;

    public string EgitmenEposta { get; set; } = string.Empty;

    public string OgrenciAdSoyad { get; set; } = string.Empty;

    public string OgrenciEposta { get; set; } = string.Empty;

    public int Puan { get; set; }

    public string? YorumMetni { get; set; }

    public DateTime DegerlendirmeTarihi { get; set; }
}