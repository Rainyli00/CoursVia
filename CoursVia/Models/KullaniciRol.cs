namespace CoursVia.Models;

public class KullaniciRol
{
    public int KullaniciRolId { get; set; }

    public int KullaniciId { get; set; }
    public int RolId { get; set; }

    // Navigation Properties
    public Kullanici Kullanici { get; set; } = null!;
    public Rol Rol { get; set; } = null!;
}