using CoursVia.Data;
using CoursVia.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoursVia.Controllers;

// Eğitmen controller'larında ortak kullanılan özellik ve yardımcı metotları barındırır.
public abstract class EgitmenBaseController : Controller
{
    protected readonly AppDbContext _context;

    protected EgitmenBaseController(AppDbContext context)
    {
        _context = context;
    }

    // Giriş yapan aktif kullanıcının Id bilgisini claim üzerinden alır.
    protected int AktifKullaniciId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Kursun eğitmen tarafından düzenlenebilir durumda olup olmadığını kontrol eder.
    protected bool KursDuzenlenebilirMi(int durumId)
    {
        return durumId == 3 || // Taslak
               durumId == 5 || // Yayında
               durumId == 6 || // Reddedildi
               durumId == 7;   // Düzeltme İsteniyor
    }

    // Yayındaki bir kurs düzenlenirse tekrar taslak durumuna alınır.
    protected void OnayliKursuTaslakYap(Kurs kurs)
    {
        if (kurs.DurumId == 5)
        {
            kurs.DurumId = 3;
            kurs.GuncellemeTarihi = DateTime.Now;
        }
    }
}