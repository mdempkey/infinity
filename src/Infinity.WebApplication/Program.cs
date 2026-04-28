using Infinity.WebApplication.Data;
using Infinity.WebApplication.Services.Home;
using Infinity.WebApplication.Services.RatingService;
using Infinity.WebApplication.Services.ReviewService;
using Infinity.WebApplication.Services.UserService;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<IIndexContentService, IndexContentService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["InfinityApi:BaseUrl"]
        ?? throw new InvalidOperationException("InfinityApi:BaseUrl is not configured."));
});

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("UserConnection")
        ?? throw new InvalidOperationException("UserConnection is not configured.")));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

var app = builder.Build();

// Apply user DB migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
