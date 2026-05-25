using Microsoft.EntityFrameworkCore;

namespace CoursVia.Data.Seed;

public class DemoDataSeeder
{
    private readonly AppDbContext _context;
    private readonly DemoKategoriSeeder _kategoriSeeder;
    private readonly DemoKullaniciSeeder _kullaniciSeeder;
    private readonly DemoKursSeeder _kursSeeder;
    private readonly DemoOgrenciHareketSeeder _ogrenciHareketSeeder;
    private readonly DemoSinavSeeder _sinavSeeder;
    private readonly DemoSistemSeeder _sistemSeeder;
    private readonly DemoOturumSeeder _oturumSeeder;

    public DemoDataSeeder(
        AppDbContext context,
        DemoKategoriSeeder kategoriSeeder,
        DemoKullaniciSeeder kullaniciSeeder,
        DemoKursSeeder kursSeeder,
        DemoOgrenciHareketSeeder ogrenciHareketSeeder,
        DemoSinavSeeder sinavSeeder,
        DemoSistemSeeder sistemSeeder,
        DemoOturumSeeder oturumSeeder)
    {
        _context = context;
        _kategoriSeeder = kategoriSeeder;
        _kullaniciSeeder = kullaniciSeeder;
        _kursSeeder = kursSeeder;
        _ogrenciHareketSeeder = ogrenciHareketSeeder;
        _sinavSeeder = sinavSeeder;
        _sistemSeeder = sistemSeeder;
        _oturumSeeder = oturumSeeder;
    }

    public async Task SeedAsync()
    {
        await _context.Database.MigrateAsync();

        await _kategoriSeeder.SeedAsync();
        await _kullaniciSeeder.SeedAsync();
        await _kursSeeder.SeedAsync();
        await _ogrenciHareketSeeder.SeedAsync();
        await _sinavSeeder.SeedAsync();
        await _sistemSeeder.SeedAsync();
        await _oturumSeeder.SeedAsync();
    }
}