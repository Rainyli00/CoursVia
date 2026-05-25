namespace CoursVia.ViewModels.Ogrenci;

public class OgrenciDersListeItemViewModel
{
    public int DersId { get; set; }

    public string DersAdi { get; set; } = null!;

    public int SiraNo { get; set; }

    public bool TamamlandiMi { get; set; }

    public bool SeciliMi { get; set; }
}