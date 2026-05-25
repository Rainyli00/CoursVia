namespace CoursVia.Models;

public class Soru
{
    public int SoruId { get; set; }

    public int SinavId { get; set; }

    public string SoruMetni { get; set; } = null!;
    public bool AktifMi { get; set; } = true;

    // Navigation Properties
    public Sinav Sinav { get; set; } = null!;

    public ICollection<SoruDersi> SoruDersleri { get; set; } = new List<SoruDersi>();
    public ICollection<SoruSecenegi> SoruSecenekleri { get; set; } = new List<SoruSecenegi>();
}
