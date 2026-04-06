using Infinity.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MockParksController : ControllerBase, IParksController
{
    private List<Park> parks;
    private Park parkOne = new Park { Id = Guid.NewGuid(), Name = "ParkOne", Resort = "TestResort", City = "TestCity", Country = "TestCountry", Lat = 0, Lng = 0};
    private Park parkTwo = new Park { Id = Guid.NewGuid(), Name = "ParkTwo", Resort = "TestResort", City = "TestCity", Country = "TestCountry", Lat = 0, Lng = 0 };
    private Park parkThree = new Park { Id = Guid.NewGuid(), Name = "ParkThree", Resort = "TestResort", City = "TestCity", Country = "TestCountry", Lat = 0, Lng = 0 };

    public MockParksController()
    {
        parks = new List<Park>();
        parks.Add(parkOne);
        parks.Add(parkTwo);
        parks.Add(parkThree);
    }
    
    [HttpGet("parks")]
    public async Task<ActionResult<IEnumerable<Park>>> GetParks()
    {
        return parks;
    }

    [HttpGet("park/{id}")]
    public async Task<ActionResult<Park>> GetPark(string id)
    {
        var park = parks.Find(park => park.Id.ToString() == id);

        if (park == null)
        {
            return NotFound();
        }

        return park;
    }

    [HttpPost("park")]
    public async Task<ActionResult<Park>> AddPark(Park park)
    {
        parks.Add(park);
        return CreatedAtAction(nameof(GetPark), new { id = park.Id }, park);
    }

    [HttpPut("park/{id}")]
    public async Task<IActionResult> EditPark(string id, Park park)
    {
        if (id != park.Id.ToString())
            return BadRequest();

        int parkIndex = parks.FindIndex(p => p.Id.ToString() == id);
        if (parkIndex != -1)
            parks[parkIndex] = park;
        else
            throw new DbUpdateConcurrencyException();

        return NoContent();
    }

    [HttpDelete("park/{id}")]
    public async Task<IActionResult> DeletePark(string id)
    {
        var park = parks.Find(p => p.Id.ToString() == id);
        if (park == null)
            return NotFound();

        parks.Remove(park);

        return NoContent();
    }
}