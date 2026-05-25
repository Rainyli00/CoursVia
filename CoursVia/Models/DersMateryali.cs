namespace CoursVia.Models;

public class DersMateryali
{
    public int MateryalId { get; set; }

    public int DersId { get; set; }
    public int MateryalTipId { get; set; }

    public string Baslik { get; set; } = null!;
    public string MateryalUrl { get; set; } = null!;

    public DateTime YuklenmeTarihi { get; set; }

    // Navigation Properties
    public Ders Ders { get; set; } = null!;
    public MateryalTipi MateryalTipi { get; set; } = null!;
}