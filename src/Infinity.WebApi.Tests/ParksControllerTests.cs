using Infinity.WebApi.Controllers;
using Infinity.WebApi.Data;
using Infinity.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApi.Tests;

public class ParksControllerTests
{
    [Fact]
    public async Task GetParks_ReturnsAllParks()
    {
        await using var context = CreateContext();
        context.Parks.AddRange(
            CreateMockPark("magic-kingdom"),
            CreateMockPark("epcot"));
        await context.SaveChangesAsync();

        var controller = new ParksController(context);

        var result = await controller.GetParks();

        var parks = Assert.IsAssignableFrom<IEnumerable<Park>>(result.Value);
        Assert.Equal(2, parks.Count());
    }

    [Fact]
    public async Task GetPark_ReturnsNotFound_WhenParkDoesNotExist()
    {
        await using var context = CreateContext();
        var controller = new ParksController(context);

        var result = await controller.GetPark("missing");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static LocationsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LocationsDbContext(options);
    }

    private static Park CreateMockPark(string id)
    {
        return new Park
        {
            Id = id,
            Name = $"Park {id}",
            Resort = "Walt Disney World",
            City = "Orlando",
            Country = "USA"
        };
    }
}
