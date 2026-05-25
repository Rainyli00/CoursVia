namespace CoursVia.ViewModels.Ogrenci;

public class OgrenciDersIzleViewModel
{
    public int KursId { get; set; }

    public int KursKayitId { get; set; }

    public string KursAdi { get; set; } = null!;

    public string EgitmenAdSoyad { get; set; } = null!;

    public int SeciliDersId { get; set; }

    public string SeciliDersAdi { get; set; } = null!;

    public string? SeciliDersAciklama { get; set; }

    public string? VideoUrl { get; set; }

    public bool SeciliDersTamamlandiMi { get; set; }

    public int ToplamDersSayisi { get; set; }

    public int TamamlananDersSayisi { get; set; }

    public int IlerlemeYuzdesi { get; set; }

    public int? OncekiDersId { get; set; }

    public int? SonrakiDersId { get; set; }

    public bool TumDerslerTamamlandiMi { get; set; }

    public bool SinavVarMi { get; set; }
    public bool SinavGecildiMi { get; set; }

    public List<OgrenciDersBolumViewModel> Bolumler { get; set; } = new();

    public List<OgrenciDersMateryalViewModel> Materyaller { get; set; } = new();
}