namespace CoursVia.ViewModels.Egitmen.Ogrencilerim;

public class OgrencilerimListeItemViewModel
{
    public int KursKayitId { get; set; }

    public int OgrenciId { get; set; }

    public string OgrenciAdSoyad { get; set; } = null!;

    public int KursId { get; set; }

    public string KursAdi { get; set; } = null!;

    public DateTime KayitTarihi { get; set; }

    public int ToplamDersSayisi { get; set; }

    public int TamamlananDersSayisi { get; set; }

    public int IlerlemeYuzdesi { get; set; }

    public string SinavDurumu { get; set; } = null!;

    public int? SonSinavPuani { get; set; }

    public bool? SinavdanGectiMi { get; set; }

    public DateTime? SonSinavTarihi { get; set; }

    public int SinavGirisSayisi { get; set; }
}