using CoursVia.Data;
using CoursVia.Models;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Services;

public class AdminLogService
{
    // Admin loglarında kullanılan işlem tipi adları.
    public const string KullaniciIslemleri = "Kullanıcı İşlemleri";
    public const string EgitmenBasvurulari = "Eğitmen Başvuruları";
    public const string KursOnaylari = "Kurs Onayları";
    public const string KursIslemleri = "Kurs İşlemleri";
    public const string YorumIslemleri = "Yorum İşlemleri";
    public const string KategoriIslemleri = "Kategori İşlemleri";
    public const string SistemKullanici = "Sistem / Kullanıcı";

    private readonly AppDbContext _context;
    private readonly IpAdresService _ipAdresService;

    public AdminLogService(AppDbContext context, IpAdresService ipAdresService)
    {
        _context = context;
        _ipAdresService = ipAdresService;
    }
    // Admin log kaydı oluşturur, eğer işlem tipi tabloda yoksa otomatik ekler.
    public async Task KaydetAsync(int? adminId, string islemTipiAdi, string? aciklama)
    {
        // İşlem tipi olmadan anlamlı log üretilemeyeceği için kayıt yapılmaz.
        if (string.IsNullOrWhiteSpace(islemTipiAdi))
        {
            return;
        }

        islemTipiAdi = islemTipiAdi.Trim();

        // İşlem tipi daha önce oluşturulmamışsa otomatik eklenir.
        var islemTipi = await _context.IslemTipleri
            .FirstOrDefaultAsync(x => x.IslemTipAdi == islemTipiAdi);

        if (islemTipi == null)
        {
            islemTipi = new IslemTipi
            {
                IslemTipAdi = islemTipiAdi
            };

            _context.IslemTipleri.Add(islemTipi);
        }

        // SaveChanges burada çağrılmaz; log genelde ana işlemle aynı transaction içinde kaydedilir.
        _context.AdminLoglari.Add(new AdminLog
        {
            AdminId = adminId,
            IslemTipi = islemTipi,
            Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim(),
            IpAdresi = _ipAdresService.IpAdresiGetir(),
            IslemTarihi = DateTime.Now
        });
    }
}
