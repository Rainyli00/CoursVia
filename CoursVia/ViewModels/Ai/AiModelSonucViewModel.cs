using CoursVia.Services.Ai;

namespace CoursVia.ViewModels.Ai;

public class AiModelSonucViewModel
{
    public AiModelTipi ModelTipi { get; set; }

    public string ModelAdi { get; set; } = string.Empty;

    public bool BasariliMi { get; set; }

    public string? Cikti { get; set; }

    public string? HataMesaji { get; set; }

    public long SureMs { get; set; }

    public bool GuvenlikFiltresiUygulandiMi { get; set; }

    public string SureText
    {
        get
        {
            if (SureMs <= 0)
                return "-";

            if (SureMs < 1000)
                return $"{SureMs} ms";

            return $"{SureMs / 1000.0:0.00} sn";
        }
    }
}