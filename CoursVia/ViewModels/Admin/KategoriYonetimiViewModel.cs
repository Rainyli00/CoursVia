namespace CoursVia.ViewModels.Admin;

public class KategoriYonetimiViewModel
{
    public string? Arama { get; set; }
    public string? DurumFiltresi { get; set; }

    public List<KategoriListeItemViewModel> Kategoriler { get; set; } = new();

    public int ToplamKategoriSayisi { get; set; }

    public int KullanilanKategoriSayisi { get; set; }

    public int BosKategoriSayisi { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; } = 1;

    public int ToplamSayfa { get; set; } = 1;

    public int SayfaBasinaKayit { get; set; } = 10;

    public bool OncekiSayfaVar => Sayfa > 1;

    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;
}

public class KategoriListeItemViewModel
{
    public int KategoriId { get; set; }

    public string KategoriAdi { get; set; } = string.Empty;

    public int KursSayisi { get; set; }

    public int EgitmenSayisi { get; set; }

    public bool KullaniliyorMu => KursSayisi > 0 || EgitmenSayisi > 0;

    public bool SistemKategorisiMi => KategoriAdi.Trim().ToLower() == "diğer";
}

public class KategoriKaydetViewModel
{
    public int? KategoriId { get; set; }

    public string KategoriAdi { get; set; } = string.Empty;
}