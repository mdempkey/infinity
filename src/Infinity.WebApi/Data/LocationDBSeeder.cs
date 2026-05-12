using Infinity.WebApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Infinity.WebApi.Data;

public static class LocationsDbSeeder
{
    private static string Imgs(params string[] filePaths) =>
        JsonSerializer.Serialize(filePaths.Select(f => $"/api/attractions/image/{f}"));

    private static readonly string SmugglersRunImgs = Imgs(
        "02_rides/millennium_falcon_smugglers_run/smug1.webp",
        "02_rides/millennium_falcon_smugglers_run/smug2.jpg",
        "02_rides/millennium_falcon_smugglers_run/smug4.jpg",
        "02_rides/millennium_falcon_smugglers_run/smug5.jpg"
    );

    private static readonly string RiseImgs = Imgs(
        "02_rides/rise_of_the_resistance/rise1.webp",
        "02_rides/rise_of_the_resistance/rise2.jpg",
        "02_rides/rise_of_the_resistance/rise4.jpg"
    );

    private static readonly string DockingBay7Imgs = Imgs(
        "05_disneyland_ca/docking_bay_7/bay1.webp",
        "05_disneyland_ca/docking_bay_7/bay2.jpg",
        "05_disneyland_ca/docking_bay_7/bay3.jpg",
        "05_disneyland_ca/docking_bay_7/bay4.jpg",
        "05_disneyland_ca/docking_bay_7/bay5.jpg"
    );

    private static readonly string OgasCantinaImgs = Imgs(
        "05_disneyland_ca/ogas_cantina/oga1.jpg",
        "05_disneyland_ca/ogas_cantina/oag2.jpg",
        "05_disneyland_ca/ogas_cantina/oga3.webp",
        "05_disneyland_ca/ogas_cantina/oga4.jpg"
    );

    private static readonly string SavisWorkshopImgs = Imgs(
        "04_experiences/savs_workshop_lightsabers/light1.jpg",
        "04_experiences/savs_workshop_lightsabers/light2.jpg",
        "04_experiences/savs_workshop_lightsabers/light3.jpg",
        "04_experiences/savs_workshop_lightsabers/light4.jpg"
    );

    private static readonly string DokOndarsImgs = Imgs(
        "04_experiences/droid_depot/driod1.jpg",
        "04_experiences/droid_depot/driod2.jpg"
    );

    private static readonly string HyperspaceMtnImgs = Imgs(
        "02_rides/hyperspace_mountain_paris/hyper1.jpg",
        "02_rides/hyperspace_mountain_paris/hyper2.jpg",
        "02_rides/hyperspace_mountain_paris/hyper3.jpg",
        "02_rides/hyperspace_mountain_paris/hyper4.jpg",
        "02_rides/hyperspace_mountain_paris/hyper5.jpg"
    );

    private static readonly string RontoRoastersImgs = Imgs(
        "05_disneyland_ca/ronto_roasters/ronto1.webp",
        "05_disneyland_ca/ronto_roasters/ronto2.webp",
        "05_disneyland_ca/ronto_roasters/ronto3.jpg",
        "05_disneyland_ca/ronto_roasters/ronto4.png"
    );

    private static readonly string BlueMilkImgs = Imgs(
        "05_disneyland_ca/blue_milk_stand/milk1.webp",
        "05_disneyland_ca/blue_milk_stand/milk2.jpg",
        "05_disneyland_ca/blue_milk_stand/milk4.jpg"
    );

    private static readonly string LaunchBayImgs = Imgs(
        "01_overview/star-wars-launch-bay-meet-robot.jpg"
    );

    

