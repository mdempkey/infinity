namespace Infinity.WebApi.Models;

public class AttractionCategory
{
    public Guid AttractionId { get; set; }
    public Guid CategoryId { get; set; }

    public Attraction Attraction { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
