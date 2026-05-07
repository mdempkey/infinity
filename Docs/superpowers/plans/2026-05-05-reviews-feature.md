# Reviews Feature Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add fully functional reviews to the attraction widget — users can submit, edit their own, and delete their own reviews, with username, last-modified date, and content displayed.

**Architecture:** A new `ReviewController` handles four REST endpoints (GET, POST, PUT, DELETE) in `Infinity.WebApplication`. The widget fetches reviews async on open (mirroring the rating pattern), renders them with inline edit/delete for owned reviews, and re-fetches after every mutation. The existing `ReviewService` is updated to set `ModifiedAt` on create and edit, and to eager-load `User` for author names.

**Tech Stack:** ASP.NET Core MVC (.NET 10), Entity Framework Core + PostgreSQL, NUnit (service and controller tests), vanilla JS (no frameworks in views)

---

## Files Changed

| File | Change |
|------|--------|
| `src/Infinity.WebApplication/Models/Review.cs` | Add `ModifiedAt` property |
| `src/Infinity.WebApplication/Data/UserDbContext.cs` | Map `modified_at` column |
| `src/Infinity.WebApplication/Services/ReviewService/ReviewService.cs` | Set `ModifiedAt`; eager-load `User` |
| `src/Infinity.WebApplication/Models/ReviewDtos.cs` | New — request/response records |
| `src/Infinity.WebApplication/Controllers/ReviewController.cs` | New — GET, POST, PUT, DELETE |
| `src/Infinity.WebApplication/Migrations/` | New EF migration for `modified_at` |
| `src/Infinity.WebApplication.Tests/Services/ReviewServiceTests.cs` | Add `ModifiedAt` and `User` assertions |
| `src/Infinity.WebApplication.Tests/Controllers/ReviewControllerTests.cs` | New — controller tests |
| `src/Infinity.WebApplication/Views/Shared/_Reviews.cshtml` | maxlength 2000; ids; char counter |
| `src/Infinity.WebApplication/Views/Shared/_ReviewsScript.cshtml` | isOwner, edit/delete buttons |
| `src/Infinity.WebApplication/Views/Shared/_AttractionWidgetScript.cshtml` | `fetchAndRenderReviews`; action binding |
| `src/Infinity.WebApplication/Views/Shared/_GlobeScript.cshtml` | Remove unused `attractionReviews` param |

---

### Task 1: Add `ModifiedAt` to Review model and set it on creation

**Files:**
- Modify: `src/Infinity.WebApplication/Models/Review.cs`
- Modify: `src/Infinity.WebApplication/Data/UserDbContext.cs`
- Modify: `src/Infinity.WebApplication/Services/ReviewService/ReviewService.cs`
- Modify: `src/Infinity.WebApplication.Tests/Services/ReviewServiceTests.cs`

- [ ] **Step 1: Add failing assertions to `AddAsync_InsertsAndReturnsReview`**

In `src/Infinity.WebApplication.Tests/Services/ReviewServiceTests.cs`, add these two lines at the end of `AddAsync_InsertsAndReturnsReview`:

```csharp
Assert.That(result.ModifiedAt, Is.Not.EqualTo(default(DateTime)));
Assert.That(result.ModifiedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
```

- [ ] **Step 2: Run test to verify it fails**

```bash
cd /Users/bccass/RiderProjects/infinity/src
dotnet test Infinity.WebApplication.Tests --filter "FullyQualifiedName~ReviewServiceTests.AddAsync_InsertsAndReturnsReview" -v minimal
```

Expected: Compile error — `'Review' does not contain a definition for 'ModifiedAt'`

- [ ] **Step 3: Add `ModifiedAt` to `Review` model**

Replace entire `src/Infinity.WebApplication/Models/Review.cs`:

```csharp
namespace Infinity.WebApplication.Models;

public class Review
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AttractionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime ModifiedAt { get; set; }
    public User User { get; set; } = null!;
}
```

- [ ] **Step 4: Map `modified_at` column in `UserDbContext`**

