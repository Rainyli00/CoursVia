namespace CoursVia.ViewModels.Ogrenci;

public class OgrenciPanelViewModel
{
    public string OgrenciAdSoyad { get; set; } = null!;

    public int ToplamKayitliKursSayisi { get; set; }

    public int DevamEdenKursSayisi { get; set; }

    public int TamamlananKursSayisi { get; set; }

    public int OrtalamaIlerlemeYuzdesi { get; set; }

    public bool FavorideMi { get; set; }
    public List<OgrenciKursOzetViewModel> Kurslar { get; set; } = new();
}