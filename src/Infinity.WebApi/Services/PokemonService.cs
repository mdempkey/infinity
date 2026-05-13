using System.Net.Http.Json;
using Infinity.WebApi.Models;

namespace Infinity.WebApi.Services;

public class PokemonService(HttpClient httpClient) : IPokemonService
{
    public async Task<IEnumerable<Pokemon>> GetAllAsync()
    {
        var response = await httpClient.GetFromJsonAsync<PokemonListResponse>("/pokemon");
        return response?.Pokemon ?? [];
    }

    public async Task<Pokemon?> GetByNameAsync(string name)
    {
        var response = await httpClient.GetAsync($"/pokemon/{name}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Pokemon>();
    }

    public async Task<Stream?> GetCryAsync(string name)
    {
        var response = await httpClient.GetAsync($"/pokemon/{name}/cry");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }
}