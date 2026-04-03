namespace Infinity.WebApi.Models;

public class Park
{
    public Guid Id { get; set; } = Guid.Empty;       // e.g. "park_gge_dla"
    public string Name { get; set; } = string.Empty;
    public string? Resort { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }

    public ICollection<Attraction> Attractions { get; set; } = [];
}
