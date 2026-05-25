namespace CoursVia.Models;

public class Oneri
{
    public int OneriId { get; set; }

    public int KullaniciId { get; set; }

    public int OneriTipId { get; set; }

    public int? KursId { get; set; }

    public string OneriMetni { get; set; } = null!;

    public DateTime OlusturmaTarihi { get; set; }

    // Navigation Properties
    public Kullanici Kullanici { get; set; } = null!;

    public OneriTipi OneriTipi { get; set; } = null!;

    public Kurs? Kurs { get; set; }
}