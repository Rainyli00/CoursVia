namespace CoursVia.ViewModels.Ogrenci;

public class OgrenciSinavlarimViewModel
{
    public int ToplamSinavSayisi { get; set; }

    public int GirilebilirSinavSayisi { get; set; }

    public int GecilenSinavSayisi { get; set; }

    public int DevamEdenSinavSayisi { get; set; }
    public string? Arama { get; set; }
    public string Durum { get; set; } = "tum";

    public int Sayfa { get; set; } = 1;
    public int ToplamSayfa { get; set; }
    public int ToplamKayit { get; set; }
    public int SayfaBasinaKayit { get; set; } = 10;

    public bool OncekiSayfaVar => Sayfa > 1;
    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;

    public List<OgrenciSinavListeItemViewModel> Sinavlar { get; set; } = new();
}

public class OgrenciSinavListeItemViewModel
{
    public int KursId { get; set; }

    public int KursKayitId { get; set; }

    public int SinavId { get; set; }

    public int? DevamEdenSinavKatilimId { get; set; }

    public int? SonSinavKatilimId { get; set; }

    public string KursAdi { get; set; } = null!;

    public string? KapakGorselUrl { get; set; }

    public int KursDurumId { get; set; }

    public string KursDurumAdi { get; set; } = string.Empty;

    public bool GuncelleniyorMu => KursDurumId == 7;

    public string SinavAdi { get; set; } = null!;

    public int GecmeNotu { get; set; }

    public int SureDakika { get; set; }

    public int SoruSayisi { get; set; }

    public int ToplamDersSayisi { get; set; }

    public int TamamlananDersSayisi { get; set; }

    public bool DerslerTamamlandiMi { get; set; }

    public int GirisSayisi { get; set; }

    public int KalanHak { get; set; }

    public int? SonPuan { get; set; }

    public bool? SonucGectiMi { get; set; }

    public bool KursTamamlandiMi { get; set; }

    public bool SinavaGirebilirMi { get; set; }

    public string DurumMetni { get; set; } = null!;
}
