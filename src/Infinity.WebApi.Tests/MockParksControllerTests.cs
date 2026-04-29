using Infinity.WebApi.Controllers;
using Infinity.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace Infinity.WebApi.Tests;

public class MockParksControllerTests
{
    [Fact]
    public async Task GetParks_ReturnsAllParks()
    {
        var controller = new MockParksController();

        var result = await controller.GetParks();

        var parks = Assert.IsAssignableFrom<IEnumerable<Park>>(result.Value);
        Assert.Equal(3, parks.Count());
    }

    [Fact]
    public async Task GetPark_ReturnsNotFound_WhenParkDoesNotExist()
    {
        var controller = new MockParksController();

        var result = await controller.GetPark("missing");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetPark_ReturnsPark_WhenParkExists()
    {
        var controller = new MockParksController();

        var result = await controller.GetPark("park_mock_1");

        Assert.NotNull(result.Value);
        Assert.Equal("park_mock_1", result.Value.Id);
    }
}
