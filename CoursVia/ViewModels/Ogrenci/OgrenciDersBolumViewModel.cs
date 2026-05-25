namespace CoursVia.ViewModels.Ogrenci;

public class OgrenciDersBolumViewModel
{
    public int BolumId { get; set; }

    public string BolumAdi { get; set; } = null!;

    public int SiraNo { get; set; }

    public List<OgrenciDersListeItemViewModel> Dersler { get; set; } = new();
}