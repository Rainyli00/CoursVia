namespace CoursVia.ViewModels.Mobile.Ortak;

public class MobileBildirimlerResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public string Durum { get; set; } = "tum";

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; }

    public int SayfaBasinaKayit { get; set; }

    public int ToplamSayfa { get; set; }

    public int OkunmamisBildirimSayisi { get; set; }

    public List<MobileBildirimItemResponse> Bildirimler { get; set; } = new();
}

public class MobileBildirimItemResponse
{
    public int BildirimId { get; set; }

    public int BildirimTipId { get; set; }

    public string BildirimTipAdi { get; set; } = string.Empty;

    public string Baslik { get; set; } = string.Empty;

    public string Mesaj { get; set; } = string.Empty;

    public bool OkunduMu { get; set; }

    public DateTime OlusturmaTarihi { get; set; }
}

public class MobileBildirimOzetResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public int ToplamBildirimSayisi { get; set; }

    public int OkunmamisBildirimSayisi { get; set; }
}

public class MobileBildirimIslemResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public int OkunmamisBildirimSayisi { get; set; }
}