using Microsoft.EntityFrameworkCore;
using Infinity.WebApi.Data;
using Infinity.WebApi.Services;
using Infinity.WebApi.Settings;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddScoped<IStringService, StringService>();

// Existing services
builder.Services.AddScoped<IStringService, StringService>();

// Image file system
builder.Services.Configure<ImageOptions>(
    builder.Configuration.GetSection(ImageOptions.SectionName));
builder.Services.AddSingleton<IImageService, ImageService>();

// Locations database
builder.Services.AddDbContext<LocationsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LocationsConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
