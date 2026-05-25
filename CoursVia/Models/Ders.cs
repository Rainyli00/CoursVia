namespace CoursVia.Models;

public class Ders
{
    public int DersId { get; set; }

    public int KursId { get; set; }
    public int BolumId { get; set; }

    public string DersAdi { get; set; } = null!;
    public string? Aciklama { get; set; }
    public string VideoUrl { get; set; } = null!;

    public int SiraNo { get; set; }
    public DateTime OlusturmaTarihi { get; set; }

    public bool AktifMi { get; set; } = true;
    public bool SistemDersiMi { get; set; }

    // Navigation Properties
    public Kurs Kurs { get; set; } = null!;
    public Bolum Bolum { get; set; } = null!;

    public ICollection<DersMateryali> DersMateryalleri { get; set; } = new List<DersMateryali>();
}