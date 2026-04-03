namespace Infinity.WebApi.Models;

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;     // e.g. "Ride", "Restaurant"
    public string? Description { get; set; }

    public ICollection<AttractionCategory> AttractionCategories { get; set; } = [];
}
