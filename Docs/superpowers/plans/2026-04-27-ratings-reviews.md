# Ratings & Reviews Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add attraction ratings (one per user, upsertable) and reviews (multiple per user, editable and deletable) to the WebApp user database, with aggregated average queries for attractions and parks.

**Architecture:** `Rating` and `Review` models are added to `UserDbContext` with FK relationships to `User`. Two new service pairs (`IRatingService`/`RatingService` and `IReviewService`/`ReviewService`) own all DB access. Neither controller nor any other layer touches `UserDbContext` directly. `AttractionId` is stored as a plain `Guid` with no FK constraint — attractions live in the WebAPI's separate locations database.

**Tech Stack:** ASP.NET Core 10 MVC, Entity Framework Core 10 (Npgsql), EF in-memory provider (tests only), NUnit.

---

## File Map

**Modify:**
- `src/Infinity.WebApplication/Models/Rating.cs` — replace stub with full model
- `src/Infinity.WebApplication/Models/Review.cs` — replace stub with full model
- `src/Infinity.WebApplication/Data/UserDbContext.cs` — add `Ratings`/`Reviews` DbSets and EF configuration
- `src/Infinity.WebApplication/Program.cs` — register `IRatingService` and `IReviewService`

**Create:**
- `src/Infinity.WebApplication/Services/RatingService/IRatingService.cs`
- `src/Infinity.WebApplication/Services/RatingService/RatingService.cs`
- `src/Infinity.WebApplication/Services/ReviewService/IReviewService.cs`
- `src/Infinity.WebApplication/Services/ReviewService/ReviewService.cs`
- `src/Infinity.WebApplication.Tests/Services/RatingServiceTests.cs`
- `src/Infinity.WebApplication.Tests/Services/ReviewServiceTests.cs`
- `src/Infinity.WebApplication/Migrations/<timestamp>_AddRatingsAndReviews.cs` (EF-generated)

---

## Task 1: Update Models and UserDbContext

**Files:**
- Modify: `src/Infinity.WebApplication/Models/Rating.cs`
- Modify: `src/Infinity.WebApplication/Models/Review.cs`
- Modify: `src/Infinity.WebApplication/Data/UserDbContext.cs`

- [ ] **Step 1: Replace Rating.cs**

Open `src/Infinity.WebApplication/Models/Rating.cs` and replace the entire contents with:

```csharp
namespace Infinity.WebApplication.Models;

public class Rating
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AttractionId { get; set; }
    public int Value { get; set; }
    public User User { get; set; } = null!;
}
```

- [ ] **Step 2: Replace Review.cs**

Open `src/Infinity.WebApplication/Models/Review.cs` and replace the entire contents with:

```csharp
namespace Infinity.WebApplication.Models;

public class Review
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AttractionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public User User { get; set; } = null!;
}
```

- [ ] **Step 3: Update UserDbContext**

Open `src/Infinity.WebApplication/Data/UserDbContext.cs` and replace the entire contents with:

```csharp
using Infinity.WebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id").ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");
            e.Property(u => u.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Rating>(e =>
        {
            e.ToTable("ratings");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
            e.Property(r => r.AttractionId).HasColumnName("attraction_id").IsRequired();
            e.Property(r => r.Value).HasColumnName("value").IsRequired();
            e.HasIndex(r => new { r.UserId, r.AttractionId }).IsUnique();
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Review>(e =>
        {
            e.ToTable("reviews");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
            e.Property(r => r.AttractionId).HasColumnName("attraction_id").IsRequired();
            e.Property(r => r.Content).HasColumnName("content").IsRequired();
            e.HasIndex(r => r.AttractionId);
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

- [ ] **Step 4: Verify the WebApp builds**

```bash
dotnet build src/Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 5: Commit**

```bash
git add src/Infinity.WebApplication/Models/Rating.cs \
        src/Infinity.WebApplication/Models/Review.cs \
        src/Infinity.WebApplication/Data/UserDbContext.cs
git commit -m "feat: add Rating and Review models and UserDbContext configuration"
```

---

## Task 2: RatingService — UpsertAsync (TDD)

**Files:**
- Create: `src/Infinity.WebApplication.Tests/Services/RatingServiceTests.cs`
- Create: `src/Infinity.WebApplication/Services/RatingService/IRatingService.cs`
- Create: `src/Infinity.WebApplication/Services/RatingService/RatingService.cs`

- [ ] **Step 1: Write failing tests for UpsertAsync**

