namespace CoursVia.ViewModels.Mobile.Ai;

public class MobileAiOneriListeResponseDto
{
    public bool Basarili { get; set; }

    public string? Arama { get; set; }

    public string Siralama { get; set; } = "yeni";

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; }

    public int SayfaBoyutu { get; set; }

    public int ToplamSayfa { get; set; }

    public bool OncekiSayfaVarMi { get; set; }

    public bool SonrakiSayfaVarMi { get; set; }

    public List<MobileAiOneriListeItemDto> Oneriler { get; set; } = new();
}

public class MobileAiOneriListeItemDto
{
    public int OneriId { get; set; }

    public int OneriTipId { get; set; }

    public string OneriTipAdi { get; set; } = string.Empty;

    public int? KursId { get; set; }

    public string? KursAdi { get; set; }

    public string OneriMetni { get; set; } = string.Empty;

    public DateTime OlusturmaTarihi { get; set; }
}