namespace Infinity.WebApi.Models;

public class Pokemon
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Cry { get; set; } = string.Empty;
}

public class PokemonListResponse
{
    public List<Pokemon> Pokemon { get; set; } = [];
}