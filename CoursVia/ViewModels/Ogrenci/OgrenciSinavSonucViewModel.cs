namespace CoursVia.ViewModels.Ogrenci;

public class OgrenciSinavSonucViewModel
{
    public int SinavKatilimId { get; set; }

    public int KursId { get; set; }

    public int KursKayitId { get; set; }

    public string KursAdi { get; set; } = null!;

    public string SinavAdi { get; set; } = null!;

    public int GecmeNotu { get; set; }

    public int AlinanPuan { get; set; }

    public bool GectiMi { get; set; }

    public int DogruSayisi { get; set; }

    public int ToplamSoruSayisi { get; set; }

    public int GirisSayisi { get; set; }

    public int KalanHak { get; set; }

    public bool KursTamamlandiMi { get; set; }

    public bool SertifikaOlustuMu { get; set; }

    public string? SertifikaKodu { get; set; }
}