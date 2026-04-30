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
        if (await context.Parks.AnyAsync()) return;

        var parks = new List<Park>
        {
            new() {
                Id = "park_dle_gge",
                Name = "Star Wars: Galaxy's Edge",
                Resort = "Disneyland Resort",
                City = "Anaheim",
                Country = "USA",
                Lat = 33.8154m,
                Lng = -117.9216m
            },
            new() {
                Id = "park_wdw_gge",
                Name = "Star Wars: Galaxy's Edge",
                Resort = "Walt Disney World",
                City = "Orlando",
                Country = "USA",
                Lat = 28.3553m,
                Lng = -81.5632m
            },
            new() {
                Id = "park_dlp_gge",
                Name = "Star Wars: Galaxy's Edge",
                Resort = "Disneyland Paris",
                City = "Chessy",
                Country = "France",
                Lat = 48.8674m,
                Lng = 2.7836m
            },
            new() {
                Id = "park_wdw_lb",
                Name = "Star Wars Launch Bay",
                Resort = "Walt Disney World",
                City = "Orlando",
                Country = "USA",
                Lat = 28.3576m,
                Lng = -81.5607m
            }
        };

        await context.Parks.AddRangeAsync(parks);
        await context.SaveChangesAsync();

        var ride       = new Category { Id = Guid.NewGuid().ToString(), Name = "Ride",       Description = "Rides and immersive experiences" };
        var restaurant = new Category { Id = Guid.NewGuid().ToString(), Name = "Restaurant", Description = "Restaurants, cafes, and food stands" };
        var shop       = new Category { Id = Guid.NewGuid().ToString(), Name = "Shop",       Description = "Merchandise and retail experiences" };
        var show       = new Category { Id = Guid.NewGuid().ToString(), Name = "Show",       Description = "Shows, meet-and-greets, and live entertainment" };

        await context.Categories.AddRangeAsync(ride, restaurant, shop, show);
        await context.SaveChangesAsync();

        var attractions = new List<Attraction>
        {
            // ── Disneyland GGE ───────────────────────────────────────────────
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_dle_gge",
                Name = "Millennium Falcon: Smugglers Run",
                Description = "Take the controls of the most famous ship in the galaxy on a custom mission.",
                Lat = 33.8152m, Lng = -117.9219m,
                Tags = JsonSerializer.Serialize(new[] { "interactive", "simulator", "classic" }),
                ImageUrls = SmugglersRunImgs
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_dle_gge",
                Name = "Star Wars: Rise of the Resistance",
                Description = "Join the Resistance and find yourself captured by the First Order in this epic adventure.",
                Lat = 33.8158m, Lng = -117.9224m,
                Tags = JsonSerializer.Serialize(new[] { "immersive", "trackless", "must-do" }),
                ImageUrls = RiseImgs
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_dle_gge",
                Name = "Docking Bay 7 Food and Cargo",
                Description = "Galactic cuisine served out of a working hangar bay in Black Spire Outpost.",
                Lat = 33.8155m, Lng = -117.9221m,
                Tags = JsonSerializer.Serialize(new[] { "quick-service", "themed", "outdoor-seating" }),
                ImageUrls = DockingBay7Imgs
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_dle_gge",
                Name = "Oga's Cantina",
                Description = "The shadiest watering hole in Black Spire Outpost — for those who prefer to stay off the radar.",
                Lat = 33.8153m, Lng = -117.9218m,
                Tags = JsonSerializer.Serialize(new[] { "bar", "reservations-recommended", "21+" }),
                ImageUrls = OgasCantinaImgs
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_dle_gge",
                Name = "Savi's Workshop",
                Description = "Build a custom lightsaber in this hands-on experience guided by the Gatherers.",
                Lat = 33.8150m, Lng = -117.9215m,
                Tags = JsonSerializer.Serialize(new[] { "interactive", "reservations-required", "upcharge" }),
                ImageUrls = SavisWorkshopImgs
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_dle_gge",
                Name = "Dok-Ondar's Den of Antiquities",
                Description = "Browse rare and exotic relics from across the galaxy in this collector's shop.",
                Lat = 33.8149m, Lng = -117.9220m,
                Tags = JsonSerializer.Serialize(new[] { "merchandise", "collectibles", "legacy-sabers" }),
                ImageUrls = DokOndarsImgs
            },

            // ── Walt Disney World GGE ────────────────────────────────────────
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_wdw_gge",
                Name = "Millennium Falcon: Smugglers Run",
                Description = "Take the controls of the most famous ship in the galaxy on a custom mission.",
                Lat = 28.3551m, Lng = -81.5634m,
                Tags = JsonSerializer.Serialize(new[] { "interactive", "simulator", "classic" }),
                ImageUrls = SmugglersRunImgs
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_wdw_gge",
                Name = "Star Wars: Rise of the Resistance",
                Description = "Join the Resistance and find yourself captured by the First Order in this epic adventure.",
                Lat = 28.3556m, Lng = -81.5630m,
                Tags = JsonSerializer.Serialize(new[] { "immersive", "trackless", "must-do" }),
                ImageUrls = RiseImgs
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_wdw_gge",
                Name = "Docking Bay 7 Food and Cargo",
                Description = "Galactic cuisine served out of a working hangar bay in Black Spire Outpost.",
                Lat = 28.3553m, Lng = -81.5633m,
                Tags = JsonSerializer.Serialize(new[] { "quick-service", "themed", "outdoor-seating" }),
                ImageUrls = DockingBay7Imgs
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_wdw_gge",
                Name = "Oga's Cantina",
                Description = "The shadiest watering hole in Black Spire Outpost.",
                Lat = 28.3552m, Lng = -81.5631m,
                Tags = JsonSerializer.Serialize(new[] { "bar", "reservations-recommended" }),
                ImageUrls = OgasCantinaImgs
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_wdw_gge",
                Name = "Savi's Workshop",
                Description = "Build a custom lightsaber guided by the Gatherers.",
                Lat = 28.3550m, Lng = -81.5635m,
                Tags = JsonSerializer.Serialize(new[] { "interactive", "reservations-required", "upcharge" }),
                ImageUrls = SavisWorkshopImgs
            },

            // ── Disneyland Paris GGE ─────────────────────────────────────────
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_dlp_gge",
                Name = "Star Wars Hyperspace Mountain",
                Description = "Blast into hyperspace on this reimagined classic coaster.",
                Lat = 48.8673m, Lng = 2.7835m,
                Tags = JsonSerializer.Serialize(new[] { "coaster", "high-thrill", "classic" }),
                ImageUrls = HyperspaceMtnImgs
            },
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_dlp_gge",
                Name = "Star Wars: Rise of the Resistance",
                Description = "The Resistance needs you — escape the First Order in this massive immersive experience.",
                Lat = 48.8675m, Lng = 2.7838m,
                Tags = JsonSerializer.Serialize(new[] { "immersive", "trackless", "must-do" }),
                ImageUrls = RiseImgs
            },

            // ── Launch Bay ───────────────────────────────────────────────────
            new() {
                Id = Guid.NewGuid().ToString(),
                ParkId = "park_wdw_lb",
                Name = "Star Wars Launch Bay",
                Description = "Celebrate Star Wars with themed exhibits, character meet-and-greets, and merchandise.",
                Lat = 28.3576m, Lng = -81.5607m,
                Tags = JsonSerializer.Serialize(new[] { "meet-and-greet", "exhibit", "merchandise" }),
                ImageUrls = LaunchBayImgs
            }
        };

        await context.Attractions.AddRangeAsync(attractions);
        await context.SaveChangesAsync();

        var attractionsByKey = await context.Attractions.ToDictionaryAsync(a => (a.ParkId, a.Name));
        var catByName = await context.Categories.ToDictionaryAsync(c => c.Name);

        var links = new List<AttractionCategory>
        {
            new() { AttractionId = attractionsByKey[("park_dle_gge", "Millennium Falcon: Smugglers Run")].Id,  CategoryId = catByName["Ride"].Id },
            new() { AttractionId = attractionsByKey[("park_dle_gge", "Star Wars: Rise of the Resistance")].Id, CategoryId = catByName["Ride"].Id },
            new() { AttractionId = attractionsByKey[("park_dle_gge", "Docking Bay 7 Food and Cargo")].Id,      CategoryId = catByName["Restaurant"].Id },
            new() { AttractionId = attractionsByKey[("park_dle_gge", "Oga's Cantina")].Id,                    CategoryId = catByName["Restaurant"].Id },
            new() { AttractionId = attractionsByKey[("park_dle_gge", "Savi's Workshop")].Id,                  CategoryId = catByName["Shop"].Id },
            new() { AttractionId = attractionsByKey[("park_dle_gge", "Dok-Ondar's Den of Antiquities")].Id,   CategoryId = catByName["Shop"].Id },
            new() { AttractionId = attractionsByKey[("park_wdw_gge", "Millennium Falcon: Smugglers Run")].Id,  CategoryId = catByName["Ride"].Id },
            new() { AttractionId = attractionsByKey[("park_wdw_gge", "Star Wars: Rise of the Resistance")].Id, CategoryId = catByName["Ride"].Id },
            new() { AttractionId = attractionsByKey[("park_wdw_gge", "Docking Bay 7 Food and Cargo")].Id,      CategoryId = catByName["Restaurant"].Id },
            new() { AttractionId = attractionsByKey[("park_wdw_gge", "Oga's Cantina")].Id,                    CategoryId = catByName["Restaurant"].Id },
            new() { AttractionId = attractionsByKey[("park_wdw_gge", "Savi's Workshop")].Id,                  CategoryId = catByName["Shop"].Id },
            new() { AttractionId = attractionsByKey[("park_dlp_gge", "Star Wars Hyperspace Mountain")].Id,    CategoryId = catByName["Ride"].Id },
            new() { AttractionId = attractionsByKey[("park_dlp_gge", "Star Wars: Rise of the Resistance")].Id, CategoryId = catByName["Ride"].Id },
            new() { AttractionId = attractionsByKey[("park_wdw_lb", "Star Wars Launch Bay")].Id,              CategoryId = catByName["Show"].Id },
        };

        await context.AttractionCategories.AddRangeAsync(links);
        await context.SaveChangesAsync();
    }
}