In `src/Infinity.WebApplication/Data/UserDbContext.cs`, inside the `modelBuilder.Entity<Review>(e => { ... })` block, add this line after the `Content` property mapping:

```csharp
e.Property(r => r.ModifiedAt).HasColumnName("modified_at").IsRequired();
```

- [ ] **Step 5: Set `ModifiedAt` in `ReviewService.AddAsync`**

In `src/Infinity.WebApplication/Services/ReviewService/ReviewService.cs`, replace the `AddAsync` method:

```csharp
public async Task<Review> AddAsync(Guid userId, Guid attractionId, string content)
{
    if (string.IsNullOrWhiteSpace(content))
        throw new ArgumentException("Review content cannot be empty.", nameof(content));
    if (content.Length > 2000)
        throw new ArgumentException("Review content cannot exceed 2000 characters.", nameof(content));

    var review = new Review
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        AttractionId = attractionId,
        Content = content,
        ModifiedAt = DateTime.UtcNow
    };
    db.Reviews.Add(review);
    await db.SaveChangesAsync();
    return review;
}
```

- [ ] **Step 6: Run test to verify it passes**

```bash
cd /Users/bccass/RiderProjects/infinity/src
dotnet test Infinity.WebApplication.Tests --filter "FullyQualifiedName~ReviewServiceTests.AddAsync_InsertsAndReturnsReview" -v minimal
```

Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/Infinity.WebApplication/Models/Review.cs src/Infinity.WebApplication/Data/UserDbContext.cs src/Infinity.WebApplication/Services/ReviewService/ReviewService.cs src/Infinity.WebApplication.Tests/Services/ReviewServiceTests.cs
git commit -m "feat: add ModifiedAt to Review model and set on creation"
```

---

### Task 2: Set `ModifiedAt` on edit; eager-load `User` in `GetByAttractionAsync`

**Files:**
- Modify: `src/Infinity.WebApplication/Services/ReviewService/ReviewService.cs`
- Modify: `src/Infinity.WebApplication.Tests/Services/ReviewServiceTests.cs`

- [ ] **Step 1: Update `EditAsync_OwnReview_UpdatesContentAndReturnsReview` and add `User` test**

In `src/Infinity.WebApplication.Tests/Services/ReviewServiceTests.cs`, replace the `EditAsync_OwnReview_UpdatesContentAndReturnsReview` test:

```csharp
[Test]
public async Task EditAsync_OwnReview_UpdatesContentAndReturnsReview()
{
    var user = await SeedUserAsync();
    var review = await _sut.AddAsync(user.Id, Guid.NewGuid(), "Original");
    var originalModifiedAt = review.ModifiedAt;

    await Task.Delay(1);
    var result = await _sut.EditAsync(review.Id, user.Id, "Updated");

    Assert.That(result, Is.Not.Null);
    Assert.That(result!.Content, Is.EqualTo("Updated"));
    Assert.That((await _db.Reviews.FindAsync(review.Id))!.Content, Is.EqualTo("Updated"));
    Assert.That(result.ModifiedAt, Is.GreaterThanOrEqualTo(originalModifiedAt));
}
```

Add this new test after `GetByAttractionAsync_WithNoReviews_ReturnsEmptyCollection`:

```csharp
[Test]
public async Task GetByAttractionAsync_PopulatesUserNavigationProperty()
{
    var user = await SeedUserAsync();
    var attractionId = Guid.NewGuid();
    await _sut.AddAsync(user.Id, attractionId, "Hello");

    var result = (await _sut.GetByAttractionAsync(attractionId)).ToList();

    Assert.That(result, Has.Count.EqualTo(1));
    Assert.That(result[0].User, Is.Not.Null);
    Assert.That(result[0].User.Username, Is.EqualTo("testuser"));
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd /Users/bccass/RiderProjects/infinity/src
dotnet test Infinity.WebApplication.Tests --filter "FullyQualifiedName~ReviewServiceTests.EditAsync_OwnReview|FullyQualifiedName~ReviewServiceTests.GetByAttractionAsync_Populates" -v minimal
```

Expected: `EditAsync` fails (ModifiedAt not updated); `GetByAttractionAsync_Populates` fails (User is null)

- [ ] **Step 3: Update `ReviewService.EditAsync` to set `ModifiedAt`**

In `src/Infinity.WebApplication/Services/ReviewService/ReviewService.cs`, replace the `EditAsync` method:

```csharp
public async Task<Review?> EditAsync(Guid reviewId, Guid userId, string content)
{
    if (string.IsNullOrWhiteSpace(content))
        throw new ArgumentException("Review content cannot be empty.", nameof(content));
    if (content.Length > 2000)
        throw new ArgumentException("Review content cannot exceed 2000 characters.", nameof(content));

    var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);
    if (review is null) return null;

    review.Content = content;
    review.ModifiedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return review;
}
```

- [ ] **Step 4: Update `ReviewService.GetByAttractionAsync` to include `User`**

In `src/Infinity.WebApplication/Services/ReviewService/ReviewService.cs`, replace the `GetByAttractionAsync` method:

```csharp
public async Task<IEnumerable<Review>> GetByAttractionAsync(Guid attractionId) =>
    await db.Reviews
        .Include(r => r.User)
        .Where(r => r.AttractionId == attractionId)
        .ToListAsync();
