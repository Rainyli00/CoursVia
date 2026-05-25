namespace CoursVia.Models;

public class Bildirim
{
    public int BildirimId { get; set; }

    public int KullaniciId { get; set; }
    public int BildirimTipId { get; set; }

    public string Baslik { get; set; } = null!;
    public string Mesaj { get; set; } = null!;

    public DateTime OlusturmaTarihi { get; set; }

    public bool OkunduMu { get; set; }

    // Navigation Properties
    public Kullanici Kullanici { get; set; } = null!;
    public BildirimTipi BildirimTipi { get; set; } = null!;
}