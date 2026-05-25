namespace CoursVia.Models;

public class SinavKatilimi
{
    public int SinavKatilimId { get; set; }

    public int KursKayitId { get; set; }
    public int SinavId { get; set; }

    public DateTime BaslamaTarihi { get; set; }
    public DateTime? BitisTarihi { get; set; }

    public int? AlinanPuan { get; set; }
    public bool? GectiMi { get; set; }

    // Navigation Properties
    public KursKaydi KursKaydi { get; set; } = null!;
    public Sinav Sinav { get; set; } = null!;

    public ICollection<OgrenciCevabi> OgrenciCevaplari { get; set; } = new List<OgrenciCevabi>();
}