namespace CoursVia.Models;

public class DersIlerlemesi
{
    public int DersIlerlemeId { get; set; }

    public int KursKayitId { get; set; }
    public int DersId { get; set; }

    public bool TamamlandiMi { get; set; }

    // Navigation Properties
    public KursKaydi KursKaydi { get; set; } = null!;
    public Ders Ders { get; set; } = null!;
}