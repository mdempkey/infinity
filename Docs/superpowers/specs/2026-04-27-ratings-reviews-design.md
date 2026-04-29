# Ratings & Reviews Design

**Date:** 2026-04-27
**Scope:** Attraction ratings and reviews stored in the WebApp-owned `userdb`

---

## Overview

Users can rate and review attractions independently. Ratings are one per user per attraction (upsert on re-submit). Reviews allow multiple per user per attraction. Both reference `AttractionId` as a plain `Guid` — no foreign key constraint, since attractions live in the WebAPI's separate locations database.

---

## 1. Data Models

### Rating

| Field | Type | Constraints |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → `users.id` |
| `AttractionId` | `Guid` | reference only, no FK |
| `Value` | `int` | 0–5, validated in service |

Unique index on `(UserId, AttractionId)`.

### Review

| Field | Type | Constraints |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → `users.id` |
| `AttractionId` | `Guid` | reference only, no FK |
| `Content` | `string` | required |

Index on `AttractionId` for querying reviews by attraction.

---

## 2. DbContext

`UserDbContext` gains two new `DbSet`s:

```csharp
public DbSet<Rating> Ratings => Set<Rating>();
public DbSet<Review> Reviews => Set<Review>();
```

`OnModelCreating` configuration:
- Both `Rating` and `Review` get a FK relationship to `User` on `UserId`
- `Rating` gets a unique index on `(UserId, AttractionId)`
- `Review` gets an index on `AttractionId`

---

## 3. Service Layer

Controllers never access `UserDbContext` directly. All operations go through services.

### IRatingService / RatingService

```csharp
Task<Rating?> UpsertAsync(Guid userId, Guid attractionId, int value);
Task<double?> GetAttractionAverageAsync(Guid attractionId);
Task<double?> GetParkAverageAsync(IEnumerable<Guid> attractionIds);
```

- `UpsertAsync` — returns `null` if `value` is outside 0–5. Uses `FirstOrDefaultAsync` to check for an existing rating: if found, updates `Value` and saves; if not found, inserts a new `Rating`.
- `GetAttractionAverageAsync` — returns the average of all ratings for the attraction, or `null` if no ratings exist.
- `GetParkAverageAsync` — accepts a list of attraction IDs (fetched from the WebAPI by the caller) and returns the average of all those attractions' average ratings, or `null` if no ratings exist across any of them. Attractions with no ratings are excluded from the average.

### IReviewService / ReviewService

```csharp
Task<Review> AddAsync(Guid userId, Guid attractionId, string content);
Task<IEnumerable<Review>> GetByAttractionAsync(Guid attractionId);
```

- `AddAsync` — inserts a new review and returns it. Multiple reviews per user per attraction are allowed.
- `GetByAttractionAsync` — returns all reviews for the given attraction.
- `EditAsync(Guid reviewId, Guid userId, string content)` — updates `Content` on the review. Returns the updated `Review`, or `null` if the review does not exist or does not belong to `userId`.
- `DeleteAsync(Guid reviewId, Guid userId)` — deletes the review. Returns `true` on success, `false` if the review does not exist or does not belong to `userId`.

Both services registered as scoped in `Program.cs`.

---

## 4. EF Migration

A new migration covers the two new tables. Applied automatically via `db.Database.MigrateAsync()` on startup (already wired in `Program.cs`).

---

## 5. Testing

Tests in `Infinity.WebApplication.Tests` using the EF in-memory provider.

**RatingServiceTests**
- `UpsertAsync` with a new user+attraction creates a rating
- `UpsertAsync` called twice updates the value (does not insert a second row)
- `UpsertAsync` with value outside 0–5 returns `null`
- `GetAttractionAverageAsync` returns correct average across multiple ratings
- `GetAttractionAverageAsync` returns `null` when no ratings exist
- `GetParkAverageAsync` returns average of attraction averages, excluding unrated attractions
- `GetParkAverageAsync` returns `null` when no ratings exist for any of the given attractions

**ReviewServiceTests**
- `AddAsync` inserts a review and returns it
- Multiple `AddAsync` calls from the same user on the same attraction all succeed
- `GetByAttractionAsync` returns only reviews for the specified attraction
- `EditAsync` updates content when the review belongs to the user, returns `null` otherwise
- `DeleteAsync` removes the review when it belongs to the user, returns `false` otherwise

---

## 6. File Changes

**Modify:**
- `src/Infinity.WebApplication/Models/Rating.cs` — replace stub with full model
- `src/Infinity.WebApplication/Models/Review.cs` — replace stub with full model
- `src/Infinity.WebApplication/Data/UserDbContext.cs` — add `Ratings` and `Reviews` DbSets and configuration
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

## Non-Goals

- Ratings or reviews on parks (park scores are derived from attraction averages only)
- Pagination of reviews
