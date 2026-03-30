using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace FiresideEditorial.Services;

public class AdminAuthService
{
    private readonly IConfiguration _config;

    public AdminAuthService(IConfiguration config)
    {
        _config = config;
    }

    public bool ValidateCredentials(string username, string password)
    {
        var adminUser = _config["Admin:Username"] ?? "admin";
        var adminPass = _config["Admin:Password"] ?? "fireside2026";
        return username == adminUser && password == adminPass;
    }

    public ClaimsPrincipal CreatePrincipal(string username)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