Create `src/Infinity.WebApplication.Tests/Services/RatingServiceTests.cs`:

```csharp
using Infinity.WebApplication.Data;
using Infinity.WebApplication.Models;
using Infinity.WebApplication.Services.RatingService;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Tests.Services;

public class RatingServiceTests
{
    private UserDbContext _db = null!;
    private RatingService _sut = null!;

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

    private async Task<User> SeedUserAsync()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
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
        Assert.That(result!.UserId, Is.EqualTo(user.Id));
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
        Assert.That(result!.Value, Is.EqualTo(5));
        Assert.That(await _db.Ratings.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task UpsertAsync_ValueBelowZero_ReturnsNull()
    {
        var user = await SeedUserAsync();

        var result = await _sut.UpsertAsync(user.Id, Guid.NewGuid(), -1);

        Assert.That(result, Is.Null);
        Assert.That(await _db.Ratings.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task UpsertAsync_ValueAboveFive_ReturnsNull()
    {
        var user = await SeedUserAsync();

        var result = await _sut.UpsertAsync(user.Id, Guid.NewGuid(), 6);

        Assert.That(result, Is.Null);
        Assert.That(await _db.Ratings.CountAsync(), Is.EqualTo(0));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "RatingServiceTests"
```

Expected: Build error — `Infinity.WebApplication.Services.RatingService` does not exist yet.

- [ ] **Step 3: Create the interface**

Create `src/Infinity.WebApplication/Services/RatingService/IRatingService.cs`:

```csharp
using Infinity.WebApplication.Models;

namespace Infinity.WebApplication.Services.RatingService;

public interface IRatingService
{
    Task<Rating?> UpsertAsync(Guid userId, Guid attractionId, int value);
    Task<double?> GetAttractionAverageAsync(Guid attractionId);
    Task<double?> GetParkAverageAsync(IEnumerable<Guid> attractionIds);
}
```

- [ ] **Step 4: Create the implementation**

Create `src/Infinity.WebApplication/Services/RatingService/RatingService.cs`:

```csharp
using Infinity.WebApplication.Data;
using Infinity.WebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Services.RatingService;

public sealed class RatingService(UserDbContext db) : IRatingService
{
    public async Task<Rating?> UpsertAsync(Guid userId, Guid attractionId, int value)
    {
        if (value < 0 || value > 5) return null;

        var existing = await db.Ratings.FirstOrDefaultAsync(r => r.UserId == userId && r.AttractionId == attractionId);
        if (existing is not null)
        {
            existing.Value = value;
            await db.SaveChangesAsync();
            return existing;
        }

        var rating = new Rating
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AttractionId = attractionId,
            Value = value
        };
        db.Ratings.Add(rating);
        await db.SaveChangesAsync();
        return rating;
    }

    public async Task<double?> GetAttractionAverageAsync(Guid attractionId)
    {
        var averages = await db.Ratings
            .Where(r => r.AttractionId == attractionId)
            .Select(r => (double?)r.Value)
            .ToListAsync();

        return averages.Count == 0 ? null : averages.Average();
    }

    public async Task<double?> GetParkAverageAsync(IEnumerable<Guid> attractionIds)
    {
        var ids = attractionIds.ToList();
        if (ids.Count == 0) return null;

        var attractionAverages = await db.Ratings
            .Where(r => ids.Contains(r.AttractionId))
            .GroupBy(r => r.AttractionId)
            .Select(g => g.Average(r => (double)r.Value))
            .ToListAsync();

        return attractionAverages.Count == 0 ? null : attractionAverages.Average();
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "RatingServiceTests"
```

Expected: 4 tests passed.

- [ ] **Step 6: Commit**

```bash
git add src/Infinity.WebApplication/Services/RatingService/ \
        src/Infinity.WebApplication.Tests/Services/RatingServiceTests.cs
git commit -m "feat: add IRatingService and RatingService with UpsertAsync"
```

---

## Task 3: RatingService — Aggregation Tests (TDD)

**Files:**
- Modify: `src/Infinity.WebApplication.Tests/Services/RatingServiceTests.cs`

- [ ] **Step 1: Add failing tests for aggregation methods**

Open `src/Infinity.WebApplication.Tests/Services/RatingServiceTests.cs` and add these tests inside the `RatingServiceTests` class, after the existing tests:

