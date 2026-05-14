using System.Text.Json.Serialization;

namespace Infinity.WebApplication.Models;

public sealed record PokemonOptionDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("cry")] string Cry);

public sealed record PokemonListResponse(
    [property: JsonPropertyName("pokemon")] List<PokemonOptionDto> Pokemon);