namespace CoursVia.ViewModels.Egitmen;

public class SoruHavuzuViewModel
{
    public int KursId { get; set; }
    public int SinavId { get; set; }

    public string KursAdi { get; set; } = null!;
    public string SinavAdi { get; set; } = null!;

    public int GecmeNotu { get; set; }
    public int SureDakika { get; set; }
    public int SoruSayisi { get; set; }
    public int HavuzdakiSoruSayisi { get; set; }

    public bool KursDuzenlenebilirMi { get; set; }

    public string? Arama { get; set; }
    public int? DersId { get; set; }

    public int Sayfa { get; set; } = 1;
    public int SayfaBoyutu { get; set; } = 10;
    public int ToplamSoruSayisi { get; set; }

    public int ToplamSayfa => ToplamSoruSayisi == 0
        ? 1
        : (int)Math.Ceiling(ToplamSoruSayisi / (double)SayfaBoyutu);

    public bool FiltreVarMi => !string.IsNullOrWhiteSpace(Arama) || DersId.HasValue;

    public List<SoruDersSecimViewModel> Dersler { get; set; } = new();

    public List<SoruHavuzuSoruViewModel> Sorular { get; set; } = new();
}

public class SoruHavuzuSoruViewModel
{
    public int SoruId { get; set; }

    public string SoruMetni { get; set; } = null!;

    public int SecenekSayisi { get; set; }
    public int DogruSecenekSayisi { get; set; }

    public bool DersBaglantilariGecerliMi { get; set; }
    public bool AktifMi { get; set; }

    public List<string> DersAdlari { get; set; } = new();
}
