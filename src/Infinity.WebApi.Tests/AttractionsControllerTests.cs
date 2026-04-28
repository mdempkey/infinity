using Infinity.WebApi.Controllers;
using Infinity.WebApi.Data;
using Infinity.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApi.Tests;

public class AttractionsControllerTests
{
    [Fact]
    public async Task GetAttractions_ReturnsAllAttractions()
    {
        await using var context = CreateContext();
        context.Attractions.AddRange(
            CreateMockAttraction("space-mountain"),
            CreateMockAttraction("pirates"));
        await context.SaveChangesAsync();

        var controller = new AttractionsController(context);

        var result = await controller.GetAttractions();

        var attractions = Assert.IsAssignableFrom<IEnumerable<Attraction>>(result.Value);
        Assert.Equal(2, attractions.Count());
    }

    [Fact]
    public async Task GetAttraction_ReturnsNotFound_WhenAttractionDoesNotExist()
    {
        await using var context = CreateContext();
        var controller = new AttractionsController(context);

        var result = await controller.GetAttraction("missing");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static LocationsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LocationsDbContext(options);
    }

    private static Attraction CreateMockAttraction(string id)
    {
        return new Attraction
        {
            Id = id,
            ParkId = "magic-kingdom",
            Name = $"Attraction {id}",
            Description = "Test attraction"
        };
    }
}
