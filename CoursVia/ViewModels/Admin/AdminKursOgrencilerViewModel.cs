namespace CoursVia.ViewModels.Admin;

public class AdminKursOgrencilerViewModel
{
    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string? Arama { get; set; }

    public string Durum { get; set; } = "tum";

    public string Siralama { get; set; } = "yeni";

    public List<AdminKursOgrenciListeItemViewModel> Ogrenciler { get; set; } = new();

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; } = 1;

    public int ToplamSayfa { get; set; } = 1;

    public int SayfaBasinaKayit { get; set; } = 10;

    public bool OncekiSayfaVar => Sayfa > 1;

    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;
}

public class AdminKursOgrenciListeItemViewModel
{
    public int KursKayitId { get; set; }

    public int OgrenciId { get; set; }

    public string AdSoyad { get; set; } = string.Empty;

    public string Eposta { get; set; } = string.Empty;

    public DateTime KayitTarihi { get; set; }

    public bool AktifMi { get; set; }

    public bool TamamlandiMi { get; set; }

    public int ToplamDersSayisi { get; set; }

    public int TamamlananDersSayisi { get; set; }

    public int IlerlemeYuzdesi { get; set; }

    public string KayitDurumu
    {
        get
        {
            if (!AktifMi)
            {
                return "Pasif";
            }

            return TamamlandiMi
                ? "Tamamlayan"
                : "Devam Eden";
        }
    }

    public string SinavDurumu { get; set; } = "Henüz girmedi";

    public int? SonSinavPuani { get; set; }

    public bool? SinavdanGectiMi { get; set; }

    public DateTime? SonSinavTarihi { get; set; }

    public int SinavGirisSayisi { get; set; }
}
