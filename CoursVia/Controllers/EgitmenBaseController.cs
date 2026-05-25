using CoursVia.Data;
using CoursVia.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoursVia.Controllers;

public abstract class EgitmenBaseController : Controller
{
    protected readonly AppDbContext _context;

    protected EgitmenBaseController(AppDbContext context)
    {
        _context = context;
    }

    protected int AktifKullaniciId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    protected bool KursDuzenlenebilirMi(int durumId)
    {
        return durumId == 3 || // Taslak
               durumId == 5 || // Yayında
               durumId == 6 || // Reddedildi
               durumId == 7;   // Düzeltme İsteniyor
    }

    protected void OnayliKursuTaslakYap(Kurs kurs)
    {
        if (kurs.DurumId == 5)
        {
            kurs.DurumId = 3;
            kurs.GuncellemeTarihi = DateTime.Now;
        }
    }
}
