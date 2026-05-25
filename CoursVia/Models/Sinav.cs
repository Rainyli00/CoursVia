namespace CoursVia.Models;

public class Sinav
{
    public int SinavId { get; set; }

    public int KursId { get; set; }

    public string SinavAdi { get; set; } = null!;
    public string? Aciklama { get; set; }

    public int GecmeNotu { get; set; }
    public int SureDakika { get; set; }

    public int SoruSayisi { get; set; }

    public DateTime OlusturmaTarihi { get; set; }

    // Navigation Properties
    public Kurs Kurs { get; set; } = null!;

    public ICollection<Soru> Sorular { get; set; } = new List<Soru>();
    public ICollection<SinavKatilimi> SinavKatilimlari { get; set; } = new List<SinavKatilimi>();
}