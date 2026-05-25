namespace CoursVia.Models;

public class Kategori
{
    public int KategoriId { get; set; }

    public string KategoriAdi { get; set; } = null!;

    public ICollection<KursKategorisi> KursKategorileri { get; set; } = new List<KursKategorisi>();

    public ICollection<EgitmenBransi> EgitmenBranslari { get; set; } = new List<EgitmenBransi>();
}