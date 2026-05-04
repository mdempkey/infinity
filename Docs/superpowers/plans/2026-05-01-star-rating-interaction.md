# Star Rating Interaction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Full-stack interactive star ratings — logged-in users click stars in the attraction widget to instantly submit or update their 1–5 rating, with a glow showing the average and filled stars showing their personal rating.

**Architecture:** Attraction IDs are threaded from the Web API through the C# view-model pipeline into the Mapbox globe JSON config, then passed to the attraction widget's `openWidget` call. On widget open, `GET /api/ratings/{id}/mine` always fires (with JWT if present) to hydrate live average, count, and the user's existing rating. On star click, `POST /api/ratings` upserts and returns the updated summary. Two new JS functions (`renderInteractiveStarRating`, `bindInteractiveStarRating`) handle rendering and interaction, keeping them separate like the existing `hydrateStarRatings` pattern.

**Tech Stack:** ASP.NET Core 10 MVC (C#), NUnit + EF Core InMemory (tests), Bootstrap 5, vanilla JS

---

## File Map

| File | Action | Responsibility |
|---|---|---|
| `src/Infinity.WebApplication/Services/RatingService/IRatingService.cs` | Modify | Add `GetUserRatingValueAsync` and `GetAttractionRatingCountAsync` |
| `src/Infinity.WebApplication/Services/RatingService/RatingService.cs` | Modify | Implement the two new methods |
| `src/Infinity.WebApplication.Tests/Services/RatingServiceTests.cs` | Modify | Tests for the two new methods |
| `src/Infinity.WebApplication/Models/RatingDtos.cs` | Create | `AttractionRatingResponse`, `RateRequest`, `RateResponse` records |
| `src/Infinity.WebApplication/Controllers/RatingController.cs` | Create | `GET /api/ratings/{attractionId}/mine` and `POST /api/ratings` |
| `src/Infinity.WebApplication/ViewModels/Home/IndexViewModel.cs` | Modify | Add `Id` to `AttractionViewModel` |
| `src/Infinity.WebApplication/Services/Home/IndexContentService.cs` | Modify | Populate `Id` from `AttractionDto.Id` |
| `src/Infinity.WebApplication/Views/Shared/_Globe.cshtml` | Modify | Add `id` to attraction JSON serialization |
| `src/Infinity.WebApplication/Views/Shared/_GlobeScript.cshtml` | Modify | Pass `attractionId` in both `openHomepageAttractionWidget` calls |
| `src/Infinity.WebApplication/Views/Shared/_Layout.cshtml` | Modify | Add `.star-rating__star--glow` and `.star-rating__star--glow-half` CSS rules |
| `src/Infinity.WebApplication/Views/Shared/_StarRatingScript.cshtml` | Modify | Add `renderInteractiveStarRating` and `bindInteractiveStarRating` |
| `src/Infinity.WebApplication/Views/Shared/_AttractionWidgetScript.cshtml` | Modify | Accept `attractionId`, fetch/update rating lifecycle |

---

### Task 1: Add `GetUserRatingValueAsync` and `GetAttractionRatingCountAsync` to `RatingService`

**Files:**
- Modify: `src/Infinity.WebApplication/Services/RatingService/IRatingService.cs`
- Modify: `src/Infinity.WebApplication/Services/RatingService/RatingService.cs`
- Modify: `src/Infinity.WebApplication.Tests/Services/RatingServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

Add these tests to `RatingServiceTests.cs` inside the existing `RatingServiceTests` class, after the last existing test:

```csharp
[Test]
public async Task GetUserRatingValueAsync_WhenUserHasRated_ReturnsValue()
{
    var user = await SeedUserAsync();
    var attractionId = Guid.NewGuid();
    await _sut.UpsertAsync(user.Id, attractionId, 3);

    var result = await _sut.GetUserRatingValueAsync(user.Id, attractionId);

    Assert.That(result, Is.EqualTo(3));
}

[Test]
public async Task GetUserRatingValueAsync_WhenUserHasNotRated_ReturnsNull()
{
    var user = await SeedUserAsync();

    var result = await _sut.GetUserRatingValueAsync(user.Id, Guid.NewGuid());

    Assert.That(result, Is.Null);
}

[Test]
public async Task GetAttractionRatingCountAsync_WithRatings_ReturnsCount()
{
    var user1 = await SeedUserAsync("1");
    var user2 = await SeedUserAsync("2");
    var attractionId = Guid.NewGuid();
    await _sut.UpsertAsync(user1.Id, attractionId, 4);
    await _sut.UpsertAsync(user2.Id, attractionId, 2);

    var result = await _sut.GetAttractionRatingCountAsync(attractionId);

    Assert.That(result, Is.EqualTo(2));
}

[Test]
public async Task GetAttractionRatingCountAsync_WithNoRatings_ReturnsZero()
{
    var result = await _sut.GetAttractionRatingCountAsync(Guid.NewGuid());

    Assert.That(result, Is.EqualTo(0));
}

[Test]
public async Task GetAttractionRatingCountAsync_OnlyCountsRatingsForSpecifiedAttraction()
{
    var user1 = await SeedUserAsync("1");
    var user2 = await SeedUserAsync("2");
    var target = Guid.NewGuid();
    var other = Guid.NewGuid();
    await _sut.UpsertAsync(user1.Id, target, 4);
    await _sut.UpsertAsync(user2.Id, other, 3);

    var result = await _sut.GetAttractionRatingCountAsync(target);

    Assert.That(result, Is.EqualTo(1));
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "FullyQualifiedName~GetUserRatingValueAsync|FullyQualifiedName~GetAttractionRatingCountAsync"
```

Expected: build error — `GetUserRatingValueAsync` and `GetAttractionRatingCountAsync` do not exist on `IRatingService`.

- [ ] **Step 3: Add the methods to `IRatingService.cs`**

Replace the entire file with:

```csharp
using Infinity.WebApplication.Models;

namespace Infinity.WebApplication.Services.RatingService;

public interface IRatingService
{
    Task<Rating> UpsertAsync(Guid userId, Guid attractionId, int value);
    Task<int?> GetUserRatingValueAsync(Guid userId, Guid attractionId);
    Task<double?> GetAttractionAverageAsync(Guid attractionId);
    Task<int> GetAttractionRatingCountAsync(Guid attractionId);
    Task<double?> GetParkAverageAsync(IEnumerable<Guid> attractionIds);
}
```

- [ ] **Step 4: Implement the methods in `RatingService.cs`**

Add these two methods to the `RatingService` class, after `UpsertAsync`:

```csharp
public async Task<int?> GetUserRatingValueAsync(Guid userId, Guid attractionId)
{
    var rating = await db.Ratings.FirstOrDefaultAsync(r => r.UserId == userId && r.AttractionId == attractionId);
    return rating?.Value;
}

public async Task<int> GetAttractionRatingCountAsync(Guid attractionId) =>
    await db.Ratings.CountAsync(r => r.AttractionId == attractionId);
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "FullyQualifiedName~GetUserRatingValueAsync|FullyQualifiedName~GetAttractionRatingCountAsync"
```

Expected: 5 tests pass, 0 fail.

- [ ] **Step 6: Run the full test suite to check for regressions**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Infinity.WebApplication/Services/RatingService/IRatingService.cs src/Infinity.WebApplication/Services/RatingService/RatingService.cs src/Infinity.WebApplication.Tests/Services/RatingServiceTests.cs
git commit -m "feat: add GetUserRatingValueAsync and GetAttractionRatingCountAsync to RatingService"
```

---

### Task 2: Create `RatingDtos.cs`

**Files:**
- Create: `src/Infinity.WebApplication/Models/RatingDtos.cs`

- [ ] **Step 1: Create the file**

```csharp
namespace Infinity.WebApplication.Models;

public record AttractionRatingResponse(int? Value, double? Average, int Count);
public record RateRequest(Guid AttractionId, int Value);
public record RateResponse(int Value, double NewAverage, int NewCount);
```

- [ ] **Step 2: Build to verify no compile errors**

```bash
dotnet build src/Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Models/RatingDtos.cs
git commit -m "feat: add RatingDtos"
```

---

### Task 3: Create `RatingController`

**Files:**
- Create: `src/Infinity.WebApplication/Controllers/RatingController.cs`

The controller is intentionally thin — all business logic lives in `IRatingService`. `GET /api/ratings/{attractionId}/mine` is `[AllowAnonymous]` so it serves logged-out users too (they still see the average and count). `POST /api/ratings` is `[Authorize]`.

- [ ] **Step 1: Create the controller**

```csharp
using System.Security.Claims;
using Infinity.WebApplication.Models;
using Infinity.WebApplication.Services.RatingService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Infinity.WebApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RatingController : ControllerBase
{
    private readonly IRatingService _ratingService;

    public RatingController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    [HttpGet("{attractionId}/mine")]
    [AllowAnonymous]
    public async Task<ActionResult<AttractionRatingResponse>> GetMine(Guid attractionId)
    {
        var average = await _ratingService.GetAttractionAverageAsync(attractionId);
        var count = await _ratingService.GetAttractionRatingCountAsync(attractionId);

        int? userRating = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
                userRating = await _ratingService.GetUserRatingValueAsync(userId, attractionId);
        }

        return Ok(new AttractionRatingResponse(userRating, average, count));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<RateResponse>> Rate(RateRequest request)
    {
        if (request.Value is < 1 or > 5)
            return BadRequest("Rating value must be between 1 and 5.");

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        await _ratingService.UpsertAsync(userId, request.AttractionId, request.Value);
        var newAverage = await _ratingService.GetAttractionAverageAsync(request.AttractionId) ?? request.Value;
        var newCount = await _ratingService.GetAttractionRatingCountAsync(request.AttractionId);

        return Ok(new RateResponse(request.Value, newAverage, newCount));
    }
}
```

- [ ] **Step 2: Build to verify no compile errors**

```bash
dotnet build src/Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Controllers/RatingController.cs
git commit -m "feat: add RatingController with GET mine and POST rate endpoints"
```

---

### Task 4: Thread attraction ID through the C# data pipeline

**Files:**
- Modify: `src/Infinity.WebApplication/ViewModels/Home/IndexViewModel.cs`
- Modify: `src/Infinity.WebApplication/Services/Home/IndexContentService.cs`
- Modify: `src/Infinity.WebApplication/Views/Shared/_Globe.cshtml`

- [ ] **Step 1: Add `Id` to `AttractionViewModel`**

In `IndexViewModel.cs`, update `AttractionViewModel` to:

```csharp
public sealed class AttractionViewModel
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required double Rating { get; init; }
    public required CoordinateViewModel Coordinates { get; init; }
    public required IReadOnlyList<AttractionReviewViewModel> Reviews { get; init; }
}
```

- [ ] **Step 2: Populate `Id` in `IndexContentService`**

In `IndexContentService.cs`, update the `AttractionViewModel` initializer inside `BuildIndexViewModelAsync`. Find this block:

```csharp
parkAttractions.Select(a => new AttractionViewModel
{
    Title = a.Name,
    Subtitle = a.Description ?? string.Empty,
    Rating = (double)a.AvgRating,
    Coordinates = new CoordinateViewModel
    {
        Lng = (double)(a.Lng ?? 0),
        Lat = (double)(a.Lat ?? 0)
    },
    Reviews = []
}).ToList()
```

Replace with:

```csharp
parkAttractions.Select(a => new AttractionViewModel
{
    Id = a.Id,
    Title = a.Name,
    Subtitle = a.Description ?? string.Empty,
    Rating = (double)a.AvgRating,
    Coordinates = new CoordinateViewModel
    {
        Lng = (double)(a.Lng ?? 0),
        Lat = (double)(a.Lat ?? 0)
    },
    Reviews = []
}).ToList()
```

- [ ] **Step 3: Add `id` to the attraction JSON in `_Globe.cshtml`**

In `_Globe.cshtml`, find the `attractions` projection inside the `globeConfigJson` serialization:

```csharp
attractions = park.Attractions.Select(attraction => new
{
    title = attraction.Title,
    subtitle = attraction.Subtitle,
    rating = attraction.Rating,
    coordinates = new[] { attraction.Coordinates.Lng, attraction.Coordinates.Lat },
    reviews = attraction.Reviews.Select(review => new
    {
        author = review.Author,
        date = review.Date,
        comment = review.Comment
    })
})
```

Replace with:

```csharp
attractions = park.Attractions.Select(attraction => new
{
    id = attraction.Id,
    title = attraction.Title,
    subtitle = attraction.Subtitle,
    rating = attraction.Rating,
    coordinates = new[] { attraction.Coordinates.Lng, attraction.Coordinates.Lat },
    reviews = attraction.Reviews.Select(review => new
    {
        author = review.Author,
        date = review.Date,
        comment = review.Comment
    })
})
```

- [ ] **Step 4: Build to verify no compile errors**

```bash
dotnet build src/Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/Infinity.WebApplication/ViewModels/Home/IndexViewModel.cs src/Infinity.WebApplication/Services/Home/IndexContentService.cs src/Infinity.WebApplication/Views/Shared/_Globe.cshtml
git commit -m "feat: thread attraction ID through view model and globe config"
```

---

### Task 5: Thread attraction ID through JavaScript

**Files:**
- Modify: `src/Infinity.WebApplication/Views/Shared/_GlobeScript.cshtml`

The `openHomepageAttractionWidget` call appears in two places in `_GlobeScript.cshtml`: inside `addAttractionMarkers` (map marker click) and inside `activateAttractionFromEvent` (attraction panel card click). Both need `attractionId: attraction.id` added.

- [ ] **Step 1: Update the `addAttractionMarkers` call**

Find this block inside `addAttractionMarkers`:

```javascript
window.openHomepageAttractionWidget?.({
    parkTitle: park.title,
    parkLocation: `${park.location}, ${park.country}`,
    attractionTitle: attraction.title,
    attractionSubtitle: attraction.subtitle,
    attractionRating: attraction.rating,
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
    attractionId: attraction.id,
    attractionReviews: attraction.reviews
});
```

- [ ] **Step 2: Update the `activateAttractionFromEvent` call**

Find this block inside `activateAttractionFromEvent`:

```javascript
window.openHomepageAttractionWidget?.({
    parkTitle: focusedPark.title,
    parkLocation: `${focusedPark.location}, ${focusedPark.country}`,
    attractionTitle: attraction.title,
    attractionSubtitle: attraction.subtitle,
    attractionRating: attraction.rating,
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
    attractionId: attraction.id,
    attractionReviews: attraction.reviews
});
```

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Views/Shared/_GlobeScript.cshtml
git commit -m "feat: pass attractionId to openHomepageAttractionWidget"
```

---

### Task 6: Add glow CSS and `renderInteractiveStarRating` / `bindInteractiveStarRating`

**Files:**
- Modify: `src/Infinity.WebApplication/Views/Shared/_Layout.cshtml`
- Modify: `src/Infinity.WebApplication/Views/Shared/_StarRatingScript.cshtml`

The existing `--full` and `--half` CSS classes already apply a drop-shadow glow to colored stars. Two new classes are needed for stars that should glow but not be filled (average > user rating): `--glow` for full positions, `--glow-half` for the fractional half position.

- [ ] **Step 1: Add glow CSS rules to `_Layout.cshtml`**

Find the `.star-rating__value` rule block (around line 398) and add the two new rules immediately after it:

```css
        .star-rating__star--glow {
            filter: drop-shadow(0 0 8px rgba(255, 204, 0, 0.55));
        }

        .star-rating__star--glow-half {
            position: relative;
        }

        .star-rating__star--glow-half::after {
            content: "★";
            position: absolute;
            top: 0;
            left: 0;
            width: 50%;
            overflow: hidden;
            white-space: nowrap;
            filter: drop-shadow(0 0 8px rgba(255, 204, 0, 0.55));
        }
```

- [ ] **Step 2: Replace the entire `_StarRatingScript.cshtml` with the following**

```html
<script>
    (() => {
        const normalizeRating = (rating) => Math.max(0, Math.min(5, Number(rating) || 0));

        const renderStarRating = (rating) => {
            const normalized = normalizeRating(rating);
            const fullStars = Math.floor(normalized);
            const hasPartialStar = normalized % 1 > 0;
            const starsHtml = Array.from({ length: 5 }, (_, index) => {
                if (index < fullStars) {
                    return `<span class="star-rating__star star-rating__star--full" aria-hidden="true">★</span>`;
                }

                if (index === fullStars && hasPartialStar) {
                    return `<span class="star-rating__star star-rating__star--half" aria-hidden="true">★</span>`;
                }

                return `<span class="star-rating__star" aria-hidden="true">★</span>`;
            }).join("");

            return `
                <div class="star-rating star-rating--five" role="img" aria-label="Rated ${normalized.toFixed(1)} out of 5">
                    <span class="star-rating__stars" aria-hidden="true">
                        ${starsHtml}
                    </span>
                    <span class="star-rating__value">${normalized.toFixed(1)}</span>
                </div>
            `;
        };

        const hydrateStarRatings = (root = document) => {
            root.querySelectorAll("[data-star-rating]").forEach((node) => {
                node.innerHTML = renderStarRating(node.dataset.starRating);
            });
        };

        const renderInteractiveStarRating = ({ attractionId, average, count, userRating, interactive }) => {
            const normalizedAverage = normalizeRating(average ?? 0);
            const filledValue = userRating != null ? userRating : normalizedAverage;
            const normalizedFilled = normalizeRating(filledValue);

            const fullFilled = Math.floor(normalizedFilled);
            const hasHalfFilled = normalizedFilled % 1 > 0;

            const fullGlow = Math.floor(normalizedAverage);
            const hasHalfGlow = normalizedAverage % 1 > 0;

            const starsHtml = Array.from({ length: 5 }, (_, i) => {
                const inFillFull = i < fullFilled;
                const inFillHalf = !inFillFull && i === fullFilled && hasHalfFilled;
                const inGlowFull = i < fullGlow;
                const inGlowHalf = !inGlowFull && i === fullGlow && hasHalfGlow;

                // Fill takes priority; glow-only classes apply only to unfilled stars
                let starClass;
                if (inFillFull) starClass = "star-rating__star--full";
                else if (inFillHalf) starClass = "star-rating__star--half";
                else if (inGlowFull) starClass = "star-rating__star--glow";
                else if (inGlowHalf) starClass = "star-rating__star--glow-half";
                else starClass = "";

                const classes = ["star-rating__star", starClass].filter(Boolean).join(" ");
                const value = i + 1;

                if (interactive) {
                    return `<button type="button" class="${classes}" data-value="${value}" aria-label="Rate ${value} star${value !== 1 ? "s" : ""}">★</button>`;
                }
                return `<span class="${classes}" aria-hidden="true">★</span>`;
            }).join("");

            const avgText = average != null ? Number(average).toFixed(1) : "…";
            const countText = count != null ? String(count) : "…";
            const label = interactive ? "group" : "img";

            return `<div class="star-rating star-rating--five star-rating--widget"
                         data-attraction-id="${attractionId}"
                         data-average="${average ?? ""}"
                         data-count="${count ?? ""}"
                         data-user-rating="${userRating ?? ""}"
                         role="${label}"
                         aria-label="Rated ${avgText} out of 5">
                <span class="star-rating__stars" aria-hidden="true">${starsHtml}</span>
                <span class="star-rating__value">${avgText} (of ${countText})</span>
            </div>`;
        };

        const bindInteractiveStarRating = (container, onRate) => {
            const buttons = Array.from(container.querySelectorAll("button[data-value]"));
            if (buttons.length === 0) return;

            const getSavedFill = () => {
                const userRating = container.dataset.userRating !== "" ? Number(container.dataset.userRating) : null;
                const avg = container.dataset.average !== "" ? Number(container.dataset.average) : 0;
                return userRating ?? avg;
            };

            const updateFill = (fillValue) => {
                const norm = normalizeRating(fillValue ?? 0);
                const full = Math.floor(norm);
                const hasHalf = norm % 1 > 0;
                buttons.forEach((btn, i) => {
                    btn.classList.remove("star-rating__star--full", "star-rating__star--half");
                    if (i < full) btn.classList.add("star-rating__star--full");
                    else if (i === full && hasHalf) btn.classList.add("star-rating__star--half");
                });
            };

            buttons.forEach((btn) => {
                btn.addEventListener("mouseover", () => updateFill(Number(btn.dataset.value)));
                btn.addEventListener("mouseout", () => updateFill(getSavedFill()));
                btn.addEventListener("click", async () => {
                    const value = Number(btn.dataset.value);
                    const previousFill = getSavedFill();
                    updateFill(value);
                    try {
                        await onRate(value);
                        container.dataset.userRating = String(value);
                    } catch {
                        updateFill(previousFill);
                    }
                });
            });
        };

        window.renderHomepageStarRating = renderStarRating;
        window.hydrateStarRatings = hydrateStarRatings;
        window.renderInteractiveStarRating = renderInteractiveStarRating;
        window.bindInteractiveStarRating = bindInteractiveStarRating;

        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", () => hydrateStarRatings());
        } else {
            hydrateStarRatings();
        }
    })();
</script>
```

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Views/Shared/_Layout.cshtml src/Infinity.WebApplication/Views/Shared/_StarRatingScript.cshtml
git commit -m "feat: add glow CSS and renderInteractiveStarRating / bindInteractiveStarRating"
```

---

### Task 7: Wire rating fetch and POST into `_AttractionWidgetScript.cshtml`

**Files:**
- Modify: `src/Infinity.WebApplication/Views/Shared/_AttractionWidgetScript.cshtml`

This is the final wiring task. The widget gains a `currentAttractionId` state variable, a `fetchAndRenderRating` async function (called on open), and a `handleRate` async function (the `onRate` callback). The `openWidget` function is updated to accept `attractionId` and the `closeWidget` function clears `currentAttractionId`.

`handleRate` throws on failure so `bindInteractiveStarRating`'s click handler can catch it and revert the optimistic fill.

- [ ] **Step 1: Replace the entire file with the following**

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
            if (!overlay) {
                return;
            }

            overlay.hidden = true;
            getHomePage()?.classList.remove("is-attraction-widget-open");
            currentAttractionId = null;
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

        const openWidget = ({ parkTitle, parkLocation, attractionTitle, attractionSubtitle, attractionRating, attractionId, attractionReviews }) => {
            const overlay = getOverlay();
            const parkNode = getParkNode();
            const titleNode = getTitleNode();
            const ratingNode = getRatingNode();
            const subtitleNode = getSubtitleNode();
            const reviewsListNode = getReviewsListNode();
            if (!overlay || !parkNode || !titleNode || !ratingNode || !subtitleNode || !reviewsListNode) {
                return;
            }

            currentAttractionId = attractionId;

            parkNode.textContent = `${parkTitle} - ${parkLocation}`;
            titleNode.textContent = attractionTitle;
            subtitleNode.textContent = attractionSubtitle;
            reviewsListNode.innerHTML = window.renderHomepageAttractionReviews?.(attractionReviews) ?? "";

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
        };

        const bindWidgetInteractions = () => {
            const overlay = getOverlay();
            const closeButton = getCloseButton();
            if (!overlay || !closeButton) {
                return;
            }

            closeButton.addEventListener("click", closeWidget);
            overlay.addEventListener("click", (event) => {
                if (event.target === overlay) {
                    closeWidget();
                }
            });

            document.addEventListener("keydown", (event) => {
                if (event.key === "Escape") {
                    closeWidget();
                }
            });
        };

        bindWidgetInteractions();
        closeWidget();
        window.openHomepageAttractionWidget = openWidget;
        window.closeHomepageAttractionWidget = closeWidget;
    })();
