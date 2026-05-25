using CoursVia.Models;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CoursVia.ViewModels.Egitmen;

public class KursOlusturViewModel
{
    [Required(ErrorMessage = "Kurs adı zorunludur.")]
    [StringLength(150, ErrorMessage = "Kurs adı en fazla 150 karakter olabilir.")]
    public string KursAdi { get; set; } = null!;

    public string? Aciklama { get; set; }

    // Kullanıcı ister URL girer, ister dosya yükler.
    // Bu yüzden burada Required yok.
    // Zorunluluk kontrolünü POST action içinde yapıyoruz:
    // URL yoksa ve dosya yoksa hata veriyoruz.
    public string? KapakGorselUrl { get; set; }

    public IFormFile? KapakGorselDosya { get; set; }

    public List<int> SeciliKategoriIdleri { get; set; } = new();

    public List<Kategori> Kategoriler { get; set; } = new();
}