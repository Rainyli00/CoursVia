namespace CoursVia.Models;

public class Sertifika
{
    public int SertifikaId { get; set; }

    public int KullaniciId { get; set; }
    public int KursId { get; set; }

    public string SertifikaKodu { get; set; } = null!;

    public DateTime VerilmeTarihi { get; set; }

    // Navigation Properties
    public Kullanici Kullanici { get; set; } = null!;
    public Kurs Kurs { get; set; } = null!;
}