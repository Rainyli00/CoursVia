namespace CoursVia.ViewModels.Sertifika;

public class SertifikaDogrulamaViewModel
{
    public string? SertifikaKodu { get; set; }
    public bool SorgulandiMi { get; set; }
    public bool GecerliMi { get; set; }

    public string? OgrenciAdSoyad { get; set; }
    public string? KursAdi { get; set; }
    public DateTime? VerilmeTarihi { get; set; }
}
