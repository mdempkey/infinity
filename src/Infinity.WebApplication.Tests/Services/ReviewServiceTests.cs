using Infinity.WebApplication.Data;
using Infinity.WebApplication.Models;
using Infinity.WebApplication.Services.ReviewService;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Tests.Services;

public class ReviewServiceTests
{
    private UserDbContext _db = null!;
    private IReviewService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new UserDbContext(
            new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        _sut = new ReviewService(_db);
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
    public async Task AddAsync_InsertsAndReturnsReview()
    {
        var user = await SeedUserAsync();
        var attractionId = Guid.NewGuid();

        var result = await _sut.AddAsync(user.Id, attractionId, "Great ride!");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo(user.Id));
        Assert.That(result.AttractionId, Is.EqualTo(attractionId));
        Assert.That(result.Content, Is.EqualTo("Great ride!"));
        Assert.That(await _db.Reviews.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task AddAsync_SameUserSameAttraction_AllowsMultipleReviews()
    {
        var user = await SeedUserAsync();
        var attractionId = Guid.NewGuid();

        await _sut.AddAsync(user.Id, attractionId, "First review");
        await _sut.AddAsync(user.Id, attractionId, "Second review");

        Assert.That(await _db.Reviews.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task AddAsync_WithContentExceedingMaxLength_ThrowsArgumentException()
    {
        var user = await SeedUserAsync();

        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.AddAsync(user.Id, Guid.NewGuid(), new string('a', 2001)));
    }

    [Test]
    public async Task AddAsync_WithWhitespaceContent_ThrowsArgumentException()
    {
        var user = await SeedUserAsync();

        Assert.ThrowsAsync<ArgumentException>(() => _sut.AddAsync(user.Id, Guid.NewGuid(), "   "));
    }

    [Test]
    public async Task GetByAttractionAsync_ReturnsOnlyMatchingAttractionReviews()
    {
        var user = await SeedUserAsync();
        var targetAttractionId = Guid.NewGuid();
        var otherAttractionId = Guid.NewGuid();

        await _sut.AddAsync(user.Id, targetAttractionId, "For target");
        await _sut.AddAsync(user.Id, targetAttractionId, "Also for target");
        await _sut.AddAsync(user.Id, otherAttractionId, "For other");

        var result = (await _sut.GetByAttractionAsync(targetAttractionId)).ToList();

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.All(r => r.AttractionId == targetAttractionId), Is.True);
    }

    [Test]
    public async Task GetByAttractionAsync_WithNoReviews_ReturnsEmptyCollection()
    {
        var result = await _sut.GetByAttractionAsync(Guid.NewGuid());

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task EditAsync_OwnReview_UpdatesContentAndReturnsReview()
    {
        var user = await SeedUserAsync();
        var review = await _sut.AddAsync(user.Id, Guid.NewGuid(), "Original");

        var result = await _sut.EditAsync(review.Id, user.Id, "Updated");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Content, Is.EqualTo("Updated"));
        Assert.That((await _db.Reviews.FindAsync(review.Id))!.Content, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task EditAsync_AnotherUsersReview_ReturnsNull()
    {
        var owner = await SeedUserAsync("owner");
        var other = await SeedUserAsync("other");
        var review = await _sut.AddAsync(owner.Id, Guid.NewGuid(), "Original");

        var result = await _sut.EditAsync(review.Id, other.Id, "Hijacked");

        Assert.That(result, Is.Null);
        Assert.That((await _db.Reviews.FindAsync(review.Id))!.Content, Is.EqualTo("Original"));
    }

    [Test]
    public async Task EditAsync_NonExistentReview_ReturnsNull()
    {
        var user = await SeedUserAsync();

        var result = await _sut.EditAsync(Guid.NewGuid(), user.Id, "Content");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task EditAsync_WithContentExceedingMaxLength_ThrowsArgumentException()
    {
        var user = await SeedUserAsync();
        var review = await _sut.AddAsync(user.Id, Guid.NewGuid(), "Original");

        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.EditAsync(review.Id, user.Id, new string('a', 2001)));
        Assert.That((await _db.Reviews.FindAsync(review.Id))!.Content, Is.EqualTo("Original"));
    }

    [Test]
    public async Task EditAsync_WithWhitespaceContent_ThrowsArgumentException()
    {
        var user = await SeedUserAsync();
        var review = await _sut.AddAsync(user.Id, Guid.NewGuid(), "Original");

        Assert.ThrowsAsync<ArgumentException>(() => _sut.EditAsync(review.Id, user.Id, "   "));
        Assert.That((await _db.Reviews.FindAsync(review.Id))!.Content, Is.EqualTo("Original"));
    }

    [Test]
    public async Task DeleteAsync_OwnReview_DeletesAndReturnsTrue()
    {
        var user = await SeedUserAsync();
        var review = await _sut.AddAsync(user.Id, Guid.NewGuid(), "To delete");

        var result = await _sut.DeleteAsync(review.Id, user.Id);

        Assert.That(result, Is.True);
        Assert.That(await _db.Reviews.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteAsync_AnotherUsersReview_ReturnsFalseAndDoesNotDelete()
    {
        var owner = await SeedUserAsync("owner");
        var other = await SeedUserAsync("other");
        var review = await _sut.AddAsync(owner.Id, Guid.NewGuid(), "Mine");

        var result = await _sut.DeleteAsync(review.Id, other.Id);

        Assert.That(result, Is.False);
        Assert.That(await _db.Reviews.FindAsync(review.Id), Is.Not.Null);
    }

    [Test]
    public async Task DeleteAsync_NonExistentReview_ReturnsFalse()
    {
        var user = await SeedUserAsync();

        var result = await _sut.DeleteAsync(Guid.NewGuid(), user.Id);

        Assert.That(result, Is.False);
    }
}
