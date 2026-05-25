namespace CoursVia.ViewModels.Admin;

public class EgitmenOnaylariViewModel
{
    public string? Arama { get; set; }

    public string Durum { get; set; } = "bekleyen";

    public List<EgitmenOnayListeItemViewModel> Basvurular { get; set; } = new();

    public int BekleyenSayisi { get; set; }

    public int OnaylananSayisi { get; set; }

    public int ReddedilenSayisi { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; } = 1;

    public int ToplamSayfa { get; set; } = 1;

    public int SayfaBasinaKayit { get; set; } = 8;

    public bool OncekiSayfaVar => Sayfa > 1;

    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;
}

public class EgitmenOnayListeItemViewModel
{
    public int EgitmenProfilId { get; set; }

    public int KullaniciId { get; set; }

    public string AdSoyad { get; set; } = string.Empty;

    public string Eposta { get; set; } = string.Empty;

    public string? Telefon { get; set; }

    public string? ProfilFotoUrl { get; set; }

    public string? UzmanlikAlani { get; set; }

    public string? Biyografi { get; set; }

    public int? DeneyimYili { get; set; }

    public string? WebsiteUrl { get; set; }

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public List<string> Branslar { get; set; } = new();
}