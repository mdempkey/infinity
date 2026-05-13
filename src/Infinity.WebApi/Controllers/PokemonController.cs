using Infinity.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Infinity.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PokemonController(IPokemonService pokemonService) : ControllerBase
{
    // GET /api/pokemon
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var pokemon = await pokemonService.GetAllAsync();
        return Ok(pokemon);
    }

    // GET /api/pokemon/{name}
    [HttpGet("{name}")]
    public async Task<IActionResult> GetByName(string name)
    {
        var pokemon = await pokemonService.GetByNameAsync(name);
        if (pokemon is null)
            return NotFound(new { error = $"Pokémon '{name}' not found." });

        return Ok(pokemon);
    }

    // GET /api/pokemon/{name}/cry
    [HttpGet("{name}/cry")]
    public async Task<IActionResult> GetCry(string name)
    {
        var stream = await pokemonService.GetCryAsync(name);
        if (stream is null)
            return NotFound(new { error = $"Cry for '{name}' not found." });

        return File(stream, "audio/wav", $"{name}.wav");
    }
}