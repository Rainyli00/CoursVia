using CoursVia.ViewModels.Ogrenci;
using System.ComponentModel.DataAnnotations;

namespace CoursVia.ViewModels.ProfilAyarlari;

public class ProfilAyarlariViewModel
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir.")]
    public string Ad { get; set; } = "";

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir.")]
    public string Soyad { get; set; } = "";

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [StringLength(150, ErrorMessage = "E-posta en fazla 150 karakter olabilir.")]
    public string Eposta { get; set; } = "";

    [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir.")]
    public string? Telefon { get; set; }

    public string? ProfilFotoUrl { get; set; }

    public string? KirpilmisProfilFotoBase64 { get; set; }

    public string? MevcutSifre { get; set; }

    public string? YeniSifre { get; set; }

    public string? YeniSifreTekrar { get; set; }

    public DateTime KayitTarihi { get; set; }

    public DateTime? SonGirisTarihi { get; set; }

    public string? AktifRol { get; set; }

    // Eğitmen başvuru modalı için
    public bool OgrenciMi { get; set; }

    public bool EgitmenMi { get; set; }

    public bool AdminMi { get; set; }

    public bool OgrenciProfiliSilinebilirMi => OgrenciMi && (EgitmenMi || AdminMi);

    public OgrenciEgitmenBasvuruViewModel EgitmenBasvuru { get; set; } = new();
}
