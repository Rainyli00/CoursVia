namespace CoursVia.ViewModels.Ogrenci;

public class OgrenciSinavaGirisViewModel
{
    public int SinavKatilimId { get; set; }

    public int KursId { get; set; }

    public int KursKayitId { get; set; }

    public int SinavId { get; set; }

    public string KursAdi { get; set; } = null!;

    public string SinavAdi { get; set; } = null!;

    public string? Aciklama { get; set; }

    public int GecmeNotu { get; set; }

    public int SureDakika { get; set; }

    public int SoruSayisi { get; set; }

    public DateTime BaslamaTarihi { get; set; }

    public List<OgrenciSinavSoruViewModel> Sorular { get; set; } = new();
}

public class OgrenciSinavSoruViewModel
{
    public int SoruId { get; set; }

    public int SiraNo { get; set; }

    public string SoruMetni { get; set; } = null!;

    public int? SeciliSecenekId { get; set; }

    public List<OgrenciSinavSecenekViewModel> Secenekler { get; set; } = new();
}

public class OgrenciSinavSecenekViewModel
{
    public int SecenekId { get; set; }

    public string SecenekMetni { get; set; } = null!;
}