namespace CoursVia.Models;

public class Rol
{
    public int RolId { get; set; }

    public string RolAdi { get; set; } = null!;

    // Navigation Properties
    public ICollection<KullaniciRol> KullaniciRolleri { get; set; } = new List<KullaniciRol>();
}