using Infinity.WebApi.Controllers;
using Infinity.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Infinity.WebApi.Tests;

public class MockAttractionsControllerTests
{
    [Fact]
    public async Task GetAttractions_ReturnsAllAttractions()
    {
        var controller = new MockAttractionsController();

        var result = await controller.GetAttractions();

        var attractions = Assert.IsAssignableFrom<IEnumerable<Attraction>>(result.Value);
        Assert.Equal(3, attractions.Count());
    }

    [Fact]
    public async Task GetAttraction_ReturnsNotFound_WhenAttractionDoesNotExist()
    {
        var controller = new MockAttractionsController();

        var result = await controller.GetAttraction("missing");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetAttraction_ReturnsAttraction_WhenAttractionExists()
    {
        var controller = new MockAttractionsController();

        var result = await controller.GetAttraction("1");

        Assert.NotNull(result.Value);
        Assert.Equal("1", result.Value.Id);
    }
}