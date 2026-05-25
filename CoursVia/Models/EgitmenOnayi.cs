namespace CoursVia.Models;

public class EgitmenOnayi
{
    public int EgitmenOnayId { get; set; }

    public int KullaniciId { get; set; }
    public int AdminId { get; set; }
    public int DurumId { get; set; }

    public string? Aciklama { get; set; }

    public DateTime IslemTarihi { get; set; }

    // Navigation Properties
    public Kullanici Kullanici { get; set; } = null!;
    public Kullanici Admin { get; set; } = null!;
    public Durum Durum { get; set; } = null!;
}