using Infinity.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApi.Controllers;

[ApiController]
[Route("api/mock/[controller]")]
public class MockAttractionsController : ControllerBase, IAttractionsController
{
    private List<Attraction> attractions;
    private Attraction attractionOne = new Attraction { Id = "1", ParkId = "park_mock_1", Name = "AttractionOne", Description = "Test", Lat = 0, Lng = 0};
    private Attraction attractionTwo = new Attraction { Id = "2", ParkId = "park_mock_1", Name = "AttractionTwo", Description = "Test", Lat = 0, Lng = 0 };
    private Attraction attractionThree = new Attraction { Id = "3", ParkId = "park_mock_1", Name = "AttractionThree", Description = "Test", Lat = 0, Lng = 0 };

    public MockAttractionsController()
    {
        attractions = new List<Attraction>();
        attractions.Add(attractionOne);
        attractions.Add(attractionTwo);
        attractions.Add(attractionThree);
    }
    
    [HttpGet("")]
    public async Task<ActionResult<IEnumerable<Attraction>>> GetAttractions()
    {
        return attractions;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Attraction>> GetAttraction(string id)
    {
        var attraction = attractions.Find(attraction => attraction.Id == id);

        if (attraction == null)
        {
            return NotFound();
        }

        return attraction;
    }
}