namespace CoursVia.ViewModels.Ogrenci;

public class KursKesfetListeItemViewModel
{
    public int KursId { get; set; }

    public string KursAdi { get; set; } = null!;

    public string? Aciklama { get; set; }

    public string? KapakGorselUrl { get; set; }

    public string EgitmenAdSoyad { get; set; } = null!;

    public List<string> Kategoriler { get; set; } = new();

    public double OrtalamaPuan { get; set; }

    public int DegerlendirmeSayisi { get; set; }

    public int OgrenciSayisi { get; set; }

    public int BolumSayisi { get; set; }

    public int DersSayisi { get; set; }

    public bool KayitliMi { get; set; }
    public bool KendiKursuMu { get; set; }

    public bool FavorideMi { get; set; }


}
