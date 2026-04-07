using System.Text.Json.Serialization;

namespace Infinity.WebApi.Models;

public class Park
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Resort { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }

    [JsonIgnore]
    public ICollection<Attraction> Attractions { get; set; } = [];
}