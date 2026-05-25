namespace CoursVia.ViewModels.Ai;

public class AiZorlanilanDersVerisi
{
    public int DersId { get; set; }

    public string DersAdi { get; set; } = null!;

    public string BolumAdi { get; set; } = null!;

    public int YanlisSayisi { get; set; }

    public decimal YanlisOrani { get; set; }
}