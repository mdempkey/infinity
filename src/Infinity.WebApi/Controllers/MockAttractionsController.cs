using Infinity.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MockAttractionsController : ControllerBase, IAttractionsController
{
    private List<Attraction> attractions;
    private Attraction attractionOne = new Attraction { Id = Guid.NewGuid(), ParkId = "1", Name = "AttractionOne", Description = "Test", Lat = 0, Lng = 0};
    private Attraction attractionTwo = new Attraction { Id = Guid.NewGuid(), ParkId = "1", Name = "AttractionTwo", Description = "Test", Lat = 0, Lng = 0 };
    private Attraction attractionThree = new Attraction { Id = Guid.NewGuid(), ParkId = "1", Name = "AttractionThree", Description = "Test", Lat = 0, Lng = 0 };

    public MockAttractionsController()
    {
        attractions = new List<Attraction>();
        attractions.Add(attractionOne);
        attractions.Add(attractionTwo);
        attractions.Add(attractionThree);
    }
    
    [HttpGet("attractions")]
    public async Task<ActionResult<IEnumerable<Attraction>>> GetAttractions()
    {
        return attractions;
    }

    [HttpGet("attraction/{id}")]
    public async Task<ActionResult<Attraction>> GetAttraction(string id)
    {
        var attraction = attractions.Find(attraction => attraction.Id.ToString() == id);

        if (attraction == null)
        {
            return NotFound();
        }

        return attraction;
    }

    [HttpPost("attraction")]
    public async Task<ActionResult<Attraction>> AddAttraction(Attraction attraction)
    {
        attractions.Add(attraction);
        return CreatedAtAction(nameof(GetAttraction), new { id = attraction.Id }, attraction);
    }

    [HttpPut("attraction/{id}")]
    public async Task<IActionResult> EditAttraction(string id, Attraction attraction)
    {
        if (id != attraction.Id.ToString())
            return BadRequest();

        int attractionIndex = attractions.FindIndex(attr => attr.Id.ToString() == id);
        if (attractionIndex != -1)
            attractions[attractionIndex] = attraction;
        else
            throw new DbUpdateConcurrencyException();

        return NoContent();
    }

    [HttpDelete("attraction/{id}")]
    public async Task<IActionResult> DeleteAttraction(string id)
    {
        var attraction = attractions.Find(attr => attr.Id.ToString() == id);
        if (attraction == null)
            return NotFound();

        attractions.Remove(attraction);

        return NoContent();
    }
}