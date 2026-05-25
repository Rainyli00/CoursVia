namespace CoursVia.ViewModels.Mobile.Auth;

public class MobileSifremiUnuttumRequestDto
{
    public string Eposta { get; set; } = string.Empty;
}

public class MobileSifremiUnuttumResponseDto
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;

    public string? Eposta { get; set; }

    public bool KodGonderildiMi { get; set; }
}

public class MobileSifreSifirlaRequestDto
{
    public string Eposta { get; set; } = string.Empty;

    public string Kod { get; set; } = string.Empty;

    public string YeniSifre { get; set; } = string.Empty;
}

public class MobileSifreSifirlaResponseDto
{
    public bool Basarili { get; set; }

    public string Mesaj { get; set; } = string.Empty;
}