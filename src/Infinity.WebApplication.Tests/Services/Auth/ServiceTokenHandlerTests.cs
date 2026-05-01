using System.Net;
using Infinity.WebApplication.Services.Auth;

namespace Infinity.WebApplication.Tests.Services.Auth;

public class ServiceTokenHandlerTests
{
    private sealed class StubTokenProvider : IServiceTokenProvider
    {
        public string GetToken() => "test-token-value";
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Test]
    public async Task SendAsync_AttachesBearerTokenToRequest()
    {
        var capturing = new CapturingHandler();
        var handler = new ServiceTokenHandler(new StubTokenProvider())
        {
            InnerHandler = capturing
        };
        var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/api/parks");

        var auth = capturing.LastRequest!.Headers.Authorization;
        Assert.That(auth!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(auth.Parameter, Is.EqualTo("test-token-value"));
    }
}
