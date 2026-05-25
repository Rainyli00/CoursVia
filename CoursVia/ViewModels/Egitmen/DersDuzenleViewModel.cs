using CoursVia.Models;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CoursVia.ViewModels.Egitmen;

public class DersDuzenleViewModel
{
    public int DersId { get; set; }

    public int KursId { get; set; }

    [Required(ErrorMessage = "Bölüm seçimi zorunludur.")]
    public int BolumId { get; set; }

    [Required(ErrorMessage = "Ders adı zorunludur.")]
    [StringLength(200, ErrorMessage = "Ders adı en fazla 200 karakter olabilir.")]
    public string DersAdi { get; set; } = null!;

    public string? Aciklama { get; set; }

    public string MevcutVideoUrl { get; set; } = null!;

    public string VideoTipi { get; set; } = "mevcut";

    public string? VideoUrl { get; set; }

    public IFormFile? VideoDosyasi { get; set; }

    public List<int> MevcutMateryalIdleri { get; set; } = new();
    public List<string> MevcutMateryalBasliklari { get; set; } = new();
    public List<int> SilinecekMateryalIdleri { get; set; } = new();

    public List<string>? MateryalBasliklari { get; set; }
    public List<IFormFile>? MateryalDosyalari { get; set; }

    public List<Bolum> Bolumler { get; set; } = new();
}
