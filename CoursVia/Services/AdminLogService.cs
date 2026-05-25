using CoursVia.Data;
using CoursVia.Models;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Services;

public class AdminLogService
{
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

    public async Task KaydetAsync(int? adminId, string islemTipiAdi, string? aciklama)
    {
        if (string.IsNullOrWhiteSpace(islemTipiAdi))
        {
            return;
        }

        islemTipiAdi = islemTipiAdi.Trim();

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
