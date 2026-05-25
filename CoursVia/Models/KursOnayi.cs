namespace CoursVia.Models;

public class KursOnayi
{
    public int KursOnayId { get; set; }

    public int KursId { get; set; }
    public int AdminId { get; set; }
    public int DurumId { get; set; }

    public string? Aciklama { get; set; }

    public DateTime IslemTarihi { get; set; }

    // Navigation Properties
    public Kurs Kurs { get; set; } = null!;
    public Kullanici Admin { get; set; } = null!;
    public Durum Durum { get; set; } = null!;
}