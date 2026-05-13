using Infinity.WebApplication.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace Infinity.WebApplication.Controllers;

[ApiController]
[Route("api/pokemon")]
public sealed class PokemonController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PokemonOptionDto>>> GetAll(CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("InfinityApi");
        var endpoint = new Uri(client.BaseAddress!, "api/pokemon");

        try
        {
            var response = await client.GetAsync("api/pokemon", cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new
                {
                    error = "Unable to load Pokémon from the API.",
                    attemptedEndpoint = endpoint.ToString(),
                    statusCode = (int)response.StatusCode,
                    apiResponse = responseText
                });
            }

            var pokemon = await response.Content.ReadFromJsonAsync<List<PokemonOptionDto>>(
                cancellationToken: cancellationToken);

            return Ok((pokemon ?? []).OrderBy(p => p.Id).ToList());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "The Pokémon proxy failed.",
                attemptedEndpoint = endpoint.ToString(),
                exception = ex.Message
            });
        }
    }
}