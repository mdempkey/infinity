using System.Text.Json.Serialization;

namespace Infinity.WebApi.Models;

public class Category
{
    public string Id { get; set; }
    public string Name { get; set; } = string.Empty;     // e.g. "Ride", "Restaurant"
    public string? Description { get; set; }

    [JsonIgnore]
    public ICollection<AttractionCategory> AttractionCategories { get; set; } = [];
}
