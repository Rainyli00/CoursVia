namespace CoursVia.Models;

public class SifreSifirlama
{
    public int SifreSifirlamaId { get; set; }

    public int KullaniciId { get; set; }

    public string Kod { get; set; } = null!;

    public DateTime OlusturmaTarihi { get; set; }

    public DateTime GecerlilikTarihi { get; set; }

    public bool KullanildiMi { get; set; }

    // Navigation Property
    public Kullanici Kullanici { get; set; } = null!;
}