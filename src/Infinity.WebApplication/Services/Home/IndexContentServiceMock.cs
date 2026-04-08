using Infinity.WebApplication.ViewModels.Home;

namespace Infinity.WebApplication.Services.Home;

public sealed class IndexContentServiceMock(IConfiguration configuration) : IIndexContentService
{
    public IndexViewModel BuildIndexViewModel()
    {
        return new IndexViewModel
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
                            Rating = 4.1,
                            Coordinates = new CoordinateViewModel { Lng = -117.9184, Lat = 33.8141 },
                            Reviews =
                            [
                                new AttractionReviewViewModel
                                {
                                    Author = "Rey Jakku",
                                    Date = "March 14, 2026",
                                    Comment = "The level of detail is unmatched. It truly feels like Batuu."
                                }
                            ]
                        },
                        new AttractionViewModel
                        {
                            Title = "Star Wars: Rise of the Resistance",
                            Subtitle = "Trackless dark ride adventure",
                            Rating = 4.5,
                            Coordinates = new CoordinateViewModel { Lng = -117.9198, Lat = 33.8127 },
                            Reviews =
                            [
                                new AttractionReviewViewModel
                                {
                                    Author = "Leia Organa",
                                    Date = "March 13, 2026",
                                    Comment = "A masterpiece of storytelling and ride systems."
                                },
                                new AttractionReviewViewModel
                                {
                                    Author = "Luke Skywalker",
                                    Date = "March 9, 2026",
                                    Comment = "Still the most immersive attraction I have experienced."
                                }
                            ]
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
                            Rating = 3.6,
                            Coordinates = new CoordinateViewModel { Lng = 2.7822, Lat = 48.8708 },
                            Reviews = []
                        },
                        new AttractionViewModel
                        {
                            Title = "Hyperspace Mountain",
                            Subtitle = "High-speed indoor coaster",
                            Rating = 4.7,
                            Coordinates = new CoordinateViewModel { Lng = 2.7789, Lat = 48.8731 },
                            Reviews = []
                        }
                    ]
                }
            ]
        };
    }
}
