namespace CoursVia.Models;

public class Durum
{
    public int DurumId { get; set; }

    public string DurumAdi { get; set; } = null!;

    public ICollection<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>();
    public ICollection<Kurs> Kurslar { get; set; } = new List<Kurs>();
}