namespace CoursVia.ViewModels.Ogrenci;

public class OgrenciKursOzetViewModel
{
    public int KursKayitId { get; set; }

    public int KursId { get; set; }

    public string KursAdi { get; set; } = null!;

    public string? KapakGorselUrl { get; set; }

    public string EgitmenAdSoyad { get; set; } = null!;

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public bool GuncelleniyorMu => DurumId == 7;

    public DateTime KayitTarihi { get; set; }

    public int ToplamDersSayisi { get; set; }

    public int TamamlananDersSayisi { get; set; }

    public int IlerlemeYuzdesi { get; set; }

    public bool KursTamamlandiMi { get; set; }

    public string SinavDurumu { get; set; } = null!;

    public int? SonSinavPuani { get; set; }

    public bool? SinavdanGectiMi { get; set; }
    public bool FavorideMi { get; set; }
    public bool DegerlendirmeVarMi { get; set; }
    public int? KendiPuan { get; set; }
    public string? KendiYorumMetni { get; set; }
    public List<string> Kategoriler { get; set; } = new();
}
