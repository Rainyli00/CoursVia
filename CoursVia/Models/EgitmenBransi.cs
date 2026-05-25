namespace CoursVia.Models;

public class EgitmenBransi
{
    public int EgitmenBransId { get; set; }

    public int EgitmenProfilId { get; set; }
    public int KategoriId { get; set; }

    // Navigation Properties
    public EgitmenProfili EgitmenProfili { get; set; } = null!;
    public Kategori Kategori { get; set; } = null!;
}