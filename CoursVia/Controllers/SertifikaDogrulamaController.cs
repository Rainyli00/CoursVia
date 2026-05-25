using CoursVia.Data;
using CoursVia.ViewModels.Sertifika;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace CoursVia.Controllers;

public class SertifikaDogrulamaController : Controller
{
    private readonly AppDbContext _context;

    public SertifikaDogrulamaController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? kod)
    {
        var model = new SertifikaDogrulamaViewModel();

        if (!string.IsNullOrWhiteSpace(kod))
        {
            model.SertifikaKodu = kod;
            return await Sorgula(model);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sorgula(SertifikaDogrulamaViewModel model)
    {
        model.SorgulandiMi = true;

        if (string.IsNullOrWhiteSpace(model.SertifikaKodu))
        {
            model.GecerliMi = false;
            return View("Index", model);
        }

        var sertifika = await _context.Sertifikalar
            .Include(x => x.Kullanici)
            .Include(x => x.Kurs)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SertifikaKodu == model.SertifikaKodu);

        if (sertifika != null)
        {
            model.GecerliMi = true;
            model.OgrenciAdSoyad = $"{sertifika.Kullanici.Ad} {sertifika.Kullanici.Soyad}";
            model.KursAdi = sertifika.Kurs.KursAdi; // Note: Ensure Kurs has KursAdi property
            model.VerilmeTarihi = sertifika.VerilmeTarihi;
        }
        else
        {
            model.GecerliMi = false;
        }

        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Indir(string kod)
    {
        if (string.IsNullOrWhiteSpace(kod))
        {
            return RedirectToAction(nameof(Index));
        }

        var sertifika = await _context.Sertifikalar
            .AsNoTracking()
            .Include(x => x.Kullanici)
            .Include(x => x.Kurs)
                .ThenInclude(x => x.Egitmen)
            .FirstOrDefaultAsync(x => x.SertifikaKodu == kod);

        if (sertifika == null)
        {
            return RedirectToAction(nameof(Index));
        }

        string ogrenciAdSoyad = $"{sertifika.Kullanici.Ad} {sertifika.Kullanici.Soyad}".Trim();
        string egitmenAdSoyad = $"{sertifika.Kurs.Egitmen.Ad} {sertifika.Kurs.Egitmen.Soyad}".Trim();

        byte[] pdfDosyasi = SertifikaPdfOlustur(
            ogrenciAdSoyad,
            sertifika.Kurs.KursAdi,
            egitmenAdSoyad,
            sertifika.SertifikaKodu,
            sertifika.VerilmeTarihi
        );

        string dosyaAdi = $"CoursVia-Sertifika-{sertifika.SertifikaKodu}.pdf";

        return File(pdfDosyasi, "application/pdf", dosyaAdi);
    }

    private static byte[] SertifikaPdfOlustur(
        string ogrenciAdSoyad,
        string kursAdi,
        string egitmenAdSoyad,
        string sertifikaKodu,
        DateTime verilmeTarihi)
    {
        byte[] qrKodGorseli = QrKodOlustur(sertifikaKodu);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.PageColor(Colors.White);

                page.DefaultTextStyle(style =>
                    style.FontSize(11)
                         .FontColor(Colors.Grey.Darken3));

                page.Content()
                    .Border(1.5f)
                    .BorderColor(Colors.Amber.Darken1)
                    .Padding(10)
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Column(column =>
                    {
                        column.Spacing(0);

                        // Üst alan
                        column.Item()
                            .PaddingHorizontal(34)
                            .PaddingTop(22)
                            .Row(header =>
                            {
                                header.RelativeItem().Column(left =>
                                {
                                    left.Item()
                                        .Text("CoursVia")
                                        .Bold()
                                        .FontSize(28)
                                        .FontColor(Colors.Blue.Darken3);

                                    left.Item()
                                        .Text("Online Eğitim Platformu")
                                        .FontSize(10)
                                        .SemiBold()
                                        .FontColor(Colors.Grey.Darken1);
                                });

                                header.RelativeItem().AlignRight().Column(right =>
                                {
                                    right.Item()
                                        .AlignRight()
                                        .Text("Sertifika No")
                                        .FontSize(9)
                                        .SemiBold()
                                        .FontColor(Colors.Grey.Darken1);

                                    right.Item()
                                        .AlignRight()
                                        .Text(sertifikaKodu)
                                        .FontSize(11)
                                        .Bold()
                                        .FontColor(Colors.Grey.Darken4);

                                    right.Item()
                                        .PaddingTop(6)
                                        .AlignRight()
                                        .Width(66)
                                        .Height(66)
                                        .Image(qrKodGorseli)
                                        .FitArea();
                                });
                            });

                        // Ana başlık
                        column.Item()
                            .PaddingTop(34)
                            .AlignCenter()
                            .Text("BAŞARI SERTİFİKASI")
                            .Bold()
                            .FontSize(42)
                            .FontColor(Colors.Grey.Darken4);

                        column.Item()
                            .PaddingHorizontal(130)
                            .PaddingTop(4)
                            .LineHorizontal(1)
                            .LineColor(Colors.Grey.Darken1);

                        // Öğrenci adı
                        column.Item()
                            .PaddingTop(30)
                            .AlignCenter()
                            .Text(ogrenciAdSoyad)
                            .Bold()
                            .FontSize(36)
                            .FontColor(Colors.Black);

                        // Sertifika açıklaması
                        column.Item()
                            .PaddingTop(22)
                            .PaddingHorizontal(95)
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span("CoursVia platformunda sunulan ")
                                    .FontSize(15)
                                    .SemiBold()
                                    .FontColor(Colors.Black);

                                text.Span($"\"{kursAdi}\"")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor(Colors.Black);

                                text.Span(" eğitimini başarıyla tamamlamış; tüm ders içeriklerini bitirerek kurs sonu değerlendirme sınavında başarılı olmuştur.")
                                    .FontSize(15)
                                    .SemiBold()
                                    .FontColor(Colors.Black);
                            });

                        column.Item()
                            .PaddingTop(10)
                            .AlignCenter()
                            .Text($"{verilmeTarihi:dd.MM.yyyy} tarihinde bu sertifikayı almaya hak kazanmıştır.")
                            .FontSize(14)
                            .SemiBold()
                            .FontColor(Colors.Black);

                        column.Item()
                            .PaddingTop(10)
                            .AlignCenter()
                            .Text("★")
                            .FontSize(10)
                            .FontColor(Colors.Amber.Darken1);

                        // Alt bilgi alanı
                        column.Item()
                            .PaddingTop(28)
                            .PaddingHorizontal(130)
                            .Row(info =>
                            {
                                info.RelativeItem().Column(item =>
                                {
                                    item.Item()
                                        .BorderTop(1)
                                        .BorderColor(Colors.Grey.Lighten1)
                                        .PaddingTop(8)
                                        .AlignCenter()
                                        .Text("Verilme Tarihi")
                                        .FontSize(9)
                                        .SemiBold()
                                        .FontColor(Colors.Grey.Darken1);

                                    item.Item()
                                        .AlignCenter()
                                        .Text(verilmeTarihi.ToString("dd.MM.yyyy"))
                                        .FontSize(12)
                                        .Bold()
                                        .FontColor(Colors.Grey.Darken4);
                                });

                                info.ConstantItem(60);

                                info.RelativeItem().Column(item =>
                                {
                                    item.Item()
                                        .BorderTop(1)
                                        .BorderColor(Colors.Grey.Lighten1)
                                        .PaddingTop(8)
                                        .AlignCenter()
                                        .Text("Eğitmen")
                                        .FontSize(9)
                                        .SemiBold()
                                        .FontColor(Colors.Grey.Darken1);

                                    item.Item()
                                        .AlignCenter()
                                        .Text(egitmenAdSoyad)
                                        .FontSize(12)
                                        .Bold()
                                        .FontColor(Colors.Grey.Darken4);
                                });
                            });
                    });
            });
        }).GeneratePdf();
    }

    private static byte[] QrKodOlustur(string sertifikaKodu)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(sertifikaKodu, QRCodeGenerator.ECCLevel.Q);

        var qrCode = new PngByteQRCode(qrData);

        return qrCode.GetGraphic(20);
    }
}
