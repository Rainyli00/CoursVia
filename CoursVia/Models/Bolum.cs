namespace CoursVia.Models;

public class Bolum
{
    public int BolumId { get; set; }

    public int KursId { get; set; }

    public string BolumAdi { get; set; } = null!;
    public int SiraNo { get; set; }

    // Navigation Properties
    public Kurs Kurs { get; set; } = null!;

    public ICollection<Ders> Dersler { get; set; } = new List<Ders>();
}