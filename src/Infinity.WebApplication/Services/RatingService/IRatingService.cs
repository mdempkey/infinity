using Infinity.WebApplication.Models;

namespace Infinity.WebApplication.Services.RatingService;

public interface IRatingService
{
    Task<Rating> UpsertAsync(Guid userId, Guid attractionId, int value);
    Task<double?> GetAttractionAverageAsync(Guid attractionId);
    Task<double?> GetParkAverageAsync(IEnumerable<Guid> attractionIds);
}
