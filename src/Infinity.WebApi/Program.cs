using Microsoft.EntityFrameworkCore;
using Infinity.WebApi.Data;
using Infinity.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddScoped<IStringService, StringService>();

builder.Services.AddDbContext<LocationsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LocationsConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var locationsDb = scope.ServiceProvider.GetRequiredService<LocationsDbContext>();
    await locationsDb.Database.MigrateAsync();
    await LocationsDbSeeder.SeedAsync(locationsDb);

    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Infinity API");
    });
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();