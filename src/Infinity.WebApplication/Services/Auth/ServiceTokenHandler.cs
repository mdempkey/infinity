using System.Net.Http.Headers;

namespace Infinity.WebApplication.Services.Auth;

public sealed class ServiceTokenHandler : DelegatingHandler
{
    private readonly IServiceTokenProvider _tokenProvider;

    public ServiceTokenHandler(IServiceTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenProvider.GetToken());
        return base.SendAsync(request, ct);
    }
}
