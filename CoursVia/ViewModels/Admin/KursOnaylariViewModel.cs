namespace CoursVia.ViewModels.Admin;

public class KursOnaylariViewModel
{
    public string? Arama { get; set; }

    public string Durum { get; set; } = "bekleyen";

    public List<KursOnayListeItemViewModel> Kurslar { get; set; } = new();

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

public class KursOnayListeItemViewModel
{
    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public string? KapakGorselUrl { get; set; }

    public string EgitmenAdSoyad { get; set; } = string.Empty;

    public string EgitmenEposta { get; set; } = string.Empty;

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public DateTime OlusturmaTarihi { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }

    public int BolumSayisi { get; set; }

    public int DersSayisi { get; set; }

    public bool SinavVarMi { get; set; }

    public int SoruSayisi { get; set; }

    public List<string> Kategoriler { get; set; } = new();
}

public class KursOnayDetayViewModel
{
    public int KursId { get; set; }

    public string KursAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public string? KapakGorselUrl { get; set; }

    public string EgitmenAdSoyad { get; set; } = string.Empty;

    public string EgitmenEposta { get; set; } = string.Empty;

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = string.Empty;

    public DateTime OlusturmaTarihi { get; set; }

    public DateTime? GuncellemeTarihi { get; set; }

    public List<string> Kategoriler { get; set; } = new();

    public List<KursOnayBolumViewModel> Bolumler { get; set; } = new();

    public KursOnaySinavViewModel? Sinav { get; set; }

    public bool KararVerilebilir => DurumId == 4;
}

public class KursOnayBolumViewModel
{
    public int BolumId { get; set; }

    public string BolumAdi { get; set; } = string.Empty;

    public int SiraNo { get; set; }

    public List<KursOnayDersViewModel> Dersler { get; set; } = new();
}

public class KursOnayDersViewModel
{
    public int DersId { get; set; }

    public string DersAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public string VideoUrl { get; set; } = string.Empty;

    public int SiraNo { get; set; }

    public int MateryalSayisi { get; set; }

    public List<KursOnayDersMateryalViewModel> Materyaller { get; set; } = new();
}

public class KursOnayDersMateryalViewModel
{
    public int MateryalId { get; set; }

    public string Baslik { get; set; } = string.Empty;

    public string MateryalUrl { get; set; } = string.Empty;

    public string MateryalTipAdi { get; set; } = string.Empty;
}

public class KursOnaySinavViewModel
{
    public int SinavId { get; set; }

    public string SinavAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public int GecmeNotu { get; set; }

    public int SureDakika { get; set; }

    public int SoruSayisi { get; set; }

    public int AktifSoruSayisi { get; set; }

    public List<KursOnaySoruViewModel> Sorular { get; set; } = new();
}

public class KursOnaySoruViewModel
{
    public int SoruId { get; set; }

    public string SoruMetni { get; set; } = string.Empty;

    public bool AktifMi { get; set; }

    public List<KursOnaySoruSecenegiViewModel> Secenekler { get; set; } = new();
}

public class KursOnaySoruSecenegiViewModel
{
    public int SecenekId { get; set; }

    public string SecenekMetni { get; set; } = string.Empty;

    public bool DogruMu { get; set; }

    public bool AktifMi { get; set; }
}
