---
title: Reviews Feature Design
date: 2026-05-05
status: approved
---

# Reviews Feature

## Overview

Add a fully functional reviews system to the Infinity attraction widget. Users can submit multiple written reviews per attraction, and authors can edit or delete their own reviews inline. Reviews display author username, last-modified date, and content.

## Data Model

Add `ModifiedAt` to the `Review` entity in `Infinity.WebApplication/Models/Review.cs`:

```csharp
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

- `ModifiedAt` is set to `DateTime.UtcNow` on both creation and edit.
- A new EF Core migration adds the column to the database.

## API Contracts

New `ReviewController` at `Infinity.WebApplication/Controllers/ReviewController.cs`, registered at `/api/reviews`.

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/reviews/{attractionId}` | Optional | Returns all reviews for an attraction |
| POST | `/api/reviews` | Required | Submits a new review |
| PUT | `/api/reviews/{reviewId}` | Required | Edits the authenticated user's review |
| DELETE | `/api/reviews/{reviewId}` | Required | Deletes the authenticated user's review |

### GET `/api/reviews/{attractionId}` — Response (200 OK)

```json
[
  {
    "id": "uuid",
    "author": "username",
    "date": "May 5, 2026",
    "comment": "Great ride!",
    "isOwner": true
  }
]
```

- `isOwner` is `true` when the JWT-authenticated user's ID matches the review's `UserId`. Anonymous users always receive `false`.
- Date is formatted server-side as `"MMMM d, yyyy"`.

### POST `/api/reviews` — Request body

```json
{ "attractionId": "uuid", "content": "string (1–2000 chars)" }
```

Returns the created review object (same shape as GET item). Returns `400` if content is empty/whitespace or exceeds 2000 characters.

### PUT `/api/reviews/{reviewId}` — Request body

```json
{ "content": "string (1–2000 chars)" }
```

Returns the updated review object. Returns `400` if content is empty/whitespace or exceeds 2000 characters. Returns `404` if the review does not exist or does not belong to the authenticated user.

### DELETE `/api/reviews/{reviewId}`

Returns `204 No Content` on success. Returns `404` if the review does not exist or does not belong to the authenticated user.

## Service Layer

`ReviewService.GetByAttractionAsync` is updated to `.Include(r => r.User)` so author usernames are available without a second query.

`ReviewService.AddAsync` sets `ModifiedAt = DateTime.UtcNow`.

`ReviewService.EditAsync` updates `ModifiedAt = DateTime.UtcNow` on save.

Content validation (1–2000 characters) is enforced in `ReviewService` for both add and edit.

## Client-side Behavior

### Loading reviews

When the attraction widget opens, `fetchAndRenderReviews(attractionId)` is called (parallel to the existing `fetchAndRenderRating` call). It calls `GET /api/reviews/{attractionId}` and passes the result to `renderHomepageAttractionReviews`.

The list is re-fetched after any submit, edit, or delete action.

### Submitting a review

The Submit button in `_Reviews.cshtml` calls `POST /api/reviews` with the textarea content and the JWT bearer token. On success, the textarea is cleared and the reviews list re-fetches.

### Editing a review

Reviews where `isOwner: true` render inline **Edit** and **Delete** buttons.

Clicking **Edit**:
1. Replaces the review `<p>` with a pre-filled `<textarea>` (maxlength 2000) and a `##/2000` character counter (right-aligned, updates live).
2. Shows **Save** and **Cancel** buttons.
3. **Save** calls `PUT /api/reviews/{id}`, then re-fetches the list.
4. **Cancel** restores the original rendered text with no network call.

### Deleting a review

Clicking **Delete** calls `DELETE /api/reviews/{id}`, then re-fetches the list.

### Character counter

Both the draft textarea (`_Reviews.cshtml`) and the inline edit textarea display a right-aligned `##/2000` counter that updates on every keystroke. The `maxlength` attribute on both textareas is set to `2000`.

### Authentication

All mutating API calls include `Authorization: Bearer <token>` via `window.getToken?.()`, matching the pattern used by the ratings feature.

## Files Changed

| File | Change |
|------|--------|
| `Models/Review.cs` | Add `ModifiedAt` property |
| `Services/ReviewService/ReviewService.cs` | Set `ModifiedAt` on add/edit; include User on get |
| `Controllers/ReviewController.cs` | New file — GET, POST, PUT, DELETE endpoints |
| `Data/UserDbContext.cs` | Verify `Reviews` DbSet is registered |
| `Migrations/` | New migration for `ModifiedAt` column |
| `Views/Shared/_Reviews.cshtml` | Wire submit button; update maxlength to 2000; add character counter |
| `Views/Shared/_ReviewsScript.cshtml` | Update renderer to handle `isOwner`, edit/delete buttons, inline edit UX, character counter |
| `Views/Shared/_AttractionWidgetScript.cshtml` | Add `fetchAndRenderReviews`; remove `attractionReviews` from `openWidget` |

## Out of Scope

- One review per attraction limit (multiple reviews per user per attraction are allowed)
- "Edited" label on modified reviews (date reflects last modification implicitly)
- Admin moderation or review flagging
- Pagination of reviews
