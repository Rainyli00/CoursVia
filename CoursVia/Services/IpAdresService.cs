using Microsoft.AspNetCore.Http;

namespace CoursVia.Services;

public class IpAdresService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IpAdresService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Geçerli HTTP isteğini yapan kullanıcının gerçek IP adresini (proxy arkasında olsa bile) tespit eder.
    public string? IpAdresiGetir()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        // Servis HTTP isteği dışında çalışıyorsa IP bilgisi alınamaz.
        if (httpContext == null)
        {
            return null;
        }

        // Proxy veya load balancer arkasında gerçek istemci IP'si X-Forwarded-For içinde gelebilir.
        string? forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            // Header birden fazla IP içerirse ilk değer istemci IP'si kabul edilir.
            return forwardedFor
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()
                ?.Trim();
        }

        // Header yoksa doğrudan bağlantının uzak IP adresi döndürülür.
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
