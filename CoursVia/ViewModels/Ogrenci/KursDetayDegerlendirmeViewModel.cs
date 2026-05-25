namespace CoursVia.ViewModels.Ogrenci;

public class KursDetayDegerlendirmeViewModel
{
    public int DegerlendirmeId { get; set; }

    public string OgrenciAdSoyad { get; set; } = string.Empty;

    public int Puan { get; set; }

    public string? YorumMetni { get; set; }

    public DateTime DegerlendirmeTarihi { get; set; }
}