namespace CoursVia.ViewModels.Egitmen;

public class KursOnIzleViewModel
{
    public int KursId { get; set; }

    public string KursAdi { get; set; } = null!;

    public string? Aciklama { get; set; }

    public string KapakGorselUrl { get; set; } = null!;

    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = null!;

    public int OgrenciSayisi { get; set; }

    public List<string> Kategoriler { get; set; } = new();

    public List<KursOnIzleBolumViewModel> Bolumler { get; set; } = new();

    public KursOnIzleSinavViewModel? Sinav { get; set; }

    public bool KursDuzenlenebilirMi { get; set; }

    public bool KursYayinaGonderilebilirMi { get; set; }

    public List<string> Eksikler { get; set; } = new();

    public double OrtalamaPuan { get; set; }

    public int DegerlendirmeSayisi { get; set; }

    public List<KursOnIzleDegerlendirmeViewModel> Degerlendirmeler { get; set; } = new();

    public int YorumSayfa { get; set; } = 1;

    public int YorumToplamSayfa { get; set; } = 1;

    public int YorumToplamKayit { get; set; }

    public int YorumSayfaBoyutu { get; set; } = 5;

    public bool YorumOncekiSayfaVar => YorumSayfa > 1;

    public bool YorumSonrakiSayfaVar => YorumSayfa < YorumToplamSayfa;
}

public class KursOnIzleDegerlendirmeViewModel
{
    public int DegerlendirmeId { get; set; }

    public string OgrenciAdSoyad { get; set; } = string.Empty;

    public int Puan { get; set; }

    public string? YorumMetni { get; set; }

    public DateTime DegerlendirmeTarihi { get; set; }
}

public class KursOnIzleBolumViewModel
{
    public int BolumId { get; set; }

    public string BolumAdi { get; set; } = null!;

    public int SiraNo { get; set; }

    public List<KursOnIzleDersViewModel> Dersler { get; set; } = new();
}

public class KursOnIzleDersViewModel
{
    public int DersId { get; set; }

    public string DersAdi { get; set; } = null!;

    public string? Aciklama { get; set; }

    public string VideoUrl { get; set; } = null!;

    public int SiraNo { get; set; }

    public List<KursOnIzleMateryalViewModel> Materyaller { get; set; } = new();
}

public class KursOnIzleMateryalViewModel
{
    public string Baslik { get; set; } = null!;

    public string MateryalUrl { get; set; } = null!;
}

public class KursOnIzleSinavViewModel
{
    public int SinavId { get; set; }

    public string SinavAdi { get; set; } = null!;

    public string? Aciklama { get; set; }

    public int GecmeNotu { get; set; }

    public int SureDakika { get; set; }

    public int SoruSayisi { get; set; }

    public int HavuzdakiSoruSayisi { get; set; }

    public int GecerliSoruSayisi { get; set; }

    public int SoruSayfa { get; set; } = 1;

    public int SoruSayfaBoyutu { get; set; } = 10;

    public int ToplamSoruSayfa => HavuzdakiSoruSayisi == 0
        ? 1
        : (int)Math.Ceiling(HavuzdakiSoruSayisi / (double)SoruSayfaBoyutu);

    public List<KursOnIzleSoruViewModel> Sorular { get; set; } = new();
}

public class KursOnIzleSoruViewModel
{
    public int SoruId { get; set; }

    public string SoruMetni { get; set; } = null!;

    public bool GecerliMi { get; set; }

    public List<string> DersAdlari { get; set; } = new();

    public List<KursOnIzleSecenekViewModel> Secenekler { get; set; } = new();
}

public class KursOnIzleSecenekViewModel
{
    public string SecenekMetni { get; set; } = null!;

    public bool DogruMu { get; set; }
}