```csharp
    [Test]
    public async Task GetAttractionAverageAsync_WithRatings_ReturnsAverage()
    {
        var user1 = new User { Id = Guid.NewGuid(), Username = "u1", Email = "u1@example.com", PasswordHash = "h", CreatedAt = DateTime.UtcNow };
        var user2 = new User { Id = Guid.NewGuid(), Username = "u2", Email = "u2@example.com", PasswordHash = "h", CreatedAt = DateTime.UtcNow };
        _db.Users.AddRange(user1, user2);
        await _db.SaveChangesAsync();

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
        var user = await SeedUserAsync();
        var attraction1 = Guid.NewGuid();
        var attraction2 = Guid.NewGuid();

        // attraction1 average = 4, attraction2 average = 2 → park average = 3
        await _sut.UpsertAsync(user.Id, attraction1, 4);
        var user2 = new User { Id = Guid.NewGuid(), Username = "u2", Email = "u2@example.com", PasswordHash = "h", CreatedAt = DateTime.UtcNow };
        _db.Users.Add(user2);
        await _db.SaveChangesAsync();
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
```

- [ ] **Step 2: Run tests to verify they pass**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "RatingServiceTests"
```

Expected: 9 tests passed.

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication.Tests/Services/RatingServiceTests.cs
git commit -m "test: add aggregation tests for RatingService"
```

---

## Task 4: ReviewService — AddAsync and GetByAttractionAsync (TDD)

**Files:**
- Create: `src/Infinity.WebApplication.Tests/Services/ReviewServiceTests.cs`
- Create: `src/Infinity.WebApplication/Services/ReviewService/IReviewService.cs`
- Create: `src/Infinity.WebApplication/Services/ReviewService/ReviewService.cs`

- [ ] **Step 1: Write failing tests**

Create `src/Infinity.WebApplication.Tests/Services/ReviewServiceTests.cs`:

```csharp
using Infinity.WebApplication.Data;
using Infinity.WebApplication.Models;
using Infinity.WebApplication.Services.ReviewService;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Tests.Services;

public class ReviewServiceTests
{
    private UserDbContext _db = null!;
    private ReviewService _sut = null!;

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
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "ReviewServiceTests"
```

Expected: Build error — `Infinity.WebApplication.Services.ReviewService` does not exist yet.

- [ ] **Step 3: Create the interface**

Create `src/Infinity.WebApplication/Services/ReviewService/IReviewService.cs`:

```csharp
using Infinity.WebApplication.Models;

namespace Infinity.WebApplication.Services.ReviewService;

public interface IReviewService
{
    Task<Review> AddAsync(Guid userId, Guid attractionId, string content);
    Task<IEnumerable<Review>> GetByAttractionAsync(Guid attractionId);
    Task<Review?> EditAsync(Guid reviewId, Guid userId, string content);
    Task<bool> DeleteAsync(Guid reviewId, Guid userId);
}
```

- [ ] **Step 4: Create the implementation**

Create `src/Infinity.WebApplication/Services/ReviewService/ReviewService.cs`:

```csharp
using Infinity.WebApplication.Data;
using Infinity.WebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Services.ReviewService;

public sealed class ReviewService(UserDbContext db) : IReviewService
{
    public async Task<Review> AddAsync(Guid userId, Guid attractionId, string content)
    {
        var review = new Review
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AttractionId = attractionId,
            Content = content
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync();
        return review;
    }

    public async Task<IEnumerable<Review>> GetByAttractionAsync(Guid attractionId) =>
        await db.Reviews.Where(r => r.AttractionId == attractionId).ToListAsync();

    public async Task<Review?> EditAsync(Guid reviewId, Guid userId, string content)
    {
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);
        if (review is null) return null;

        review.Content = content;
        await db.SaveChangesAsync();
        return review;
    }

    public async Task<bool> DeleteAsync(Guid reviewId, Guid userId)
    {
        var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);
        if (review is null) return false;

        db.Reviews.Remove(review);
        await db.SaveChangesAsync();
        return true;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "ReviewServiceTests"
```

Expected: 4 tests passed.

- [ ] **Step 6: Commit**

```bash
git add src/Infinity.WebApplication/Services/ReviewService/ \
        src/Infinity.WebApplication.Tests/Services/ReviewServiceTests.cs
git commit -m "feat: add IReviewService and ReviewService with AddAsync and GetByAttractionAsync"
```

---

## Task 5: ReviewService — EditAsync and DeleteAsync Tests (TDD)

**Files:**
- Modify: `src/Infinity.WebApplication.Tests/Services/ReviewServiceTests.cs`

- [ ] **Step 1: Add failing tests for EditAsync and DeleteAsync**

