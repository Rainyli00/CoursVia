namespace CoursVia.ViewModels.Ogrenci;

public class OgrenciDersMateryalViewModel
{
    public int DersMateryalId { get; set; }

    public string MateryalAdi { get; set; } = null!;

    public string? DosyaUrl { get; set; }

    public string? MateryalTipiAdi { get; set; }
}