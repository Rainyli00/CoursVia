namespace CoursVia.ViewModels.Ogrenci
{
    public class OgrenciSertifikalarViewModel
    {
        public int ToplamSertifikaSayisi { get; set; }
        public DateTime? SonSertifikaTarihi { get; set; }

        public string? Arama { get; set; }
        public string Sirala { get; set; } = "tarih-desc";

        public int Sayfa { get; set; } = 1;
        public int ToplamSayfa { get; set; }
        public int ToplamKayit { get; set; }
        public int SayfaBasinaKayit { get; set; } = 10;

        public bool OncekiSayfaVar => Sayfa > 1;
        public bool SonrakiSayfaVar => Sayfa < ToplamSayfa;

        public List<OgrenciSertifikaListeItemViewModel> Sertifikalar { get; set; } = new();
    }

    public class OgrenciSertifikaListeItemViewModel
    {
        public int SertifikaId { get; set; }
        public int KursId { get; set; }

        public string KursAdi { get; set; } = string.Empty;
        public string EgitmenAdSoyad { get; set; } = string.Empty;
        public string SertifikaKodu { get; set; } = string.Empty;
        public string? KapakGorselUrl { get; set; }

        public DateTime VerilmeTarihi { get; set; }
    }
}