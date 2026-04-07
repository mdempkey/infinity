namespace Infinity.WebApi.Models;

public class AttractionCategory
{
    public string AttractionId { get; set; }
    public string CategoryId { get; set; }

    public Attraction Attraction { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
