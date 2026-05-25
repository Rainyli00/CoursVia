namespace CoursVia.ViewModels.Mobile.Auth;

public class MobileLoginRequest
{
    public string Eposta { get; set; } = string.Empty;

    public string Sifre { get; set; } = string.Empty;
}

public class MobileRefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class MobileLogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class MobileOnlineDurumRequest
{
    public bool OnlineMi { get; set; }
}