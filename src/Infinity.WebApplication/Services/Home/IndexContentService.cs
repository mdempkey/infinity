using System.Net.Http.Json;
using Infinity.WebApplication.ViewModels.Home;

namespace Infinity.WebApplication.Services.Home;

public sealed class IndexContentService(IConfiguration configuration, HttpClient httpClient) : IIndexContentService
{
    public async Task<IndexViewModel> BuildIndexViewModelAsync()
    {
        var parks = await httpClient.GetFromJsonAsync<List<ParkDto>>("/api/Parks") ?? [];
        var attractions = await httpClient.GetFromJsonAsync<List<AttractionDto>>("/api/Attractions") ?? [];

        var attractionsByPark = attractions
            .GroupBy(a => a.ParkId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return new IndexViewModel
        {
            MapboxAccessToken = configuration["Mapbox:AccessToken"] ?? string.Empty,
            Globe = new GlobeViewModel
            {
                InitialCenter = new CoordinateViewModel { Lng = 20, Lat = 15 },
                InitialZoom = 1.55,
                FocusedZoom = 15.3
            },
            Parks = parks.Select(p => new ParkViewModel
            {
                Id = p.Id,
                Title = p.Name,
                City = p.City ?? string.Empty,
                Country = p.Country ?? string.Empty,
                Coordinates = new CoordinateViewModel
                {
                    Lng = (double)(p.Lng ?? 0),
                    Lat = (double)(p.Lat ?? 0)
                },
                Attractions = attractionsByPark.TryGetValue(p.Id, out var parkAttractions)
                    ? parkAttractions.Select(a => new AttractionViewModel
                    {
                        Title = a.Name,
                        Subtitle = a.Description ?? string.Empty,
                        Rating = (double)a.AvgRating,
                        Coordinates = new CoordinateViewModel
                        {
                            Lng = (double)(a.Lng ?? 0),
                            Lat = (double)(a.Lat ?? 0)
                        },
                        Reviews = []
                    }).ToList()
                    : []
            }).ToList()
        };
    }

    private record ParkDto(
        string Id,
        string Name,
        string? Resort,
        string? City,
        string? Country,
        decimal? Lat,
        decimal? Lng);

    private record AttractionDto(
        string Id,
        string ParkId,
        string Name,
        string? Description,
        decimal? Lat,
        decimal? Lng,
        decimal AvgRating,
        int ReviewCount);
}