namespace CoursVia.Models;

public class Kurs
{
    public int KursId { get; set; }

    public int EgitmenId { get; set; }
    public int DurumId { get; set; }

    public string KursAdi { get; set; } = null!;
    public string? Aciklama { get; set; }
    public string KapakGorselUrl { get; set; } = null!;

    public DateTime OlusturmaTarihi { get; set; }
    public DateTime? GuncellemeTarihi { get; set; }

    // Navigation Properties
    public Kullanici Egitmen { get; set; } = null!;
    public Durum Durum { get; set; } = null!;
    public Sinav? Sinav { get; set; }

    public ICollection<KursKategorisi> KursKategorileri { get; set; } = new List<KursKategorisi>();

    public ICollection<Bolum> Bolumler { get; set; } = new List<Bolum>();

    public ICollection<Ders> Dersler { get; set; } = new List<Ders>();

    public ICollection<KursDegerlendirmesi> KursDegerlendirmeleri { get; set; } = new List<KursDegerlendirmesi>();
}