    public static async Task SeedAsync(LocationsDbContext context)
    {
        if (await context.Parks.AnyAsync())
        {
            // Clear existing data to replace it as requested
            context.AttractionCategories.RemoveRange(context.AttractionCategories);
            context.Attractions.RemoveRange(context.Attractions);
            context.Categories.RemoveRange(context.Categories);
            context.Parks.RemoveRange(context.Parks);
            await context.SaveChangesAsync();
        }

        var parks = new List<Park>
        {
            new() {
                Id = "park_florida_usa",
                Name = "Jurassic Movies Park",
                Resort = "Universal Studios",
                City = "Florida",
                Country = "USA",
                Lat = 28.470782m,
                Lng = -81.473573m
            }
        };

        await context.Parks.AddRangeAsync(parks);
        await context.SaveChangesAsync();

        var movieCat = new Category { Id = Guid.NewGuid().ToString(), Name = "Movie", Description = "Jurassic Park Movies" };

        await context.Categories.AddRangeAsync(movieCat);
        await context.SaveChangesAsync();

        var lat = 28.470782m;
        var lng = -81.473573m;

        var attractions = new List<Attraction>
        {
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_florida_usa",
                Name = "Jurassic Park",
                Description = "During a preview tour, a theme park suffers a major power breakdown that allows cloned dinosaurs to run loose.",
                Lat = lat, Lng = lng,
                Tags = JsonSerializer.Serialize(new[] { "Classic", "Action", "Sci-Fi" }),
                ImageUrls = JsonSerializer.Serialize(new[] { "https://upload.wikimedia.org/wikipedia/en/e/e7/Jurassic_Park_poster.jpg" })
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_florida_usa",
                Name = "The Lost World: Jurassic Park",
                Description = "A research team travels to Isla Sorna where dinosaurs still live in the wild.",
                Lat = lat, Lng = lng,
                Tags = JsonSerializer.Serialize(new[] { "Adventure", "Sci-Fi" }),
                ImageUrls = JsonSerializer.Serialize(new[] { "https://upload.wikimedia.org/wikipedia/en/c/cc/The_Lost_World_%E2%80%93_Jurassic_Park_poster.jpg" })
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_florida_usa",
                Name = "Jurassic Park III",
                Description = "Dr. Alan Grant joins a mission to Isla Sorna and faces dangerous dinosaurs again.",
                Lat = lat, Lng = lng,
                Tags = JsonSerializer.Serialize(new[] { "Adventure", "Action" }),
                ImageUrls = JsonSerializer.Serialize(new[] { "https://upload.wikimedia.org/wikipedia/en/6/6d/Jurassic_Park_III_poster.jpg" })
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_florida_usa",
                Name = "Jurassic World",
                Description = "A new dinosaur theme park is open, but a genetically modified dinosaur escapes.",
                Lat = lat, Lng = lng,
                Tags = JsonSerializer.Serialize(new[] { "Modern", "Action", "Dinosaurs" }),
                ImageUrls = JsonSerializer.Serialize(new[] { "https://upload.wikimedia.org/wikipedia/en/6/6e/Jurassic_World_poster.jpg" })
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_florida_usa",
                Name = "Jurassic World: Fallen Kingdom",
                Description = "The team returns to save dinosaurs from a volcanic disaster and a new threat.",
                Lat = lat, Lng = lng,
                Tags = JsonSerializer.Serialize(new[] { "Survival", "Action" }),
                ImageUrls = JsonSerializer.Serialize(new[] { "https://upload.wikimedia.org/wikipedia/en/c/c6/Jurassic_World_Fallen_Kingdom.png" })
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_florida_usa",
                Name = "Jurassic World Dominion",
                Description = "Humans and dinosaurs must learn to live together in the modern world.",
                Lat = lat, Lng = lng,
                Tags = JsonSerializer.Serialize(new[] { "Epic", "Conclusion" }),
                ImageUrls = JsonSerializer.Serialize(new[] { "https://upload.wikimedia.org/wikipedia/en/c/ce/JurassicWorldDominion_Poster.jpeg" })
            }
        };

        await context.Attractions.AddRangeAsync(attractions);
        await context.SaveChangesAsync();

        var links = attractions.Select(a => new AttractionCategory
        {
            AttractionId = a.Id,
            CategoryId = movieCat.Id
        }).ToList();

        await context.AttractionCategories.AddRangeAsync(links);
        await context.SaveChangesAsync();
    }
}