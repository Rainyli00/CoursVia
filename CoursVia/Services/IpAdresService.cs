using Microsoft.AspNetCore.Http;

namespace CoursVia.Services;

public class IpAdresService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IpAdresService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? IpAdresiGetir()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return null;
        }

        string? forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()
                ?.Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}