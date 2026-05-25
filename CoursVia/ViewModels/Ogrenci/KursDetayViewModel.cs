namespace CoursVia.ViewModels.Ogrenci;

public class KursDetayViewModel
{
    public int KursId { get; set; }

    public string KursAdi { get; set; } = null!;

    public string? Aciklama { get; set; }

    public string? KapakGorselUrl { get; set; }

    public string EgitmenAdSoyad { get; set; } = null!;

    public List<string> Kategoriler { get; set; } = new();

    public int BolumSayisi { get; set; }

    public int DersSayisi { get; set; }

    public bool KayitliMi { get; set; }

    public bool KendiKursuMu { get; set; }
    public double OrtalamaPuan { get; set; }
    public int DegerlendirmeSayisi { get; set; }

    public List<KursDetayDegerlendirmeViewModel> Degerlendirmeler { get; set; } = new();

    public int YorumSayfa { get; set; } = 1;
    public int YorumToplamSayfa { get; set; }
    public int YorumToplamKayit { get; set; }

    public bool YorumOncekiSayfaVar => YorumSayfa > 1;
    public bool YorumSonrakiSayfaVar => YorumSayfa < YorumToplamSayfa;

    public List<KursDetayBolumViewModel> Bolumler { get; set; } = new();

    public KursDetaySinavViewModel? Sinav { get; set; }
}

public class KursDetayBolumViewModel
{
    public int BolumId { get; set; }

    public string BolumAdi { get; set; } = null!;

    public int SiraNo { get; set; }

    public List<KursDetayDersViewModel> Dersler { get; set; } = new();
}

public class KursDetayDersViewModel
{
    public int DersId { get; set; }

    public string DersAdi { get; set; } = null!;

    public string? Aciklama { get; set; }

    public int SiraNo { get; set; }

    public int MateryalSayisi { get; set; }
}

public class KursDetaySinavViewModel
{
    public string SinavAdi { get; set; } = null!;

    public string? Aciklama { get; set; }

    public int GecmeNotu { get; set; }

    public int SureDakika { get; set; }

    public int SoruSayisi { get; set; }
}