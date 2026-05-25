namespace CoursVia.Models;

public class OgrenciCevabi
{
    public int OgrenciCevapId { get; set; }

    public int SinavKatilimId { get; set; }
    public int SoruId { get; set; }
    public int? SecenekId { get; set; }

    public bool DogruMu { get; set; }

    public DateTime VerilmeTarihi { get; set; }

    // Navigation Properties
    public SinavKatilimi SinavKatilimi { get; set; } = null!;
    public Soru Soru { get; set; } = null!;
    public SoruSecenegi? SoruSecenegi { get; set; }
}