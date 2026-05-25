using CoursVia.Models;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CoursVia.ViewModels.Egitmen;

public class KursDuzenleViewModel
{
    public int KursId { get; set; }

    [Required(ErrorMessage = "Kurs adı zorunludur.")]
    [StringLength(150, ErrorMessage = "Kurs adı en fazla 150 karakter olabilir.")]
    public string KursAdi { get; set; } = null!;

    public string? Aciklama { get; set; }

    public string? KapakGorselUrl { get; set; }
    
    public string? MevcutKapakGorselUrl { get; set; } // Mevcut görseli göstermek ve saklamak için

    public IFormFile? KapakGorselDosya { get; set; }

    public List<int> SeciliKategoriIdleri { get; set; } = new();

    public List<Kategori> Kategoriler { get; set; } = new();
}
