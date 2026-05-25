using System.Security.Claims;
using CoursVia.Data;
using CoursVia.ViewModels.Ogrenci;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace CoursVia.Controllers
{
    [Authorize(Roles = "Öğrenci")]
    public class OgrenciSertifikaController : Controller
    {
        private readonly AppDbContext _context;

        public OgrenciSertifikaController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? arama, string sirala = "tarih-desc", int sayfa = 1)
        {
            int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            const int sayfaBasinaKayit = 6;

            arama = arama?.Trim();

            if (sayfa < 1)
            {
                sayfa = 1;
            }

            var query = _context.Sertifikalar
                .AsNoTracking()
                .Where(x => x.KullaniciId == kullaniciId);

            if (!string.IsNullOrWhiteSpace(arama))
            {
                query = query.Where(x =>
                    x.Kurs.KursAdi.Contains(arama) ||
                    x.SertifikaKodu.Contains(arama));
            }

            query = sirala switch
            {
                "tarih-asc" => query.OrderBy(x => x.VerilmeTarihi),
                _ => query.OrderByDescending(x => x.VerilmeTarihi)
            };

            int toplamKayit = await query.CountAsync();

            int toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);

            if (toplamSayfa < 1)
            {
                toplamSayfa = 1;
            }

            if (sayfa > toplamSayfa)
            {
                sayfa = toplamSayfa;
            }

            var sertifikalar = await query
                .Skip((sayfa - 1) * sayfaBasinaKayit)
                .Take(sayfaBasinaKayit)
                .Select(x => new OgrenciSertifikaListeItemViewModel
                {
                    SertifikaId = x.SertifikaId,
                    KursId = x.KursId,
                    KursAdi = x.Kurs.KursAdi,
                    EgitmenAdSoyad = (x.Kurs.Egitmen.Ad + " " + x.Kurs.Egitmen.Soyad).Trim(),
                    SertifikaKodu = x.SertifikaKodu,
                    KapakGorselUrl = x.Kurs.KapakGorselUrl,
                    VerilmeTarihi = x.VerilmeTarihi
                })
                .ToListAsync();

            var sonSertifikaTarihi = await _context.Sertifikalar
                .AsNoTracking()
                .Where(x => x.KullaniciId == kullaniciId)
                .OrderByDescending(x => x.VerilmeTarihi)
                .Select(x => (DateTime?)x.VerilmeTarihi)
                .FirstOrDefaultAsync();

            var model = new OgrenciSertifikalarViewModel
            {
                ToplamSertifikaSayisi = toplamKayit,
                SonSertifikaTarihi = sonSertifikaTarihi,
                Arama = arama,
                Sirala = sirala,
                Sayfa = sayfa,
                ToplamSayfa = toplamSayfa,
                ToplamKayit = toplamKayit,
                SayfaBasinaKayit = sayfaBasinaKayit,
                Sertifikalar = sertifikalar
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Indir(int id)
        {
            int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var sertifika = await _context.Sertifikalar
                .AsNoTracking()
                .Include(x => x.Kullanici)
                .Include(x => x.Kurs)
                    .ThenInclude(x => x.Egitmen)
                .FirstOrDefaultAsync(x =>
                    x.SertifikaId == id &&
                    x.KullaniciId == kullaniciId);

            if (sertifika == null)
            {
                TempData["HataMesaji"] = "Sertifika bulunamadı.";
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
}