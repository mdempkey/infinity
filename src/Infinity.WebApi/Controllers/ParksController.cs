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
}
