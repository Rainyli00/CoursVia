namespace CoursVia.Models;

public class EgitmenProfili
{
    public int EgitmenProfilId { get; set; }

    public int KullaniciId { get; set; }

    public int DurumId { get; set; }

    public string? UzmanlikAlani { get; set; }
    public string? Biyografi { get; set; }

    public int? DeneyimYili { get; set; }

    public string? WebsiteUrl { get; set; }

    // Navigation Properties
    public Kullanici Kullanici { get; set; } = null!;
    public Durum Durum { get; set; } = null!;

    public ICollection<EgitmenBransi> EgitmenBranslari { get; set; } = new List<EgitmenBransi>();
}