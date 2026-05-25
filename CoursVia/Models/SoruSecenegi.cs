namespace CoursVia.Models;

public class SoruSecenegi
{
    public int SecenekId { get; set; }

    public int SoruId { get; set; }

    public string SecenekMetni { get; set; } = null!;
    public bool DogruMu { get; set; }
    public bool AktifMi { get; set; } = true;

    // Navigation Properties
    public Soru Soru { get; set; } = null!;
}
