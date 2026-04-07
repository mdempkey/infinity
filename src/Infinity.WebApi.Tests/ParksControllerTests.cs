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
            CreatePark("magic-kingdom"),
            CreatePark("epcot"));
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

    [Fact]
    public async Task AddPark_PersistsPark_AndReturnsCreatedAtAction()
    {
        await using var context = CreateContext();
        var controller = new ParksController(context);
        var park = CreatePark("animal-kingdom");

        var result = await controller.AddPark(park);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(ParksController.GetPark), created.ActionName);
        Assert.Equal("animal-kingdom", created.RouteValues?["id"]);
        Assert.Equal(1, await context.Parks.CountAsync());
    }

    [Fact]
    public async Task EditPark_ReturnsBadRequest_WhenRouteIdDoesNotMatchBody()
    {
        await using var context = CreateContext();
        var controller = new ParksController(context);

        var result = await controller.EditPark("magic-kingdom", CreatePark("epcot"));

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task EditPark_ReturnsNotFound_WhenParkDoesNotExist()
    {
        await using var context = CreateContext();
        var controller = new ParksController(context);

        var result = await controller.EditPark("missing", CreatePark("missing"));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeletePark_RemovesPark_AndReturnsNoContent()
    {
        await using var context = CreateContext();
        context.Parks.Add(CreatePark("hollywood-studios"));
        await context.SaveChangesAsync();
        var controller = new ParksController(context);

        var result = await controller.DeletePark("hollywood-studios");

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(await context.Parks.ToListAsync());
    }

    private static LocationsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LocationsDbContext(options);
    }

    private static Park CreatePark(string id)
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
