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
            CreateAttraction("space-mountain"),
            CreateAttraction("pirates"));
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

    [Fact]
    public async Task AddAttraction_PersistsAttraction_AndReturnsCreatedAtAction()
    {
        await using var context = CreateContext();
        var controller = new AttractionsController(context);
        var attraction = CreateAttraction("haunted-mansion");

        var result = await controller.AddAttraction(attraction);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(AttractionsController.GetAttraction), created.ActionName);
        Assert.Equal("haunted-mansion", created.RouteValues?["id"]);
        Assert.Equal(1, await context.Attractions.CountAsync());
    }

    [Fact]
    public async Task EditAttraction_ReturnsBadRequest_WhenRouteIdDoesNotMatchBody()
    {
        await using var context = CreateContext();
        var controller = new AttractionsController(context);

        var result = await controller.EditAttraction("space-mountain", CreateAttraction("pirates"));

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task EditAttraction_ReturnsNotFound_WhenAttractionDoesNotExist()
    {
        await using var context = CreateContext();
        var controller = new AttractionsController(context);

        var result = await controller.EditAttraction("missing", CreateAttraction("missing"));

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteAttraction_RemovesAttraction_AndReturnsNoContent()
    {
        await using var context = CreateContext();
        context.Attractions.Add(CreateAttraction("big-thunder"));
        await context.SaveChangesAsync();
        var controller = new AttractionsController(context);

        var result = await controller.DeleteAttraction("big-thunder");

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(await context.Attractions.ToListAsync());
    }

    private static LocationsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LocationsDbContext(options);
    }

    private static Attraction CreateAttraction(string id)
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
