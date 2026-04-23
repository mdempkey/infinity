using Infinity.WebApplication.Services.Home;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<IIndexContentService, IndexContentService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["InfinityApi:BaseUrl"]
        ?? throw new InvalidOperationException("InfinityApi:BaseUrl is not configured."));
});

var app = builder.Build();

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
