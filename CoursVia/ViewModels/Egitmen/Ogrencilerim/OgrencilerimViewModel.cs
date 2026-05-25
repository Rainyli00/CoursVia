namespace CoursVia.ViewModels.Egitmen.Ogrencilerim;

public class OgrencilerimViewModel
{
    public string? Arama { get; set; }

    public int? KursId { get; set; }

    public List<KursSecimViewModel> Kurslar { get; set; } = new();

    public List<OgrencilerimListeItemViewModel> Ogrenciler { get; set; } = new();

    public int ToplamOgrenciSayisi { get; set; }

    public int OrtalamaIlerlemeYuzdesi { get; set; }

    public int SinavaGirenOgrenciSayisi { get; set; }

    public int SinavdanGecenOgrenciSayisi { get; set; }

    public int ToplamKayit { get; set; }

    public int Sayfa { get; set; } = 1;

    public int ToplamSayfa { get; set; } = 1;

    public int SayfaBasinaKayit { get; set; } = 10;

    public bool OncekiSayfaVar => Sayfa > 1;

    public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;
}