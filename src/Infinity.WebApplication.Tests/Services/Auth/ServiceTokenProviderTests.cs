using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Infinity.WebApplication.Services.Auth;
using Microsoft.Extensions.Configuration;

namespace Infinity.WebApplication.Tests.Services.Auth;

public class ServiceTokenProviderTests
{
    private const string TestKey = "test-signing-key-that-is-long-enough-32chars!";
    private const string TestIssuer = "infinity-web";
    private const string TestAudience = "infinity-api";

    private static IConfiguration BuildConfig(double expiryHours = 1) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = TestKey,
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:ServiceAudience"] = TestAudience,
                ["Jwt:ServiceExpiryHours"] = expiryHours.ToString()
            })
            .Build();

    [Test]
    public void GetToken_ReturnsReadableJwt()
    {
        var provider = new ServiceTokenProvider(BuildConfig());

        var token = provider.GetToken();

        Assert.That(new JwtSecurityTokenHandler().CanReadToken(token), Is.True);
    }

    [Test]
    public void GetToken_TokenHasServiceRoleClaim()
    {
        var provider = new ServiceTokenProvider(BuildConfig());

        var token = provider.GetToken();
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(parsed.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Service"), Is.True);
    }

    [Test]
    public void GetToken_TokenHasCorrectAudience()
    {
        var provider = new ServiceTokenProvider(BuildConfig());

        var token = provider.GetToken();
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.That(parsed.Audiences, Contains.Item(TestAudience));
    }

    [Test]
    public void GetToken_ReturnsCachedTokenOnSubsequentCalls()
    {
        var provider = new ServiceTokenProvider(BuildConfig());

        var first = provider.GetToken();
        var second = provider.GetToken();

        Assert.That(second, Is.EqualTo(first));
    }
}
