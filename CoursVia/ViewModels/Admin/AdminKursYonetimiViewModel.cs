namespace CoursVia.ViewModels.Admin;

public class AdminKursYonetimiViewModel
{
    public string? Arama { get; set; }

    public string Durum { get; set; } = "tum";

    public int? KategoriId { get; set; }

    public List<AdminKursKategoriFiltreViewModel> KategoriSecenekleri { get; set; } = new();

    public List<AdminKursListeItemViewModel> Kurslar { get; set; } = new();

    public int ToplamKursSayisi { get; set; }

    public int OnayliKursSayisi { get; set; }

    public int OnayBekleyenKursSayisi { get; set; }

    public int TaslakKursSayisi { get; set; }

    public int DuzeltmeIstenenKursSayisi { get; set; }

    public int ReddedilenKursSayisi { get; set; }

    public int PasifKursSayisi { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; } = 1;

    public int ToplamSayfa { get; set; } = 1;

    public int SayfaBasinaKayit { get; set; } = 10;

    public bool OncekiSayfaVar => Sayfa > 1;

    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;
}

public class AdminKursKategoriFiltreViewModel
{
    public int KategoriId { get; set; }

    public string KategoriAdi { get; set; } = string.Empty;

    public int KursSayisi { get; set; }
}

public class AdminKursListeItemViewModel
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

    public int OgrenciSayisi { get; set; }

    public int SertifikaSayisi { get; set; }

    public int DegerlendirmeSayisi { get; set; }

    public bool SinavVarMi { get; set; }

    public int SoruSayisi { get; set; }

    public List<string> Kategoriler { get; set; } = new();

    public List<int> KategoriIdleri { get; set; } = new();
}

public class AdminKursDetayViewModel
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

    public List<AdminKursKategoriFiltreViewModel> KategoriSecenekleri { get; set; } = new();

    public List<int> KategoriIdleri { get; set; } = new();

    public List<AdminKursBolumViewModel> Bolumler { get; set; } = new();

    public AdminKursSinavViewModel? Sinav { get; set; }

    public int OgrenciSayisi { get; set; }

    public int KayitliOgrenciSayisi { get; set; }

    public int DevamEdenOgrenciSayisi { get; set; }

    public int TamamlayanOgrenciSayisi { get; set; }

    public int PasifKayitSayisi { get; set; }

    public int SertifikaSayisi { get; set; }

    public int DegerlendirmeSayisi { get; set; }

    public double OrtalamaPuan { get; set; }

    public List<AdminKursDegerlendirmeViewModel> Degerlendirmeler { get; set; } = new();

    public int YorumSayfa { get; set; } = 1;

    public int YorumToplamSayfa { get; set; } = 1;

    public int YorumToplamKayit { get; set; }

    public int YorumSayfaBasinaKayit { get; set; } = 5;

    public bool YorumOncekiSayfaVar => YorumSayfa > 1;

    public bool YorumSonrakiSayfaVar => YorumSayfa < YorumToplamSayfa;

    public int FavoriSayisi { get; set; }

    public bool DuzenlemeyeGonderilebilir => DurumId != 7 && DurumId != 2;

    public bool PasifeAlinabilir => DurumId != 2;

    public bool YayinaAlinabilir => DurumId == 2 || DurumId == 6 || DurumId == 7;

    public bool Silinebilir => true;
}

public class AdminKursDegerlendirmeViewModel
{
    public int DegerlendirmeId { get; set; }

    public string OgrenciAdSoyad { get; set; } = string.Empty;

    public int Puan { get; set; }

    public string? YorumMetni { get; set; }

    public DateTime DegerlendirmeTarihi { get; set; }
}

public class AdminKursBolumViewModel
{
    public int BolumId { get; set; }

    public string BolumAdi { get; set; } = string.Empty;

    public int SiraNo { get; set; }

    public List<AdminKursDersViewModel> Dersler { get; set; } = new();
}

public class AdminKursDersViewModel
{
    public int DersId { get; set; }

    public string DersAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public string VideoUrl { get; set; } = string.Empty;

    public int SiraNo { get; set; }

    public bool AktifMi { get; set; }

    public bool SistemDersiMi { get; set; }

    public List<AdminKursDersMateryalViewModel> Materyaller { get; set; } = new();
}

public class AdminKursDersMateryalViewModel
{
    public int MateryalId { get; set; }

    public string Baslik { get; set; } = string.Empty;

    public string MateryalUrl { get; set; } = string.Empty;

    public string MateryalTipAdi { get; set; } = string.Empty;
}

public class AdminKursSinavViewModel
{
    public int SinavId { get; set; }

    public string SinavAdi { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public int GecmeNotu { get; set; }

    public int SureDakika { get; set; }

    public int SoruSayisi { get; set; }

    public int AktifSoruSayisi { get; set; }

    public List<AdminKursSoruViewModel> Sorular { get; set; } = new();
}

public class AdminKursSoruViewModel
{
    public int SoruId { get; set; }

    public string SoruMetni { get; set; } = string.Empty;

    public bool AktifMi { get; set; }

    public List<AdminKursSoruSecenegiViewModel> Secenekler { get; set; } = new();
}

public class AdminKursSoruSecenegiViewModel
{
    public int SecenekId { get; set; }

    public string SecenekMetni { get; set; } = string.Empty;

    public bool DogruMu { get; set; }

    public bool AktifMi { get; set; }
}