Open `src/Infinity.WebApplication.Tests/Services/ReviewServiceTests.cs` and add these tests inside the `ReviewServiceTests` class, after the existing tests:

```csharp
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
        Assert.That(await _db.Reviews.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task DeleteAsync_NonExistentReview_ReturnsFalse()
    {
        var user = await SeedUserAsync();

        var result = await _sut.DeleteAsync(Guid.NewGuid(), user.Id);

        Assert.That(result, Is.False);
    }
```

- [ ] **Step 2: Run tests to verify they pass**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "ReviewServiceTests"
```

Expected: 10 tests passed.

- [ ] **Step 3: Run full test suite to verify no regressions**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj
```

Expected: All tests passed.

- [ ] **Step 4: Commit**

```bash
git add src/Infinity.WebApplication.Tests/Services/ReviewServiceTests.cs
git commit -m "test: add edit and delete tests for ReviewService"
```

---

## Task 6: Register Services in Program.cs

**Files:**
- Modify: `src/Infinity.WebApplication/Program.cs`

- [ ] **Step 1: Register IRatingService and IReviewService**

Open `src/Infinity.WebApplication/Program.cs` and replace the entire contents with:

```csharp
using Infinity.WebApplication.Data;
using Infinity.WebApplication.Services.Home;
using Infinity.WebApplication.Services.RatingService;
using Infinity.WebApplication.Services.ReviewService;
using Infinity.WebApplication.Services.UserService;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<IIndexContentService, IndexContentService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["InfinityApi:BaseUrl"]
        ?? throw new InvalidOperationException("InfinityApi:BaseUrl is not configured."));
});

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("UserConnection")
        ?? throw new InvalidOperationException("UserConnection is not configured.")));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

var app = builder.Build();

// Apply user DB migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
```

- [ ] **Step 2: Verify the WebApp builds**

```bash
dotnet build src/Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: `Build succeeded. 0 Error(s).`

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Program.cs
git commit -m "feat: register IRatingService and IReviewService in Program.cs"
```

---

## Task 7: Generate EF Migration

**Files:**
- Create: `src/Infinity.WebApplication/Migrations/<timestamp>_AddRatingsAndReviews.cs` (EF-generated)

- [ ] **Step 1: Generate the migration**

Run from the repo root:

```bash
dotnet ef migrations add AddRatingsAndReviews \
  --project src/Infinity.WebApplication/Infinity.WebApplication.csproj \
  --context UserDbContext \
  --output-dir Migrations
```

Expected: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 2: Verify migration files were created**

```bash
ls src/Infinity.WebApplication/Migrations/
```

Expected: You see the existing `20260424061349_InitialCreate.cs` plus new files named `<timestamp>_AddRatingsAndReviews.cs` and an updated `UserDbContextModelSnapshot.cs`.

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Migrations/
git commit -m "feat: add EF migration for ratings and reviews tables"
```

---

## Spec Coverage Check

| Spec requirement | Task |
|---|---|
| `Rating` model: Id, UserId, AttractionId, Value | Task 1 |
| `Review` model: Id, UserId, AttractionId, Content | Task 1 |
| FK from Rating/Review to User | Task 1 |
| Unique index on Rating(UserId, AttractionId) | Task 1 |
| Index on Review(AttractionId) | Task 1 |
| UserDbContext Ratings/Reviews DbSets | Task 1 |
| `UpsertAsync` — creates on first call | Task 2 |
| `UpsertAsync` — updates value on re-submit | Task 2 |
| `UpsertAsync` — rejects value outside 0–5 | Task 2 |
| `GetAttractionAverageAsync` — returns average | Task 3 |
| `GetAttractionAverageAsync` — returns null when no ratings | Task 3 |
| `GetParkAverageAsync` — average of attraction averages | Task 3 |
| `GetParkAverageAsync` — excludes unrated attractions | Task 3 |
| `GetParkAverageAsync` — returns null when none rated | Task 3 |
| `AddAsync` — inserts review | Task 4 |
| Multiple reviews per user per attraction allowed | Task 4 |
| `GetByAttractionAsync` — returns correct set | Task 4 |
| `EditAsync` — updates own review | Task 5 |
| `EditAsync` — returns null for another user's review | Task 5 |
| `DeleteAsync` — deletes own review | Task 5 |
| `DeleteAsync` — returns false for another user's review | Task 5 |
| Services registered in Program.cs | Task 6 |
| EF migration covering both tables | Task 7 |
