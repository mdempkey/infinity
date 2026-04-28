using Infinity.WebApplication.Models;

namespace Infinity.WebApplication.Services.ReviewService;

public interface IReviewService
{
    Task<Review> AddAsync(Guid userId, Guid attractionId, string content);
    Task<IEnumerable<Review>> GetByAttractionAsync(Guid attractionId);
    Task<Review?> EditAsync(Guid reviewId, Guid userId, string content);
    Task<bool> DeleteAsync(Guid reviewId, Guid userId);
}
