using System.ComponentModel.DataAnnotations;

namespace CoursVia.ViewModels.Ogrenci;

public class OgrenciEgitmenBasvuruViewModel
{
    public int? EgitmenProfilId { get; set; }

    public int? MevcutDurumId { get; set; }

    public string? MevcutDurumAdi { get; set; }

    [Display(Name = "Uzmanlık Alanı")]
    [Required(ErrorMessage = "Uzmanlık alanı zorunludur.")]
    [StringLength(150, ErrorMessage = "Uzmanlık alanı en fazla 150 karakter olabilir.")]
    public string? UzmanlikAlani { get; set; }

    [Display(Name = "Biyografi")]
    [Required(ErrorMessage = "Biyografi zorunludur.")]
    public string? Biyografi { get; set; }

    [Display(Name = "Deneyim Yılı")]
    [Range(0, 60, ErrorMessage = "Deneyim yılı 0 ile 60 arasında olmalıdır.")]
    public int? DeneyimYili { get; set; }

    [Display(Name = "Web Site / Portfolyo")]
    [StringLength(250, ErrorMessage = "Web site adresi en fazla 250 karakter olabilir.")]
    public string? WebsiteUrl { get; set; }

    [Display(Name = "Branşlar")]
    public List<int> SeciliBransIdleri { get; set; } = new();

    public List<OgrenciEgitmenBransSecimViewModel> BransSecenekleri { get; set; } = new();

    public bool BasvuruVarMi { get; set; }

    public bool OnayBekliyorMu => MevcutDurumId == 4;

    public bool OnaylandiMi => MevcutDurumId == 8;

    public bool ReddedildiMi => MevcutDurumId == 6;
}

public class OgrenciEgitmenBransSecimViewModel
{
    public int KategoriId { get; set; }

    public string KategoriAdi { get; set; } = string.Empty;

    public bool SeciliMi { get; set; }
}