```

- [ ] **Step 5: Run all ReviewService tests to verify they pass**

```bash
cd /Users/bccass/RiderProjects/infinity/src
dotnet test Infinity.WebApplication.Tests --filter "FullyQualifiedName~ReviewServiceTests" -v minimal
```

Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add src/Infinity.WebApplication/Services/ReviewService/ReviewService.cs src/Infinity.WebApplication.Tests/Services/ReviewServiceTests.cs
git commit -m "feat: set ModifiedAt on edit and eager-load User in GetByAttractionAsync"
```

---

### Task 3: Generate EF Core migration for `ModifiedAt`

**Files:**
- Create: `src/Infinity.WebApplication/Migrations/` (generated)

- [ ] **Step 1: Generate migration**

```bash
cd /Users/bccass/RiderProjects/infinity/src
dotnet ef migrations add AddReviewModifiedAt --project Infinity.WebApplication --startup-project Infinity.WebApplication --context UserDbContext
```

Expected: Output ending with `Done.` and two new files created in `Migrations/`.

- [ ] **Step 2: Verify migration content**

Open the generated migration file. The `Up` method should contain:

```csharp
migrationBuilder.AddColumn<DateTime>(
    name: "modified_at",
    table: "reviews",
    type: "timestamp with time zone",
    nullable: false,
    defaultValue: new DateTime(...)
);
```

The `Down` method should contain:

```csharp
migrationBuilder.DropColumn(
    name: "modified_at",
    table: "reviews");
```

If either is missing, add it manually before committing.

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Migrations/
git commit -m "feat: add EF migration for Review ModifiedAt column"
```

---

### Task 4: Create Review DTOs and `ReviewController`

**Files:**
- Create: `src/Infinity.WebApplication/Models/ReviewDtos.cs`
- Create: `src/Infinity.WebApplication/Controllers/ReviewController.cs`
- Create: `src/Infinity.WebApplication.Tests/Controllers/ReviewControllerTests.cs`

- [ ] **Step 1: Write failing controller tests**

Create `src/Infinity.WebApplication.Tests/Controllers/ReviewControllerTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run tests to verify they fail (compilation)**

```bash
cd /Users/bccass/RiderProjects/infinity/src
dotnet test Infinity.WebApplication.Tests --filter "FullyQualifiedName~ReviewControllerTests" -v minimal
```

Expected: Compile errors — `ReviewController`, `ReviewResponse`, `SubmitReviewRequest`, `EditReviewRequest` not defined.

- [ ] **Step 3: Create `ReviewDtos.cs`**

Create `src/Infinity.WebApplication/Models/ReviewDtos.cs`:

```csharp
namespace Infinity.WebApplication.Models;

public record ReviewResponse(Guid Id, string Author, string Date, string Comment, bool IsOwner);
public record SubmitReviewRequest(Guid AttractionId, string Content);
public record EditReviewRequest(string Content);
```

