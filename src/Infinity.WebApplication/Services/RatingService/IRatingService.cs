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
