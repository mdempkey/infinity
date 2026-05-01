using Infinity.WebApi.Data;
using Infinity.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApi.Controllers;

[ApiController]
[Authorize]
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
}