- [ ] **Step 4: Create `ReviewController.cs`**

Create `src/Infinity.WebApplication/Controllers/ReviewController.cs`:

```csharp
using System.Security.Claims;
using Infinity.WebApplication.Models;
using Infinity.WebApplication.Services.ReviewService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Infinity.WebApplication.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet("{attractionId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ReviewResponse>>> GetByAttraction(Guid attractionId)
    {
        Guid? userId = null;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(claim, out var parsedId))
            userId = parsedId;

        var reviews = await _reviewService.GetByAttractionAsync(attractionId);

        var response = reviews.Select(r => new ReviewResponse(
            Id: r.Id,
            Author: r.User?.Username ?? "Unknown",
            Date: r.ModifiedAt.ToString("MMMM d, yyyy"),
            Comment: r.Content,
            IsOwner: userId.HasValue && r.UserId == userId.Value
        ));

        return Ok(response);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReviewResponse>> Submit(SubmitReviewRequest request)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claim, out var userId))
            return Unauthorized();

        try
        {
            var review = await _reviewService.AddAsync(userId, request.AttractionId, request.Content);
            return Ok(new ReviewResponse(
                Id: review.Id,
                Author: User.Identity?.Name ?? "Unknown",
                Date: review.ModifiedAt.ToString("MMMM d, yyyy"),
                Comment: review.Content,
                IsOwner: true
            ));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{reviewId:guid}")]
    [Authorize]
    public async Task<ActionResult<ReviewResponse>> Edit(Guid reviewId, EditReviewRequest request)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claim, out var userId))
            return Unauthorized();

        try
        {
            var review = await _reviewService.EditAsync(reviewId, userId, request.Content);
            if (review is null)
                return NotFound();

            return Ok(new ReviewResponse(
                Id: review.Id,
                Author: User.Identity?.Name ?? "Unknown",
                Date: review.ModifiedAt.ToString("MMMM d, yyyy"),
                Comment: review.Content,
                IsOwner: true
            ));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{reviewId:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid reviewId)
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claim, out var userId))
            return Unauthorized();

        var deleted = await _reviewService.DeleteAsync(reviewId, userId);
        return deleted ? NoContent() : NotFound();
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
cd /Users/bccass/RiderProjects/infinity/src
dotnet test Infinity.WebApplication.Tests --filter "FullyQualifiedName~ReviewControllerTests" -v minimal
```

Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add src/Infinity.WebApplication/Models/ReviewDtos.cs src/Infinity.WebApplication/Controllers/ReviewController.cs src/Infinity.WebApplication.Tests/Controllers/ReviewControllerTests.cs
git commit -m "feat: add ReviewController with GET, POST, PUT, DELETE endpoints"
```

---

### Task 5: Update `_Reviews.cshtml` (draft form)

**Files:**
- Modify: `src/Infinity.WebApplication/Views/Shared/_Reviews.cshtml`

- [ ] **Step 1: Replace `_Reviews.cshtml`**

Replace the entire file content:

```cshtml
<section class="attraction-widget__reviews mt-3 pt-3 border-top border-secondary-subtle" aria-label="Reviews">
    <h3 class="h6 text-uppercase text-primary mb-2">Reviews</h3>
    <div id="attraction-widget-reviews-list"></div>

    @if (User?.Identity?.IsAuthenticated ?? false)
    {
        <form class="mt-3" novalidate>
            <label class="form-label small text-secondary mb-2" for="attraction-widget-review-input">Add your review</label>
            <textarea id="attraction-widget-review-input"
                      class="form-control form-control-sm bg-dark-subtle text-light border-secondary"
                      rows="4"
                      maxlength="2000"
                      placeholder="Share your experience..."></textarea>
            <div class="d-flex justify-content-between align-items-center mt-1">
                <button type="button" id="attraction-widget-review-submit" class="btn btn-sm fw-semibold">Submit Review</button>
                <span id="attraction-widget-review-counter" class="small text-secondary">0/2000</span>
            </div>
        </form>
    }
    else
    {
        <div class="alert alert-secondary py-2 px-3 mt-3 mb-0" role="note">You must be signed in to leave a review.</div>
    }
