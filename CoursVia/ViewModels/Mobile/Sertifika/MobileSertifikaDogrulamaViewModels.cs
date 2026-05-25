namespace CoursVia.ViewModels.Mobile.Sertifika;

public class MobileSertifikaDogrulamaResponseDto
{
    public bool Basarili { get; set; }

    public bool GecerliMi { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public MobileSertifikaDogrulamaDetayDto? Sertifika { get; set; }
}

public class MobileSertifikaDogrulamaDetayDto
{
    public int SertifikaId { get; set; }

    public string SertifikaKodu { get; set; } = string.Empty;

    public string OgrenciAdSoyad { get; set; } = string.Empty;

    public string KursAdi { get; set; } = string.Empty;

    public DateTime VerilmeTarihi { get; set; }
}