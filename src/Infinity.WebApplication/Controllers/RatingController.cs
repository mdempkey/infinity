using System.Security.Claims;
using Infinity.WebApplication.Models;
using Infinity.WebApplication.Services.RatingService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Infinity.WebApplication.Controllers;

[ApiController]
[Route("api/ratings")]
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