</section>
```

- [ ] **Step 2: Commit**

```bash
git add src/Infinity.WebApplication/Views/Shared/_Reviews.cshtml
git commit -m "feat: update Reviews partial with maxlength 2000 and character counter"
```

---

### Task 6: Update `_ReviewsScript.cshtml` (renderer)

**Files:**
- Modify: `src/Infinity.WebApplication/Views/Shared/_ReviewsScript.cshtml`

- [ ] **Step 1: Replace `_ReviewsScript.cshtml`**

Replace the entire file content:

```html
<script>
    (() => {
        const escapeHtml = (value) => String(value ?? "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\"", "&quot;")
            .replaceAll("'", "&#39;");

        const renderReviews = (reviews) => {
            if (!Array.isArray(reviews) || reviews.length === 0) {
                return `<p class="small text-secondary mb-0">No reviews yet. Be the first to leave a review.</p>`;
            }

            const rows = reviews
                .map((review) => `
                    <article class="list-group-item bg-transparent text-light border-secondary-subtle px-0"
                             data-review-id="${escapeHtml(String(review.id))}">
                        <div class="d-flex justify-content-between gap-2 small text-secondary mb-1">
                            <strong class="text-light">${escapeHtml(review.author)}</strong>
                            <span>${escapeHtml(review.date)}</span>
                        </div>
                        <p class="mb-1 small text-light-emphasis" data-review-content>${escapeHtml(review.comment)}</p>
                        ${review.isOwner ? `
                            <div class="d-flex gap-2 mt-1" data-review-actions>
                                <button type="button"
                                        class="btn btn-link btn-sm p-0 text-secondary text-decoration-none"
                                        data-review-action="edit">Edit</button>
                                <button type="button"
                                        class="btn btn-link btn-sm p-0 text-secondary text-decoration-none"
                                        data-review-action="delete">Delete</button>
                            </div>
                        ` : ""}
                    </article>
                `)
                .join("");

            return `<div class="list-group list-group-flush mt-2">${rows}</div>`;
        };

        window.renderHomepageAttractionReviews = renderReviews;
    })();
