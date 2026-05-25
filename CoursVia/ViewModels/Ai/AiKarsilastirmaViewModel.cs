using CoursVia.Services.Ai;

namespace CoursVia.ViewModels.Ai;

public class AiKarsilastirmaViewModel
{
    public AiIstekTipi IstekTipi { get; set; }

    public AiModelTipi SecilenModelTipi { get; set; }

    public int? KursId { get; set; }

    public int? SinavKatilimId { get; set; }

    public string Baslik { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public List<AiModelSonucViewModel> Sonuclar { get; set; } = new();
}