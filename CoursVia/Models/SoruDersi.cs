namespace CoursVia.Models;

public class SoruDersi
{
    public int SoruDersId { get; set; }

    public int SoruId { get; set; }
    public int DersId { get; set; }

    // Navigation Properties
    public Soru Soru { get; set; } = null!;
    public Ders Ders { get; set; } = null!;
}