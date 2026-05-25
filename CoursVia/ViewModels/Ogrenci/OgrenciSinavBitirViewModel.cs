namespace CoursVia.ViewModels.Ogrenci;

public class OgrenciSinavBitirViewModel
{
    public int SinavKatilimId { get; set; }

    public List<OgrenciSinavCevapViewModel> Cevaplar { get; set; } = new();
}

public class OgrenciSinavCevapViewModel
{
    public int SoruId { get; set; }

    public int? SecenekId { get; set; }
}