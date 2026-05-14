using System.Net;
using System.Net.Http.Headers;
using Infinity.WebApplication.Controllers;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace Infinity.WebApplication.Tests.Controllers;

internal sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(response);
}

internal sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => client;
}

public class PokemonControllerTests
{
    private static PokemonController CreateSut(HttpResponseMessage upstreamResponse)
    {
        var handler = new FakeHttpMessageHandler(upstreamResponse);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        return new PokemonController(new FakeHttpClientFactory(client));
    }

    [Test]
    public async Task GetCry_ReturnsFileResult_WhenUpstreamSucceeds()
    {
        var upstream = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        };
        upstream.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");

        var result = await CreateSut(upstream).GetCry("pikachu", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<FileContentResult>());
    }

    [Test]
    public async Task GetCry_ContentTypeMatchesUpstream()
    {
        var upstream = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        };
        upstream.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/ogg");

        var result = await CreateSut(upstream).GetCry("pikachu", CancellationToken.None) as FileContentResult;

        Assert.That(result!.ContentType, Is.EqualTo("audio/ogg"));
    }

    [Test]
    public async Task GetCry_UsesDefaultContentType_WhenUpstreamHasNoContentType()
    {
        var upstream = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        };
        // deliberately do NOT set Content-Type header

        var result = await CreateSut(upstream).GetCry("pikachu", CancellationToken.None) as FileContentResult;

        Assert.That(result!.ContentType, Is.EqualTo("audio/wav"));
    }

    [Test]
    public async Task GetCry_Returns404_WhenUpstreamReturns404()
    {
        var result = await CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound))
            .GetCry("missingmon", CancellationToken.None);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task GetCry_Returns502_WhenUpstreamReturnsError()
    {
        var result = await CreateSut(new HttpResponseMessage(HttpStatusCode.InternalServerError))
            .GetCry("pikachu", CancellationToken.None) as StatusCodeResult;

        Assert.That(result?.StatusCode, Is.EqualTo(502));
    }
}
