namespace CoursVia.ViewModels.Bildirim;

public class BildirimlerViewModel
{
    public string Durum { get; set; } = "tum";

    public List<BildirimListeItemViewModel> Bildirimler { get; set; } = new();

    public int ToplamBildirimSayisi { get; set; }

    public int OkunmamisBildirimSayisi { get; set; }

    public int OkunmusBildirimSayisi { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; } = 1;

    public int ToplamSayfa { get; set; } = 1;

    public int SayfaBasinaKayit { get; set; } = 10;

    public bool OncekiSayfaVar => Sayfa > 1;

    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;
}

public class BildirimListeItemViewModel
{
    public int BildirimId { get; set; }

    public string BildirimTipiAdi { get; set; } = string.Empty;

    public string Baslik { get; set; } = string.Empty;

    public string Mesaj { get; set; } = string.Empty;

    public DateTime OlusturmaTarihi { get; set; }

    public bool OkunduMu { get; set; }
}