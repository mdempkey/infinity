using Infinity.WebApi.Data;
using Infinity.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParksController : ControllerBase, IParksController
{
    private readonly LocationsDbContext _context;

    public ParksController(LocationsDbContext context)
    {
        _context = context;
    }

    [HttpGet("")]
    public async Task<ActionResult<IEnumerable<Park>>> GetParks()
    {
        return await _context.Parks.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Park>> GetPark(string id)
    {
        var park = await _context.Parks.FindAsync(id);

        if (park == null)
        {
            return NotFound();
        }

        return park;
    }

    [HttpPost("")]
    public async Task<ActionResult<Park>> AddPark(Park park)
    {
        _context.Parks.Add(park);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPark), new { id = park.Id }, park);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditPark(string id, Park park)
    {
        if (id != park.Id.ToString()) return BadRequest();

        _context.Entry(park).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Parks.Any(u => u.Id.ToString() == id)) return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePark(string id)
    {
        var park = await _context.Parks.FindAsync(id);
        if (park == null) return NotFound();

        _context.Parks.Remove(park);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
