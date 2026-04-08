using Infinity.WebApplication.ViewModels.Home;

namespace Infinity.WebApplication.Services.Home;

public sealed class HomePageContentService(IConfiguration configuration) : IHomePageContentService
{
    public HomeIndexViewModel BuildHomeIndexViewModel()
    {
        return new HomeIndexViewModel
        {
            MapboxAccessToken = configuration["Mapbox:AccessToken"] ?? string.Empty,
            Globe = new GlobeViewModel
            {
                InitialCenter = new CoordinateViewModel { Lng = 20, Lat = 15 },
                InitialZoom = 1.55,
                FocusedZoom = 15.3
            },
            Parks =
            [
                new ParkViewModel
                {
                    Id = "disneyland",
                    Title = "Disneyland Resort",
                    City = "Anaheim",
                    Country = "United States",
                    Coordinates = new CoordinateViewModel { Lng = -117.9190, Lat = 33.8121 },
                    Attractions =
                    [
                        new AttractionViewModel
                        {
                            Title = "Star Wars: Galaxy's Edge",
                            Subtitle = "Immersive Batuu land experience",
                            Coordinates = new CoordinateViewModel { Lng = -117.9184, Lat = 33.8141 }
                        },
                        new AttractionViewModel
                        {
                            Title = "Star Wars: Rise of the Resistance",
                            Subtitle = "Trackless dark ride adventure",
                            Coordinates = new CoordinateViewModel { Lng = -117.9198, Lat = 33.8127 }
                        }
                    ]
                },
                new ParkViewModel
                {
                    Id = "disneyland-paris",
                    Title = "Disneyland Paris",
                    City = "Marne-la-Vallee",
                    Country = "France",
                    Coordinates = new CoordinateViewModel { Lng = 2.7758, Lat = 48.8722 },
                    Attractions =
                    [
                        new AttractionViewModel
                        {
                            Title = "Star Tours: The Adventures Continue",
                            Subtitle = "3D motion simulator space flight",
                            Coordinates = new CoordinateViewModel { Lng = 2.7822, Lat = 48.8708 }
                        },
                        new AttractionViewModel
                        {
                            Title = "Hyperspace Mountain",
                            Subtitle = "High-speed indoor coaster",
                            Coordinates = new CoordinateViewModel { Lng = 2.7789, Lat = 48.8731 }
                        }
                    ]
                }
            ]
        };
    }
}
