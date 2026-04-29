using Infinity.WebApplication.Data;
using Infinity.WebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Services.RatingService;

public sealed class RatingService(UserDbContext db) : IRatingService
{
    public async Task<Rating> UpsertAsync(Guid userId, Guid attractionId, int value)
    {
        if (value < 0 || value > 5)
            throw new ArgumentException("Rating value must be between 0 and 5.", nameof(value));

        var existing = await db.Ratings.FirstOrDefaultAsync(r => r.UserId == userId && r.AttractionId == attractionId);
        if (existing is not null)
        {
            existing.Value = value;
            await db.SaveChangesAsync();
            return existing;
        }

        var rating = new Rating
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AttractionId = attractionId,
            Value = value
        };
        db.Ratings.Add(rating);
        await db.SaveChangesAsync();
        return rating;
    }


    public async Task<double?> GetAttractionAverageAsync(Guid attractionId)
    {
        var averages = await db.Ratings
            .Where(r => r.AttractionId == attractionId)
            .Select(r => (double?)r.Value)
            .ToListAsync();

        return averages.Count == 0 ? null : averages.Average();
    }

    public async Task<double?> GetParkAverageAsync(IEnumerable<Guid> attractionIds)
    {
        var ids = attractionIds.ToList();
        if (ids.Count == 0) return null;

        var attractionAverages = await db.Ratings
            .Where(r => ids.Contains(r.AttractionId))
            .GroupBy(r => r.AttractionId)
            .Select(g => g.Average(r => (double)r.Value))
            .ToListAsync();

        return attractionAverages.Count == 0 ? null : attractionAverages.Average();
    }
}
