namespace CoursVia.Models;

public class KursKaydi
{
    public int KursKayitId { get; set; }

    public int KullaniciId { get; set; }
    public int KursId { get; set; }

    public DateTime KayitTarihi { get; set; }

    public bool TamamlandiMi { get; set; }
    public DateTime? TamamlanmaTarihi { get; set; }

    public bool AktifMi { get; set; }

    // Navigation Properties
    public Kullanici Kullanici { get; set; } = null!;
    public Kurs Kurs { get; set; } = null!;

    public ICollection<DersIlerlemesi> DersIlerlemeleri { get; set; } = new List<DersIlerlemesi>();
}