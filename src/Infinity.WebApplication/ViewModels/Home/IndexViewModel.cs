namespace Infinity.WebApplication.ViewModels.Home;

public sealed class IndexViewModel
{
    public required string MapboxAccessToken { get; init; }
    public required IReadOnlyList<ParkViewModel> Parks { get; init; }
    public required GlobeViewModel Globe { get; init; }
}

public sealed class GlobeViewModel
{
    public required CoordinateViewModel InitialCenter { get; init; }
    public required double InitialZoom { get; init; }
    public required double FocusedZoom { get; init; }
}

public sealed class ParkViewModel
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string City { get; init; }
    public required string Country { get; init; }
    public required CoordinateViewModel Coordinates { get; init; }
    public required IReadOnlyList<AttractionViewModel> Attractions { get; init; }
}

public sealed class AttractionViewModel
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required double Rating { get; init; }
    public required CoordinateViewModel Coordinates { get; init; }
    public required IReadOnlyList<AttractionReviewViewModel> Reviews { get; init; }
}

public sealed class AttractionReviewViewModel
{
    public required string Author { get; init; }
    public required string Date { get; init; }
    public required string Comment { get; init; }
}

public sealed class CoordinateViewModel
{
    public required double Lng { get; init; }
    public required double Lat { get; init; }
}
