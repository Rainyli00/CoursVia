namespace CoursVia.ViewModels.Ai;

public class AiOgrenciCalismaVerisi
{
    public int SinavKatilimId { get; set; }

    public int KursId { get; set; }

    public string KursAdi { get; set; } = null!;

    public int SinavPuani { get; set; }

    public int GecmePuani { get; set; }

    public string YanlislarinYogunlastigiBolum { get; set; } = null!;

    public List<AiYanlisDersVerisi> YanlisYapilanDersler { get; set; } = new();
}