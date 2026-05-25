using System.ComponentModel.DataAnnotations;

namespace CoursVia.ViewModels.Egitmen;

public class SinavHazirlaViewModel
{
    public int KursId { get; set; }

    public int? SinavId { get; set; }

    public string KursAdi { get; set; } = null!;

    [Required(ErrorMessage = "Sınav adı zorunludur.")]
    [StringLength(150, ErrorMessage = "Sınav adı en fazla 150 karakter olabilir.")]
    public string SinavAdi { get; set; } = null!;

    public string? Aciklama { get; set; }

    [Range(1, 100, ErrorMessage = "Geçme notu 1 ile 100 arasında olmalıdır.")]
    public int GecmeNotu { get; set; }

    [Range(1, 300, ErrorMessage = "Süre 1 ile 300 dakika arasında olmalıdır.")]
    public int SureDakika { get; set; }

    [Range(1, 200, ErrorMessage = "Soru sayısı 1 ile 200 arasında olmalıdır.")]
    public int SoruSayisi { get; set; }

    public int HavuzdakiSoruSayisi { get; set; }
}   