namespace CoursVia.Models;

public class KursKategorisi
{
    public int KursKategoriId { get; set; }

    public int KursId { get; set; }
    public int KategoriId { get; set; }

    // Navigation Properties
    public Kurs Kurs { get; set; } = null!;
    public Kategori Kategori { get; set; } = null!;
}