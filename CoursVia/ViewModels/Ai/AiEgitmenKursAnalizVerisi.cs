namespace CoursVia.ViewModels.Ai;

public class AiEgitmenKursAnalizVerisi
{
    public int KursId { get; set; }

    public string KursAdi { get; set; } = null!;

    public int ToplamOgrenciSayisi { get; set; }

    public decimal OrtalamaPuan { get; set; }

    public decimal GenelTamamlanmaOrani { get; set; }

    public string ZorlanilanBolum { get; set; } = null!;

    public List<AiZorlanilanDersVerisi> ZorlanilanDersler { get; set; } = new();
}