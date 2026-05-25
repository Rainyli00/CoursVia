using System.ComponentModel.DataAnnotations;

namespace CoursVia.ViewModels.Egitmen;

public class SoruEkleViewModel
{
    public int KursId { get; set; }
    public int SinavId { get; set; }

    public string KursAdi { get; set; } = null!;
    public string SinavAdi { get; set; } = null!;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Soru metni zorunludur.")]
    public string? SoruMetni { get; set; }

    public List<int> SeciliDersIdleri { get; set; } = new();

    public List<string> SecenekMetinleri { get; set; } = new();

    public List<int> SecenekIdleri { get; set; } = new();

    public List<bool> SecenekAktifDurumlari { get; set; } = new();

    public int? DogruSecenekIndex { get; set; }

    public List<SoruDersSecimViewModel> Dersler { get; set; } = new();
}

public class SoruDuzenleViewModel : SoruEkleViewModel
{
    public int SoruId { get; set; }
}

public class SoruDersSecimViewModel
{
    public int DersId { get; set; }

    public string DersAdi { get; set; } = null!;
    public string BolumAdi { get; set; } = null!;
}