</script>
```

- [ ] **Step 2: Commit**

```bash
git add src/Infinity.WebApplication/Views/Shared/_ReviewsScript.cshtml
git commit -m "feat: update Reviews renderer with isOwner edit/delete buttons"
```

---

### Task 7: Update `_AttractionWidgetScript.cshtml` (fetch, submit, edit, delete)

**Files:**
- Modify: `src/Infinity.WebApplication/Views/Shared/_AttractionWidgetScript.cshtml`

- [ ] **Step 1: Replace `_AttractionWidgetScript.cshtml`**

Replace the entire file content:

```html
<script>
    (() => {
        const getOverlay = () => document.getElementById("attraction-widget-overlay");
        const getParkNode = () => document.getElementById("attraction-widget-park");
        const getTitleNode = () => document.getElementById("attraction-widget-title");
        const getRatingNode = () => document.getElementById("attraction-widget-rating");
        const getSubtitleNode = () => document.getElementById("attraction-widget-subtitle");
        const getReviewsListNode = () => document.getElementById("attraction-widget-reviews-list");
        const getCloseButton = () => document.getElementById("attraction-widget-close");
        const getHomePage = () => document.querySelector(".home-page");

        let currentAttractionId = null;

        const closeWidget = () => {
            const overlay = getOverlay();
            if (!overlay) return;
            overlay.hidden = true;
            getHomePage()?.classList.remove("is-attraction-widget-open");
            currentAttractionId = null;
        };

        const fetchAndRenderReviews = async (attractionId) => {
            const reviewsListNode = getReviewsListNode();
            if (!reviewsListNode) return;

            const token = window.getToken?.();
            const headers = token ? { "Authorization": `Bearer ${token}` } : {};

            try {
                const res = await fetch(`/api/reviews/${attractionId}`, { headers });
                if (!res.ok) return;
                const reviews = await res.json();
                if (currentAttractionId !== attractionId) return;
                reviewsListNode.innerHTML = window.renderHomepageAttractionReviews?.(reviews) ?? "";
            } catch {
                // leave current render in place silently
            }
        };

        const handleSubmitReview = async () => {
            const textarea = document.getElementById("attraction-widget-review-input");
            const counter = document.getElementById("attraction-widget-review-counter");
            if (!textarea || !currentAttractionId) return;
            const content = textarea.value.trim();
            if (!content) return;

            const token = window.getToken?.();
            if (!token) return;

            try {
                const res = await fetch("/api/reviews", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        "Authorization": `Bearer ${token}`
                    },
                    body: JSON.stringify({ attractionId: currentAttractionId, content })
                });
                if (!res.ok) return;
                textarea.value = "";
                if (counter) counter.textContent = "0/2000";
                await fetchAndRenderReviews(currentAttractionId);
            } catch {
                // silently fail
            }
        };

        const handleEditReview = (article, reviewId) => {
            const contentPara = article.querySelector("[data-review-content]");
            const actionsDiv = article.querySelector("[data-review-actions]");
            if (!contentPara) return;
            const originalContent = contentPara.textContent ?? "";

            contentPara.hidden = true;
            if (actionsDiv) actionsDiv.hidden = true;

            const editContainer = document.createElement("div");
            editContainer.setAttribute("data-edit-container", "");
            editContainer.innerHTML = `
                <textarea class="form-control form-control-sm bg-dark-subtle text-light border-secondary mt-1"
                          rows="3" maxlength="2000" data-edit-textarea></textarea>
                <div class="d-flex justify-content-between align-items-center mt-1">
                    <div class="d-flex gap-2">
                        <button type="button"
                                class="btn btn-link btn-sm p-0 text-secondary text-decoration-none"
                                data-review-action="save">Save</button>
                        <button type="button"
                                class="btn btn-link btn-sm p-0 text-secondary text-decoration-none"
                                data-review-action="cancel">Cancel</button>
                    </div>
                    <span class="small text-secondary" data-edit-counter>${originalContent.length}/2000</span>
                </div>
            `;
            article.appendChild(editContainer);

            const editTextarea = editContainer.querySelector("[data-edit-textarea]");
            const editCounter = editContainer.querySelector("[data-edit-counter]");
            if (editTextarea) {
                editTextarea.value = originalContent;
                editTextarea.addEventListener("input", () => {
                    if (editCounter) editCounter.textContent = `${editTextarea.value.length}/2000`;
                });
                editTextarea.focus();
                editTextarea.setSelectionRange(editTextarea.value.length, editTextarea.value.length);
            }
        };

        const handleSaveEdit = async (article, reviewId) => {
            const editTextarea = article.querySelector("[data-edit-textarea]");
            if (!editTextarea) return;
            const content = editTextarea.value.trim();
            if (!content) return;

            const token = window.getToken?.();
            if (!token) return;

            try {
                const res = await fetch(`/api/reviews/${reviewId}`, {
                    method: "PUT",
                    headers: {
                        "Content-Type": "application/json",
                        "Authorization": `Bearer ${token}`
                    },
                    body: JSON.stringify({ content })
                });
                if (!res.ok) return;
                await fetchAndRenderReviews(currentAttractionId);
            } catch {
                // silently fail
            }
        };

        const handleCancelEdit = (article) => {
            const contentPara = article.querySelector("[data-review-content]");
            const actionsDiv = article.querySelector("[data-review-actions]");
            const editContainer = article.querySelector("[data-edit-container]");
            if (contentPara) contentPara.hidden = false;
            if (actionsDiv) actionsDiv.hidden = false;
            if (editContainer) editContainer.remove();
        };

        const handleDeleteReview = async (reviewId) => {
            const token = window.getToken?.();
            if (!token) return;

            try {
                const res = await fetch(`/api/reviews/${reviewId}`, {
                    method: "DELETE",
                    headers: { "Authorization": `Bearer ${token}` }
                });
                if (!res.ok) return;
                await fetchAndRenderReviews(currentAttractionId);
            } catch {
                // silently fail
            }
        };

        const handleRate = async (attractionId, value) => {
            const token = window.getToken?.();
            if (!token) throw new Error("not authenticated");

            const res = await fetch("/api/ratings", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${token}`
                },
                body: JSON.stringify({ attractionId, value })
            });
            if (!res.ok) throw new Error(`POST failed: ${res.status}`);

            const data = await res.json();
            const ratingNode = getRatingNode();
            if (!ratingNode || currentAttractionId !== attractionId) return;

            ratingNode.innerHTML = window.renderInteractiveStarRating?.({
                attractionId,
                average: data.newAverage,
                count: data.newCount,
                userRating: data.value,
                interactive: true
            }) ?? "";

            const container = ratingNode.firstElementChild;
            if (container) {
                window.bindInteractiveStarRating?.(container, (v) => handleRate(attractionId, v));
            }

            const panelRating = document.querySelector(`[data-rating-container="${attractionId}"]`);
            if (panelRating) {
                panelRating.innerHTML = window.renderHomepageStarRating?.(data.newAverage) ?? "";
            }
        };

        const fetchAndRenderRating = async (attractionId, initialAverage) => {
            const token = window.getToken?.();
            const headers = token ? { "Authorization": `Bearer ${token}` } : {};

            try {
                const res = await fetch(`/api/ratings/${attractionId}/mine`, { headers });
                if (!res.ok) return;

                const data = await res.json();
                const ratingNode = getRatingNode();
                if (!ratingNode || currentAttractionId !== attractionId) return;

                const isLoggedIn = !!token;
                ratingNode.innerHTML = window.renderInteractiveStarRating?.({
                    attractionId,
                    average: data.average ?? initialAverage,
                    count: data.count,
                    userRating: data.value,
                    interactive: isLoggedIn
                }) ?? "";

                if (isLoggedIn) {
                    const container = ratingNode.firstElementChild;
                    if (container) {
                        window.bindInteractiveStarRating?.(container, (v) => handleRate(attractionId, v));
                    }
                }
            } catch {
                // leave initial render in place silently
            }
        };

        const openWidget = ({ parkTitle, parkLocation, attractionTitle, attractionSubtitle, attractionRating, attractionId }) => {
            const overlay = getOverlay();
            const parkNode = getParkNode();
            const titleNode = getTitleNode();
            const ratingNode = getRatingNode();
            const subtitleNode = getSubtitleNode();
            const reviewsListNode = getReviewsListNode();
            if (!overlay || !parkNode || !titleNode || !ratingNode || !subtitleNode || !reviewsListNode) return;

            currentAttractionId = attractionId;

            parkNode.textContent = `${parkTitle} - ${parkLocation}`;
            titleNode.textContent = attractionTitle;
            subtitleNode.textContent = attractionSubtitle;
            reviewsListNode.innerHTML = `<p class="small text-secondary mb-0">Loading reviews…</p>`;

            ratingNode.innerHTML = window.renderInteractiveStarRating?.({
                attractionId,
                average: attractionRating,
                count: null,
                userRating: null,
                interactive: false
            }) ?? "";

            overlay.hidden = false;
            getHomePage()?.classList.add("is-attraction-widget-open");

            fetchAndRenderRating(attractionId, attractionRating);
            fetchAndRenderReviews(attractionId);
        };

        const bindWidgetInteractions = () => {
            const overlay = getOverlay();
            const closeButton = getCloseButton();
            if (!overlay || !closeButton) return;

            closeButton.addEventListener("click", closeWidget);
            overlay.addEventListener("click", (event) => {
                if (event.target === overlay) closeWidget();
            });
            document.addEventListener("keydown", (event) => {
                if (event.key === "Escape") closeWidget();
            });

            const draftTextarea = document.getElementById("attraction-widget-review-input");
            const draftCounter = document.getElementById("attraction-widget-review-counter");
            if (draftTextarea && draftCounter) {
                draftTextarea.addEventListener("input", () => {
                    draftCounter.textContent = `${draftTextarea.value.length}/2000`;
                });
            }

            const submitBtn = document.getElementById("attraction-widget-review-submit");
            if (submitBtn) {
                submitBtn.addEventListener("click", handleSubmitReview);
            }

            const reviewsListNode = getReviewsListNode();
            if (reviewsListNode) {
                reviewsListNode.addEventListener("click", (event) => {
                    const btn = event.target.closest("[data-review-action]");
                    if (!btn) return;
                    const action = btn.getAttribute("data-review-action");
                    const article = btn.closest("article[data-review-id]");
                    if (!article) return;
                    const reviewId = article.getAttribute("data-review-id");
                    if (!reviewId) return;

                    if (action === "edit") handleEditReview(article, reviewId);
                    else if (action === "delete") handleDeleteReview(reviewId);
                    else if (action === "save") handleSaveEdit(article, reviewId);
                    else if (action === "cancel") handleCancelEdit(article);
                });
            }
        };

        bindWidgetInteractions();
        closeWidget();
        window.openHomepageAttractionWidget = openWidget;
        window.closeHomepageAttractionWidget = closeWidget;
    })();
</script>
```

- [ ] **Step 2: Commit**

```bash
git add src/Infinity.WebApplication/Views/Shared/_AttractionWidgetScript.cshtml
git commit -m "feat: add fetchAndRenderReviews and inline edit/delete to attraction widget"
```

---

### Task 8: Remove unused `attractionReviews` from `_GlobeScript.cshtml`

**Files:**
- Modify: `src/Infinity.WebApplication/Views/Shared/_GlobeScript.cshtml`

- [ ] **Step 1: Remove `attractionReviews` from the globe marker click handler**

In `_GlobeScript.cshtml`, find the first `openHomepageAttractionWidget` call (inside `addAttractionMarkers`, around line 138):

```javascript
                        window.openHomepageAttractionWidget?.({
                            parkTitle: park.title,
                            parkLocation: `${park.location}, ${park.country}`,
                            attractionTitle: attraction.title,
                            attractionSubtitle: attraction.subtitle,
                            attractionRating: attraction.rating,
                            attractionId: attraction.id,
                            attractionReviews: attraction.reviews
                        });
```

Replace with:

```javascript
                        window.openHomepageAttractionWidget?.({
                            parkTitle: park.title,
                            parkLocation: `${park.location}, ${park.country}`,
                            attractionTitle: attraction.title,
                            attractionSubtitle: attraction.subtitle,
                            attractionRating: attraction.rating,
                            attractionId: attraction.id
                        });
```

- [ ] **Step 2: Remove `attractionReviews` from the panel card click handler**

Find the second `openHomepageAttractionWidget` call (inside `activateAttractionFromEvent`, around line 229):

```javascript
                    window.openHomepageAttractionWidget?.({
                        parkTitle: focusedPark.title,
                        parkLocation: `${focusedPark.location}, ${focusedPark.country}`,
                        attractionTitle: attraction.title,
                        attractionSubtitle: attraction.subtitle,
                        attractionRating: attraction.rating,
                        attractionId: attraction.id,
                        attractionReviews: attraction.reviews
                    });
```

Replace with:

```javascript
                    window.openHomepageAttractionWidget?.({
                        parkTitle: focusedPark.title,
                        parkLocation: `${focusedPark.location}, ${focusedPark.country}`,
                        attractionTitle: attraction.title,
                        attractionSubtitle: attraction.subtitle,
                        attractionRating: attraction.rating,
                        attractionId: attraction.id
                    });
```

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Views/Shared/_GlobeScript.cshtml
git commit -m "chore: remove unused attractionReviews param from openWidget calls"
```

---

### Task 9: Final verification

**Files:** (none)

- [ ] **Step 1: Run full test suite**

```bash
cd /Users/bccass/RiderProjects/infinity/src
dotnet test --verbosity minimal
```

Expected: All tests pass, 0 failures.

- [ ] **Step 2: Verify the build is clean**

```bash
cd /Users/bccass/RiderProjects/infinity/src
dotnet build Infinity.WebApplication --no-incremental
```

Expected: `Build succeeded. 0 Error(s).`
