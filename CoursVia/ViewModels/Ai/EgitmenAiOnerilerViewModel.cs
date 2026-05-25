using CoursVia.Services.Ai;

namespace CoursVia.ViewModels.Ai;

public class EgitmenAiOnerilerViewModel
{
    public AiModelTipi SeciliModelTipi { get; set; } = AiModelTipi.Gemini;

    public List<EgitmenAiKursSecimItemViewModel> Kurslar { get; set; } = new();

    public List<EgitmenGecmisOneriViewModel> GecmisOneriler { get; set; } = new();
}

public class EgitmenAiKursSecimItemViewModel
{
    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string DurumAdi { get; set; } = string.Empty;

    public int OgrenciSayisi { get; set; }

    public decimal OrtalamaPuan { get; set; }

    public bool SinavVarMi { get; set; }

    public DateTime OlusturmaTarihi { get; set; }
}

public class EgitmenGecmisOneriViewModel
{
    public int OneriId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string OneriTipAdi { get; set; } = string.Empty;

    public string OneriMetni { get; set; } = string.Empty;

    public DateTime OlusturmaTarihi { get; set; }
}