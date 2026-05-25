namespace CoursVia.Models;

public class KursDegerlendirmesi
{
    public int DegerlendirmeId { get; set; }

    public int KullaniciId { get; set; }
    public int KursId { get; set; }

    public byte Puan { get; set; }

    public string? YorumMetni { get; set; }

    public DateTime DegerlendirmeTarihi { get; set; }

    // Navigation Properties
    public Kullanici Kullanici { get; set; } = null!;
    public Kurs Kurs { get; set; } = null!;
}