</script>
```

- [ ] **Step 2: Build to verify no compile errors**

```bash
dotnet build src/Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: `Build succeeded` with 0 errors.

- [ ] **Step 3: Run the full test suite**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 4: Manual smoke test**

Start the app:

```bash
dotnet run --project src/Infinity.WebApplication/Infinity.WebApplication.csproj
```

Verify each scenario in the browser:

| Scenario | Expected |
|---|---|
| Open attraction widget while logged out | Stars render immediately (read-only) with average fill and glow; numeric label shows `X.X (of N)` after the fetch resolves |
| Open attraction widget while logged in, never rated this attraction | Stars become interactive (hover previews work); filled = average, glow = average; numeric label shows `X.X (of N)` |
| Click a star while logged in | Clicked star highlights optimistically; after POST, filled stars update to clicked value, glow and label update to new average and count |
| Open widget for same attraction again after rating | Filled stars show your saved rating; glow still shows average |
| Open widget, click a star, server returns error | Fill reverts to previous state |
| Close widget and open a different attraction | Stars reflect the new attraction's data, not the previous one |

- [ ] **Step 5: Commit**

```bash
git add src/Infinity.WebApplication/Views/Shared/_AttractionWidgetScript.cshtml
git commit -m "feat: wire interactive star rating into attraction widget"
```
