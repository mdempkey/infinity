using System.Text.Json.Serialization;

namespace Infinity.WebApi.Models;

public class AttractionCategory
{
    public Guid AttractionId { get; set; }
    public Guid CategoryId { get; set; }

    [JsonIgnore]
    public Attraction Attraction { get; set; } = null!;

    [JsonIgnore]
    public Category Category { get; set; } = null!;
}
