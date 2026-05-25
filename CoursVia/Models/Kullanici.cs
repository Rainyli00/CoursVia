namespace CoursVia.Models;

public class Kullanici
{
    public int KullaniciId { get; set; }

    public int DurumId { get; set; }

    public string Ad { get; set; } = null!;
    public string Soyad { get; set; } = null!;
    public string Eposta { get; set; } = null!;
    public string SifreHash { get; set; } = null!;

    public string? Telefon { get; set; }
    public string? ProfilFotoUrl { get; set; }

    public DateTime KayitTarihi { get; set; }
    public DateTime? SonGirisTarihi { get; set; }
    public string? SonIpAdresi { get; set; }
    public bool OnlineMi { get; set; }

    // Navigation Properties
    public Durum Durum { get; set; } = null!;

    public EgitmenProfili? EgitmenProfili { get; set; }

    public ICollection<KullaniciRol> KullaniciRolleri { get; set; } = new List<KullaniciRol>();

    public ICollection<Kurs> EgitmenKurslari { get; set; } = new List<Kurs>();
    public ICollection<KursKaydi> KursKayitlari { get; set; } = new List<KursKaydi>();
    public ICollection<SifreSifirlama> SifreSifirlamalari { get; set; } = new List<SifreSifirlama>();

    public ICollection<MobilOturum> MobilOturumlari { get; set; } = new List<MobilOturum>();
}