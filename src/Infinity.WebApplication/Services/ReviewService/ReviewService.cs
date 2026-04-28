using Infinity.WebApplication.Data;
using Infinity.WebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Services.ReviewService;

public sealed class ReviewService(UserDbContext db) : IReviewService
{
    public async Task<Review> AddAsync(Guid userId, Guid attractionId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Review content cannot be empty.", nameof(content));
        if (content.Length > 2000)
            throw new ArgumentException("Review content cannot exceed 2000 characters.", nameof(content));

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
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Review content cannot be empty.", nameof(content));
        if (content.Length > 2000)
            throw new ArgumentException("Review content cannot exceed 2000 characters.", nameof(content));

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
