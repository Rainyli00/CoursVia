namespace CoursVia.Models;

public class Favori
{
    public int FavoriId { get; set; }

    public int KullaniciId { get; set; }
    public int KursId { get; set; }

    public DateTime EklenmeTarihi { get; set; }

    // Navigation Properties
    public Kullanici Kullanici { get; set; } = null!;
    public Kurs Kurs { get; set; } = null!;
}