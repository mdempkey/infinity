using Infinity.WebApplication.Data;
using Infinity.WebApplication.Models;
using Infinity.WebApplication.Services.RatingService;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Tests.Services;

public class RatingServiceTests
{
    private UserDbContext _db = null!;
    private IRatingService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new UserDbContext(
            new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        _sut = new RatingService(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<User> SeedUserAsync(string suffix = "")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"testuser{suffix}",
            Email = $"test{suffix}@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Test]
    public async Task UpsertAsync_NewRating_CreatesAndReturnsRating()
    {
        var user = await SeedUserAsync();
        var attractionId = Guid.NewGuid();

        var result = await _sut.UpsertAsync(user.Id, attractionId, 4);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo(user.Id));
        Assert.That(result.AttractionId, Is.EqualTo(attractionId));
        Assert.That(result.Value, Is.EqualTo(4));
        Assert.That(await _db.Ratings.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task UpsertAsync_ExistingRating_UpdatesValueWithoutInsertingNewRow()
    {
        var user = await SeedUserAsync();
        var attractionId = Guid.NewGuid();
        await _sut.UpsertAsync(user.Id, attractionId, 3);

        var result = await _sut.UpsertAsync(user.Id, attractionId, 5);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(5));
        Assert.That(await _db.Ratings.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task UpsertAsync_ValueBelowZero_ThrowsArgumentException()
    {
        var user = await SeedUserAsync();

        Assert.ThrowsAsync<ArgumentException>(() => _sut.UpsertAsync(user.Id, Guid.NewGuid(), -1));
        Assert.That(await _db.Ratings.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task UpsertAsync_ValueAboveFive_ThrowsArgumentException()
    {
        var user = await SeedUserAsync();

        Assert.ThrowsAsync<ArgumentException>(() => _sut.UpsertAsync(user.Id, Guid.NewGuid(), 6));
        Assert.That(await _db.Ratings.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task UpsertAsync_ValueOfZero_CreatesRating()
    {
        var user = await SeedUserAsync();

        var result = await _sut.UpsertAsync(user.Id, Guid.NewGuid(), 0);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(0));
    }

    [Test]
    public async Task UpsertAsync_ValueOfFive_CreatesRating()
    {
        var user = await SeedUserAsync();

        var result = await _sut.UpsertAsync(user.Id, Guid.NewGuid(), 5);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo(5));
    }

    [Test]
    public async Task GetAttractionAverageAsync_WithRatings_ReturnsAverage()
    {
        var user1 = await SeedUserAsync("1");
        var user2 = await SeedUserAsync("2");
        var attractionId = Guid.NewGuid();

        await _sut.UpsertAsync(user1.Id, attractionId, 4);
        await _sut.UpsertAsync(user2.Id, attractionId, 2);

        var result = await _sut.GetAttractionAverageAsync(attractionId);

        Assert.That(result, Is.EqualTo(3.0));
    }

    [Test]
    public async Task GetAttractionAverageAsync_WithNoRatings_ReturnsNull()
    {
        var result = await _sut.GetAttractionAverageAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetParkAverageAsync_ReturnsAverageOfAttractionAverages()
    {
        var user1 = await SeedUserAsync("1");
        var user2 = await SeedUserAsync("2");
        var attraction1 = Guid.NewGuid();
        var attraction2 = Guid.NewGuid();

        // attraction1 average = 4, attraction2 average = 2 → park average = 3
        await _sut.UpsertAsync(user1.Id, attraction1, 4);
        await _sut.UpsertAsync(user2.Id, attraction2, 2);

        var result = await _sut.GetParkAverageAsync([attraction1, attraction2]);

        Assert.That(result, Is.EqualTo(3.0));
    }

    [Test]
    public async Task GetParkAverageAsync_ExcludesUnratedAttractions()
    {
        var user = await SeedUserAsync();
        var ratedAttraction = Guid.NewGuid();
        var unratedAttraction = Guid.NewGuid();

        await _sut.UpsertAsync(user.Id, ratedAttraction, 4);

        var result = await _sut.GetParkAverageAsync([ratedAttraction, unratedAttraction]);

        Assert.That(result, Is.EqualTo(4.0));
    }

    [Test]
    public async Task GetParkAverageAsync_WithNoRatingsForAnyAttraction_ReturnsNull()
    {
        var result = await _sut.GetParkAverageAsync([Guid.NewGuid(), Guid.NewGuid()]);

        Assert.That(result, Is.Null);
    }
}
