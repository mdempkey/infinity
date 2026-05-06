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
