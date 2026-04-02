using System.Text.Json.Serialization;

namespace Infinity.WebApi.Models;

public class Attraction
{
    public Guid Id { get; set; }
    public string ParkId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }

    /// <summary>JSON array of image URL strings stored as JSONB.</summary>
    public string? ImageUrls { get; set; }

    /// <summary>JSON array of tag strings stored as JSONB.</summary>
    public string? Tags { get; set; }

    /// <summary>Cached average rating — updated via DB trigger on review write.</summary>
    public decimal AvgRating { get; set; }

    /// <summary>Cached review count — updated via DB trigger.</summary>
    public int ReviewCount { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Park Park { get; set; } = null!;
    public ICollection<AttractionCategory> AttractionCategories { get; set; } = [];
}
