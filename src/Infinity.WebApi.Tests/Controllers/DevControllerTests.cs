using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Infinity.WebApi.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Infinity.WebApi.Tests.Controllers;

public class DevControllerTests
{
    private static DevController CreateController(bool isDevelopment = true)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-signing-key-that-is-long-enough-for-hmac256",
                ["Jwt:Issuer"] = "infinity-web",
                ["Jwt:Audience"] = "infinity-api"
            })
            .Build();

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns(isDevelopment ? "Development" : "Production");

        return new DevController(config, env.Object);
    }

    [Fact]
    public void GetToken_ReturnsOk()
    {
        var result = CreateController().GetToken();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetToken_ReturnsValidJwt()
    {
        var result = (OkObjectResult)CreateController().GetToken();
        var token = Assert.IsType<string>(result.Value);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.NotNull(parsed);
    }

    [Fact]
    public void GetToken_TokenHasServiceAudience()
    {
        var result = (OkObjectResult)CreateController().GetToken();
        var token = new JwtSecurityTokenHandler().ReadJwtToken((string)result.Value!);

        Assert.Contains("infinity-api", token.Audiences);
    }

    [Fact]
    public void GetToken_TokenHasServiceRole()
    {
        var result = (OkObjectResult)CreateController().GetToken();
        var token = new JwtSecurityTokenHandler().ReadJwtToken((string)result.Value!);

        var role = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        Assert.Equal("Service", role?.Value);
    }

    [Fact]
    public void GetToken_TokenExpiresInOneHour()
    {
        var before = DateTime.UtcNow.AddMinutes(59);
        var result = (OkObjectResult)CreateController().GetToken();
        var token = new JwtSecurityTokenHandler().ReadJwtToken((string)result.Value!);
        var after = DateTime.UtcNow.AddMinutes(61);

        Assert.InRange(token.ValidTo, before, after);
    }

    [Fact]
    public void GetToken_ReturnsNotFound_InProduction()
    {
        var result = CreateController(isDevelopment: false).GetToken();

        Assert.IsType<NotFoundResult>(result);
    }
}
