using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Infinity.WebApplication.Services.Auth;

public sealed class ServiceTokenProvider : IServiceTokenProvider
{
    private readonly IConfiguration _config;
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly Lock _lock = new();

    public ServiceTokenProvider(IConfiguration config)
    {
        _config = config;
    }

    public string GetToken()
    {
        lock (_lock)
        {
            if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry - TimeSpan.FromSeconds(60))
                return _cachedToken;

            var expiryHours = double.Parse(_config["Jwt:ServiceExpiryHours"]
                ?? throw new InvalidOperationException("Jwt:ServiceExpiryHours is not configured."));
            _tokenExpiry = DateTime.UtcNow.AddHours(expiryHours);
            _cachedToken = GenerateToken(_tokenExpiry);
            return _cachedToken;
        }
    }

    private string GenerateToken(DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:ServiceAudience"],
            claims: [new Claim(ClaimTypes.Role, "Service")],
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
