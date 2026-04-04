using Infinity.WebApi.Data;
using Infinity.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttractionsController : ControllerBase, IAttractionsController
{
    private readonly LocationsDbContext _context;

    public AttractionsController(LocationsDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<ActionResult<IEnumerable<Attraction>>> GetAttractions()
    {
        return await _context.Attractions.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Attraction>> GetAttraction(string id)
    {
        var attraction = await _context.Attractions.FindAsync(id);

        if (attraction == null)
        {
            return NotFound();
        }

        return attraction;
    }

    [HttpPost("")]
    public async Task<ActionResult<Attraction>> AddAttraction(Attraction attraction)
    {
        _context.Attractions.Add(attraction);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAttraction), new { id = attraction.Id }, attraction);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditAttraction(string id, Attraction attraction)
    {
        if (id != attraction.Id.ToString()) return BadRequest();

        _context.Entry(attraction).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Attractions.Any(u => u.Id.ToString() == id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAttraction(string id)
    {
        var attraction = await _context.Attractions.FindAsync(id);
        if (attraction == null) return NotFound();

        _context.Attractions.Remove(attraction);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
