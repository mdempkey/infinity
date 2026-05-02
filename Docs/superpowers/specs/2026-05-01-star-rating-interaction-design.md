# Star Rating Interaction Design

**Date:** 2026-05-01
**Branch:** `frontend/signup-form`
**Scope:** Full-stack interactive star ratings — logged-in users can click stars in the attraction widget to submit or update their rating

---

## Overview

Logged-in users can click stars in the attraction widget to instantly save a 1–5 rating for an attraction. The star display has two visual layers: a glow that always shows the average rating across all submissions, and filled stars that show the user's own rating (if they've rated) or the average (if they haven't). Ratings and reviews are completely separate features.

---

## Section 1: Data Flow

The attraction ID travels from the Web API through the view model pipeline to the frontend JS config.

| Layer | Change |
|---|---|
| `ViewModels/Home/IndexViewModel.cs` | Add `string Id { get; init; }` to `AttractionViewModel` |
| `Services/Home/IndexContentService.cs` | Populate `Id` from `AttractionDto.Id` (already in the record, currently unused) |
| `Views/Shared/_Globe.cshtml` | Add `id = attraction.Id` to the attraction object in the JSON config |
| `Views/Shared/_GlobeScript.cshtml` | Pass `attractionId: attraction.id` in both `openHomepageAttractionWidget(...)` call sites |
| `Views/Shared/_AttractionWidgetScript.cshtml` | Accept `attractionId` in `openWidget`; store in IIFE scope while widget is open; clear on close |

---

## Section 2: API

New controller: `Controllers/RatingController.cs`. Both endpoints require `[Authorize]`. User ID is extracted from `ClaimTypes.NameIdentifier` on the JWT (same claim written by `AuthController.GenerateToken`).

### `GET /api/ratings/{attractionId}/mine`

Returns the current rating summary for one attraction, plus the authenticated user's own rating if a valid JWT is present.

- **Auth:** `[AllowAnonymous]` — no token required, but if a valid JWT is present `value` is populated
- **Response:** `{ value: int?, average: double?, count: int }` — `value` is `null` if unauthenticated or unrated; `average` is `null` if no ratings exist yet

### `POST /api/ratings`

Submits or updates the authenticated user's rating.

- **Body:** `{ attractionId: Guid, value: int }`
- **Validation:** `value` must be 1–5; return 400 otherwise
- **Logic:** calls `RatingService.UpsertAsync`, then `RatingService.GetAttractionAverageAsync` and `RatingService.GetAttractionRatingCountAsync` for the fresh summary
- **Response:** `{ value: int, newAverage: double, newCount: int }`
- **Auth failure:** 401

### DTOs

New file `Models/RatingDtos.cs` (mirrors the existing `AuthDtos.cs` pattern):

```csharp
public record AttractionRatingResponse(int? Value, double? Average, int Count);
public record RateRequest(Guid AttractionId, int Value);
public record RateResponse(int Value, double NewAverage, int NewCount);
```

`IRatingService` gains one new method: `GetAttractionRatingCountAsync(Guid attractionId)` returning `Task<int>`.

---

## Section 3: Star Rendering

`Views/Shared/_StarRatingScript.cshtml` gains one new exported function. The existing `renderStarRating` (used on attraction panel cards) is unchanged.

### `renderInteractiveStarRating(attractionId, average, userRating, interactive)`

Returns an HTML string for insertion into `#attraction-widget-rating`.

- `average` — the attraction's current average rating (number)
- `userRating` — the user's saved rating (`int | null`)
- `interactive` — `true` renders star buttons; `false` renders star spans (read-only)

**Filled stars:** `userRating ?? average`
- If the user has rated: their integer value determines how many stars are filled
- If not rated: the fractional average is shown with full/half/empty logic (same as the existing read-only display)

**Glow:** Each star that falls within the average rating gets a CSS glow class (e.g. `star-rating__star--glow`, implemented with `filter: drop-shadow` or `text-shadow`). The glow follows the same full/half/empty progression as the filled stars, so a 3.5 average produces a glow on the first 3 full stars and the half star. Glow intensity is fixed — the extent of the glow communicates the average naturally, no scaling needed.

**Numeric summary:** Displayed next to the stars in the format `3.5 (of 42)` — average rounded to 1 decimal place, followed by the total rating count in parentheses. Both values update when the POST response returns `newAverage` and `newCount`.

**Interactive mode** (`interactive = true`):
- Each star is a `<button data-value="N">` (1–5)
- Hover and click handlers are bound separately after insertion (see below)

**Read-only mode** (`interactive = false`, logged-out or loading state):
- Each star is a `<span>` — no interaction possible
- Glow and filled stars still render

### `bindInteractiveStarRating(container, onRate)`

Called by the widget script after inserting the HTML. Wires all interaction for the container:

- **Hover:** on `mouseover` a star button, preview fill updates to that star's position; glow stays fixed. On `mouseout`, display reverts to the saved state.
- **Click:** on click, calls `onRate(value)` — the callback provided by the widget script, which fires the POST.

Keeping rendering (`renderInteractiveStarRating`) and behaviour (`bindInteractiveStarRating`) separate mirrors the existing `hydrateStarRatings` pattern and lets the widget script own the API call without the star script knowing about it.

---

## Section 4: Widget Integration

`Views/Shared/_AttractionWidgetScript.cshtml` coordinates the fetch-and-render lifecycle.

### On `openWidget({ ..., attractionId })`

1. Store `attractionId` in IIFE scope
2. Render read-only stars immediately (`interactive = false`, using `attraction.rating` from page data for the initial average, count shown as `…`) — visible with no loading flicker
3. Always call `GET /api/ratings/{attractionId}/mine`, sending `Authorization: Bearer {token}` if `getToken()` is set
   - On success: re-render with live `{ average, count, value }` from the response; if `getToken()` is set, render `interactive = true` and call `bindInteractiveStarRating(container, onRate)`; otherwise render `interactive = false`
   - On error: leave the initial read-only display in place silently

### On star click (`onRate` callback)

1. `POST /api/ratings` with `{ attractionId, value }` and `Authorization: Bearer {token}`
2. Clicked star highlights optimistically on click
3. On success: re-render with updated `{ value, newAverage, newCount }` — filled stars reflect saved rating, glow and numeric summary update to new average and count; re-bind handlers via `bindInteractiveStarRating`
4. On error: revert optimistic highlight silently

### On `closeWidget`

Clear the stored `attractionId` to prevent stale state leaking to the next opened attraction.

---

## Files Changed

| File | Action |
|---|---|
| `ViewModels/Home/IndexViewModel.cs` | Add `Id` to `AttractionViewModel` |
| `Services/Home/IndexContentService.cs` | Populate `Id` from `AttractionDto` |
| `Views/Shared/_Globe.cshtml` | Serialize `id` into attraction JSON |
| `Views/Shared/_GlobeScript.cshtml` | Pass `attractionId` to `openWidget` |
| `Views/Shared/_AttractionWidgetScript.cshtml` | Accept `attractionId`, fetch/update rating lifecycle |
| `Views/Shared/_StarRatingScript.cshtml` | Add `renderInteractiveStarRating` and `bindInteractiveStarRating` |
| `Controllers/RatingController.cs` | New — two endpoints |
| `Models/RatingDtos.cs` | New — three records |
| `Services/RatingService/IRatingService.cs` | Add `GetAttractionRatingCountAsync` |
| `Services/RatingService/RatingService.cs` | Implement `GetAttractionRatingCountAsync` |

---

## Out of Scope

- Deleting a rating (no requirement to un-rate)
- Any changes to reviews
- Token expiry handling beyond silently falling back to read-only
