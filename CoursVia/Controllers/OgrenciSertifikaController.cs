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
    // Öğrencinin kazandığı sertifikaları listeleme ve PDF olarak indirme işlemlerini yönetir.
    [Authorize(Roles = "Öğrenci")]
    public class OgrenciSertifikaController : Controller
    {
        private readonly AppDbContext _context;

        public OgrenciSertifikaController(AppDbContext context)
        {
            _context = context;
        }

        // Öğrencinin sertifikalarını arama, sıralama ve sayfalama ile listeler.
        public async Task<IActionResult> Index(string? arama, string sirala = "tarih-desc", int sayfa = 1)
        {
            int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            const int sayfaBasinaKayit = 6;

            arama = arama?.Trim();

            if (sayfa < 1)
            {
                sayfa = 1;
            }

            // Sadece giriş yapan öğrenciye ait sertifikalar alınır.
            var query = _context.Sertifikalar
                .AsNoTracking()
                .Where(x => x.KullaniciId == kullaniciId);

            // Kurs adına veya sertifika koduna göre arama yapılır.
            if (!string.IsNullOrWhiteSpace(arama))
            {
                query = query.Where(x =>
                    x.Kurs.KursAdi.Contains(arama) ||
                    x.SertifikaKodu.Contains(arama));
            }

            // Sertifikalar verilme tarihine göre sıralanır.
            query = sirala switch
            {
                "tarih-asc" => query.OrderBy(x => x.VerilmeTarihi),
                _ => query.OrderByDescending(x => x.VerilmeTarihi)
            };

            int toplamKayit = await query.CountAsync();

            int toplamSayfa = (int)Math.Ceiling(toplamKayit / (double)sayfaBasinaKayit);
            // Sayfa sayısı 1'den az olamaz, bu nedenle toplamSayfa 1 olarak ayarlanır.

            if (toplamSayfa < 1)
            {
                toplamSayfa = 1;
            }
            // Geçerli sayfa, toplam sayfa sayısını aşamaz.
            if (sayfa > toplamSayfa)
            {
                sayfa = toplamSayfa;
            }

            // Sayfada gösterilecek sertifikalar ViewModel'e dönüştürülür.
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

            // Öğrencinin en son aldığı sertifika tarihi bulunur.
            var sonSertifikaTarihi = await _context.Sertifikalar
                .AsNoTracking()
                .Where(x => x.KullaniciId == kullaniciId)
                .OrderByDescending(x => x.VerilmeTarihi)
                .Select(x => (DateTime?)x.VerilmeTarihi)
                .FirstOrDefaultAsync();

            // Sertifika liste ekranında kullanılacak model hazırlanır.
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

        // Öğrencinin seçtiği sertifikayı PDF olarak indirir.
        [HttpGet]
        public async Task<IActionResult> Indir(int id)
        {
            int kullaniciId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Sertifikanın gerçekten giriş yapan öğrenciye ait olup olmadığı kontrol edilir.
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

            // Sertifika bilgilerine göre PDF dosyası oluşturulur.
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

        // Sertifika PDF içeriğini QuestPDF kullanarak oluşturur.
        private static byte[] SertifikaPdfOlustur(
            string ogrenciAdSoyad,
            string kursAdi,
            string egitmenAdSoyad,
            string sertifikaKodu,
            DateTime verilmeTarihi)
        {
            // Sertifika kodunu temsil eden QR kod görseli oluşturulur.
            byte[] qrKodGorseli = QrKodOlustur(sertifikaKodu);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Sertifika yatay A4 formatında hazırlanır.
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

                            // Sertifikanın üst kısmında platform adı, sertifika kodu ve QR kod yer alır.
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

                            // Sertifikanın ana başlığı yazdırılır.
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

                            // Sertifikayı alan öğrencinin adı büyük şekilde gösterilir.
                            column.Item()
                                .PaddingTop(30)
                                .AlignCenter()
                                .Text(ogrenciAdSoyad)
                                .Bold()
                                .FontSize(36)
                                .FontColor(Colors.Black);

                            // Kurs tamamlama açıklaması oluşturulur.
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

                            // Alt bilgi alanında verilme tarihi ve eğitmen adı gösterilir.
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

        // Sertifika kodu için PNG formatında QR kod üretir.
        private static byte[] QrKodOlustur(string sertifikaKodu)
        {
            using var qrGenerator = new QRCodeGenerator();
            // QR kod verisi oluşturulur ve hata düzeltme seviyesi Q olarak ayarlanır.
            using var qrData = qrGenerator.CreateQrCode(
                sertifikaKodu,
                QRCodeGenerator.ECCLevel.Q
            );

            var qrCode = new PngByteQRCode(qrData);

            return qrCode.GetGraphic(20);
        }
    }
}