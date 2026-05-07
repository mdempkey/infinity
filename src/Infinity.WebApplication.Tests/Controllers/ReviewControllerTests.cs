using System.Security.Claims;
using Infinity.WebApplication.Controllers;
using Infinity.WebApplication.Data;
using Infinity.WebApplication.Models;
using Infinity.WebApplication.Services.ReviewService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Tests.Controllers;

public class ReviewControllerTests
{
    private UserDbContext _db = null!;
    private IReviewService _reviewService = null!;
    private ReviewController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new UserDbContext(
            new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        _reviewService = new ReviewService(_db);
        _sut = new ReviewController(_reviewService);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<User> SeedUserAsync(string suffix = "")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = $"user{suffix}",
            Email = $"user{suffix}@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private void SetUser(Guid userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
    }

    private void SetAnonymousUser()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };
    }

    [Test]
    public async Task GetByAttraction_ReturnsEmptyList_WhenNoReviews()
    {
        SetAnonymousUser();

        var result = await _sut.GetByAttraction(Guid.NewGuid());

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var reviews = ok!.Value as IEnumerable<ReviewResponse>;
        Assert.That(reviews, Is.Empty);
    }

    [Test]
    public async Task GetByAttraction_ReturnsReview_WithIsOwnerTrue_ForAuthenticatedOwner()
    {
        var user = await SeedUserAsync();
        SetUser(user.Id);
        var attractionId = Guid.NewGuid();
        await _reviewService.AddAsync(user.Id, attractionId, "Great!");

        var result = await _sut.GetByAttraction(attractionId);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var reviews = (ok!.Value as IEnumerable<ReviewResponse>)!.ToList();
        Assert.That(reviews, Has.Count.EqualTo(1));
        Assert.That(reviews[0].IsOwner, Is.True);
        Assert.That(reviews[0].Author, Is.EqualTo("user"));
        Assert.That(reviews[0].Comment, Is.EqualTo("Great!"));
    }

    [Test]
    public async Task GetByAttraction_SetsIsOwnerFalse_ForOtherUsersReview()
    {
        var owner = await SeedUserAsync("owner");
        var viewer = await SeedUserAsync("viewer");
        SetUser(viewer.Id);
        var attractionId = Guid.NewGuid();
        await _reviewService.AddAsync(owner.Id, attractionId, "Mine");

        var result = await _sut.GetByAttraction(attractionId);

        var ok = result.Result as OkObjectResult;
        var reviews = (ok!.Value as IEnumerable<ReviewResponse>)!.ToList();
        Assert.That(reviews[0].IsOwner, Is.False);
    }

    [Test]
    public async Task GetByAttraction_SetsIsOwnerFalse_ForAnonymousUser()
    {
        var user = await SeedUserAsync();
        SetAnonymousUser();
        var attractionId = Guid.NewGuid();
        await _reviewService.AddAsync(user.Id, attractionId, "Hello");

        var result = await _sut.GetByAttraction(attractionId);

        var ok = result.Result as OkObjectResult;
        var reviews = (ok!.Value as IEnumerable<ReviewResponse>)!.ToList();
        Assert.That(reviews[0].IsOwner, Is.False);
    }

    [Test]
    public async Task Submit_CreatesReview_AndReturnsOk()
    {
        var user = await SeedUserAsync();
        SetUser(user.Id);
        var attractionId = Guid.NewGuid();

        var result = await _sut.Submit(new SubmitReviewRequest(attractionId, "Awesome!"));

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var review = ok!.Value as ReviewResponse;
        Assert.That(review, Is.Not.Null);
        Assert.That(review!.Comment, Is.EqualTo("Awesome!"));
        Assert.That(review.IsOwner, Is.True);
        Assert.That(review!.Author, Is.Not.Null);
        Assert.That(await _db.Reviews.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task Submit_WithEmptyContent_ReturnsBadRequest()
    {
        var user = await SeedUserAsync();
        SetUser(user.Id);

        var result = await _sut.Submit(new SubmitReviewRequest(Guid.NewGuid(), "   "));

        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Edit_OwnReview_ReturnsUpdatedReview()
    {
        var user = await SeedUserAsync();
        SetUser(user.Id);
        var review = await _reviewService.AddAsync(user.Id, Guid.NewGuid(), "Original");

        var result = await _sut.Edit(review.Id, new EditReviewRequest("Updated"));

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var updated = ok!.Value as ReviewResponse;
        Assert.That(updated!.Comment, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task Edit_AnotherUsersReview_ReturnsNotFound()
    {
        var owner = await SeedUserAsync("owner");
        var other = await SeedUserAsync("other");
        var review = await _reviewService.AddAsync(owner.Id, Guid.NewGuid(), "Original");
        SetUser(other.Id);

        var result = await _sut.Edit(review.Id, new EditReviewRequest("Hijacked"));

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Delete_OwnReview_ReturnsNoContent()
    {
        var user = await SeedUserAsync();
        SetUser(user.Id);
        var review = await _reviewService.AddAsync(user.Id, Guid.NewGuid(), "To delete");

        var result = await _sut.Delete(review.Id);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        Assert.That(await _db.Reviews.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task Delete_AnotherUsersReview_ReturnsNotFound()
    {
        var owner = await SeedUserAsync("owner");
        var other = await SeedUserAsync("other");
        var review = await _reviewService.AddAsync(owner.Id, Guid.NewGuid(), "Mine");
        SetUser(other.Id);

        var result = await _sut.Delete(review.Id);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
        Assert.That(await _db.Reviews.CountAsync(), Is.EqualTo(1));
    }
}
