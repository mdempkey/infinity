using Infinity.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApi.Controllers;

[ApiController]
[Route("api/mock/[controller]")]
public class MockParksController : ControllerBase, IParksController
{
    private List<Park> parks;
    private Park parkOne = new Park { Id = "park_mock_1", Name = "ParkOne", Resort = "TestResort", City = "TestCity", Country = "TestCountry", Lat = 0, Lng = 0};
    private Park parkTwo = new Park { Id = "park_mock_2", Name = "ParkTwo", Resort = "TestResort", City = "TestCity", Country = "TestCountry", Lat = 0, Lng = 0 };
    private Park parkThree = new Park { Id = "park_mock_3", Name = "ParkThree", Resort = "TestResort", City = "TestCity", Country = "TestCountry", Lat = 0, Lng = 0 };

    public MockParksController()
    {
        parks = new List<Park>();
        parks.Add(parkOne);
        parks.Add(parkTwo);
        parks.Add(parkThree);
    }
    
    [HttpGet("")]
    public async Task<ActionResult<IEnumerable<Park>>> GetParks()
    {
        return parks;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Park>> GetPark(string id)
    {
        var park = parks.Find(park => park.Id.ToString() == id);

        if (park == null)
        {
            return NotFound();
        }

        return park;
    }
}