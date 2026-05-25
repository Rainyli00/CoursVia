namespace CoursVia.ViewModels.Admin;

public class AdminLogViewModel
{
    public string? Arama { get; set; }

    public string Kategori { get; set; } = "tum";

    public string Siralama { get; set; } = "yeni";

    public DateTime? BaslangicTarihi { get; set; }

    public DateTime? BitisTarihi { get; set; }

    public List<AdminLogListeItemViewModel> Loglar { get; set; } = new();

    public int ToplamLogSayisi { get; set; }

    public int BugunkuLogSayisi { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; } = 1;

    public int ToplamSayfa { get; set; } = 1;

    public int SayfaBasinaKayit { get; set; } = 12;

    public bool OncekiSayfaVar => Sayfa > 1;

    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;
}

public class AdminLogListeItemViewModel
{
    public int AdminLogId { get; set; }

    public string AdminAdSoyad { get; set; } = string.Empty;

    public string AdminEposta { get; set; } = string.Empty;

    public string IslemTipiAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public string? IpAdresi { get; set; }

    public DateTime IslemTarihi { get; set; }
}