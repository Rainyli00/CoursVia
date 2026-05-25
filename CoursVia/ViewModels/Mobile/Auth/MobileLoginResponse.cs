namespace CoursVia.ViewModels.Mobile.Auth;

public class MobileLoginResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public string? AccessToken { get; set; }

    public DateTime? AccessTokenBitisTarihi { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenBitisTarihi { get; set; }

    public MobileKullaniciResponse? Kullanici { get; set; }
}

public class MobileRefreshTokenResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public string? AccessToken { get; set; }

    public DateTime? AccessTokenBitisTarihi { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenBitisTarihi { get; set; }
}

public class MobileKullaniciResponse
{
    public int KullaniciId { get; set; }

    public string AdSoyad { get; set; } = string.Empty;

    public string Eposta { get; set; } = string.Empty;

    public string? ProfilFotoUrl { get; set; }

    public List<string> Roller { get; set; } = new();
}

public class MobileAuthIslemResponse
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;
}