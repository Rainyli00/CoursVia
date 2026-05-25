using CoursVia.Services.Ai;

namespace CoursVia.ViewModels.Ai;

public class OgrenciAiOnerilerViewModel
{
    public List<OgrenciAiSinavSecimItemViewModel> TamamlananSinavlar { get; set; } = new();

    public List<EgitmenGecmisOneriViewModel> GecmisOneriler { get; set; } = new();
}

public class OgrenciAiSinavSecimItemViewModel
{
    public int SinavKatilimId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string SinavAdi { get; set; } = string.Empty;

    public int AlinanPuan { get; set; }

    public int GecmeNotu { get; set; }

    public DateTime BitisTarihi { get; set; }
}
