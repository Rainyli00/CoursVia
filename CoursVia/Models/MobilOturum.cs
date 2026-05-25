namespace CoursVia.Models;

public class MobilOturum
{
    public int MobilOturumId { get; set; }

    public int KullaniciId { get; set; }

    // Refresh token düz metin olarak değil, hash olarak tutulur.
    public string RefreshTokenHash { get; set; } = null!;

    public DateTime RefreshTokenBitisTarihi { get; set; }

    public DateTime OlusturmaTarihi { get; set; }

    public bool AktifMi { get; set; } = true;

    // Navigation Properties
    public Kullanici Kullanici { get; set; } = null!;
}