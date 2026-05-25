using CoursVia.Models;
using Microsoft.EntityFrameworkCore;

namespace CoursVia.Data.Seed;

public class DemoKategoriSeeder
{
    private readonly AppDbContext _context;

    public DemoKategoriSeeder(AppDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        var kategoriler = new List<string>
        {
            "Yazılım Geliştirme",
            "Web Tasarım",
            "Veri Bilimi",
            "Yapay Zeka",
            "Dil Eğitimi",
            "Kişisel Gelişim",
            "Finans",
            "Pazarlama",
            "Tasarım",
            "Girişimcilik",
            "Matematik",
            "Diğer"
        };

        var mevcutKategoriAdlari = await _context.Kategoriler
            .Select(x => x.KategoriAdi)
            .ToListAsync();

        foreach (var kategoriAdi in kategoriler)
        {
            bool varMi = mevcutKategoriAdlari
                .Any(x => x.Trim().ToLower() == kategoriAdi.Trim().ToLower());

            if (varMi)
            {
                continue;
            }

            _context.Kategoriler.Add(new Kategori
            {
                KategoriAdi = kategoriAdi
            });
        }

        await _context.SaveChangesAsync();
    }
}