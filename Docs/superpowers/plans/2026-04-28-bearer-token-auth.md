# Bearer Token Authentication Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Protect all WebApi (LocationsDB) endpoints with a machine-to-machine JWT service token issued by WebApplication, and enforce user JWT authentication on user-specific WebApplication actions.

**Architecture:** WebApplication generates short-lived JWTs (audience `infinity-api`) via a singleton `ServiceTokenProvider`, which a `DelegatingHandler` automatically attaches to every outgoing `HttpClient` request to WebApi. WebApi validates these tokens using a shared signing key supplied by the `JWT_SIGNING_KEY` environment variable. User-specific WebApplication endpoints (reviews, ratings, profile management) are gated with `[Authorize]`.

**Tech Stack:** ASP.NET Core 10, `Microsoft.AspNetCore.Authentication.JwtBearer` (already referenced in both projects), NUnit 4 (WebApplication.Tests), xUnit (WebApi.Tests)

---

## File Map

### New files
- `src/Infinity.WebApplication/Services/Auth/IServiceTokenProvider.cs`
- `src/Infinity.WebApplication/Services/Auth/ServiceTokenProvider.cs`
- `src/Infinity.WebApplication/Services/Auth/ServiceTokenHandler.cs`
- `src/Infinity.WebApplication.Tests/Services/Auth/ServiceTokenProviderTests.cs`
- `src/Infinity.WebApplication.Tests/Services/Auth/ServiceTokenHandlerTests.cs`
- `src/Infinity.WebApi.Tests/Controllers/AuthorizationAttributeTests.cs`

### Modified files
- `src/compose.yaml` — add `Jwt__Key` env var to both services
- `src/Infinity.WebApi/appsettings.json` — add `Jwt` config block
- `src/Infinity.WebApi/appsettings.Development.json` — add `Jwt` config block
- `src/Infinity.WebApi/Program.cs` — add JWT auth middleware
- `src/Infinity.WebApi/Controllers/AttractionsController.cs` — add `[Authorize]`
- `src/Infinity.WebApi/Controllers/ParksController.cs` — add `[Authorize]`
- `src/Infinity.WebApi/Controllers/ImagesController.cs` — add `[Authorize]`
- `src/Infinity.WebApplication/appsettings.json` — add `ServiceAudience` and `ServiceExpiryHours`
- `src/Infinity.WebApplication/Program.cs` — register `ServiceTokenProvider`, `ServiceTokenHandler`, update `HttpClient`

---

## Task 1: Wire JWT_SIGNING_KEY into Docker Compose

**Files:**
- Modify: `src/compose.yaml`

- [ ] **Step 1: Add `Jwt__Key` to both services**

  In `src/compose.yaml`, add `- Jwt__Key=${JWT_SIGNING_KEY}` to the `environment` block of both `infinity.webapi` and `infinity.webapp`. The double-underscore maps to `Jwt:Key` in ASP.NET Core configuration.

  ```yaml
  services:
    infinity.webapi:
      image: infinity.webapi
      build:
        context: .
        dockerfile: Infinity.WebApi/Dockerfile
      environment:
        - ConnectionStrings__LocationsConnection=Host=db;Database=infinity;Username=postgres;Password=postgres
        - ASPNETCORE_ENVIRONMENT=Development
        - ASPNETCORE_URLS=http://0.0.0.0:8080
        - Jwt__Key=${JWT_SIGNING_KEY}
      depends_on:
        db:
          condition: service_healthy
      ports:
        - "8080:8080"
        - "8081:8081"
      networks:
        - api-net

    db:
      image: postgres:latest
      environment:
        POSTGRES_DB: infinity
        POSTGRES_USER: postgres
        POSTGRES_PASSWORD: postgres
      ports:
        - "5432:5432"
      healthcheck:
        test: ["CMD-SHELL", "pg_isready -U postgres"]
        interval: 2s
        timeout: 3s
        retries: 30
        start_period: 5s
      networks:
        - api-net

    infinity.webapp:
      image: infinity.webapp
      build:
        context: .
        dockerfile: Infinity.WebApplication/Dockerfile
      environment:
        - ConnectionStrings__UserConnection=Host=userdb;Database=infinity_users;Username=postgres;Password=postgres
        - InfinityApi__BaseUrl=http://infinity.webapi:8080
        - Mapbox__AccessToken=${MAPBOX_ACCESS_TOKEN}
        - ASPNETCORE_ENVIRONMENT=Development
        - ASPNETCORE_URLS=http://0.0.0.0:8080
        - Jwt__Key=${JWT_SIGNING_KEY}
      depends_on:
        infinity.webapi:
          condition: service_started
        userdb:
          condition: service_healthy
      ports:
        - "8082:8080"
      networks:
        - api-net
        - webapp-net

    userdb:
      image: postgres:latest
      environment:
        POSTGRES_DB: infinity_users
        POSTGRES_USER: postgres
        POSTGRES_PASSWORD: postgres
      ports:
        - "5433:5432"
      healthcheck:
        test: ["CMD-SHELL", "pg_isready -U postgres"]
        interval: 2s
        timeout: 3s
        retries: 30
        start_period: 5s
      networks:
        - webapp-net

  networks:
    api-net:
    webapp-net:
  ```

- [ ] **Step 2: Commit**

  ```bash
  git add src/compose.yaml
  git commit -m "chore: wire JWT_SIGNING_KEY into compose environment"
  ```

---

## Task 2: Add Jwt Config to WebApi appsettings

**Files:**
- Modify: `src/Infinity.WebApi/appsettings.json`
- Modify: `src/Infinity.WebApi/appsettings.Development.json`

- [ ] **Step 1: Add Jwt block to `appsettings.json`**

  `Key` is left empty — it is supplied at runtime by `Jwt__Key` from the environment variable.

  ```json
  {
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "AllowedHosts": "*",
    "ConnectionStrings": {
      "LocationsConnection": "Host=localhost;Database=infinity_locations;Username=postgres;Password=postgres"
    },
    "Jwt": {
      "Key": "",
      "Issuer": "infinity-web",
      "Audience": "infinity-api"
    },
    "Images": {
      "StoragePath": "images/attractions",
      "AllowedExtensions": [ ".jpg", ".jpeg", ".png", ".webp", ".gif" ],
      "MaxFileSizeBytes": 10485760
    }
  }
  ```

- [ ] **Step 2: Add Jwt block to `appsettings.Development.json`**

  ```json
  {
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "Jwt": {
      "Key": "",
      "Issuer": "infinity-web",
      "Audience": "infinity-api"
    }
  }
  ```

- [ ] **Step 3: Commit**

  ```bash
  git add src/Infinity.WebApi/appsettings.json src/Infinity.WebApi/appsettings.Development.json
  git commit -m "chore: add Jwt config block to WebApi appsettings"
  ```

---

## Task 3: Add ServiceAudience Config to WebApplication appsettings

**Files:**
- Modify: `src/Infinity.WebApplication/appsettings.json`

- [ ] **Step 1: Add `ServiceAudience` and `ServiceExpiryHours` to the existing `Jwt` block**

  ```json
  {
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "Mapbox": {
      "AccessToken": ""
    },
    "AllowedHosts": "*",
    "ConnectionStrings": {
      "UserConnection": "Host=userdb;Database=infinity_users;Username=postgres;Password=postgres"
    },
    "Jwt": {
      "Key": "",
      "Issuer": "infinity-web",
      "Audience": "infinity-client",
      "ExpiryHours": 24,
      "ServiceAudience": "infinity-api",
      "ServiceExpiryHours": 1
    },
    "InfinityApi": {
      "BaseUrl": "http://localhost:5031"
    }
  }
  ```

- [ ] **Step 2: Commit**

  ```bash
  git add src/Infinity.WebApplication/appsettings.json
  git commit -m "chore: add ServiceAudience and ServiceExpiryHours to WebApplication Jwt config"
  ```

---

## Task 4: Add JWT Auth Middleware to WebApi

**Files:**
- Modify: `src/Infinity.WebApi/Program.cs`

- [ ] **Step 1: Add auth middleware to `Program.cs`**

  Add `AddAuthentication` / `AddJwtBearer` in the services section and `UseAuthentication` / `UseAuthorization` in the pipeline, before `MapControllers`. The full file after changes:

  ```csharp
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
  builder.Services.AddScoped<IStringService, StringService>();

  builder.Services.Configure<ImageOptions>(
      builder.Configuration.GetSection(ImageOptions.SectionName));
  builder.Services.AddSingleton<IImageService, ImageService>();

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
  ```

- [ ] **Step 2: Commit**

  ```bash
  git add src/Infinity.WebApi/Program.cs
  git commit -m "feat: add JWT bearer auth middleware to WebApi"
  ```

---

## Task 5: Add [Authorize] to WebApi Controllers

**Files:**
- Test: `src/Infinity.WebApi.Tests/Controllers/AuthorizationAttributeTests.cs`
- Modify: `src/Infinity.WebApi/Controllers/AttractionsController.cs`
- Modify: `src/Infinity.WebApi/Controllers/ParksController.cs`
- Modify: `src/Infinity.WebApi/Controllers/ImagesController.cs`

- [ ] **Step 1: Write the failing tests**

  Create `src/Infinity.WebApi.Tests/Controllers/AuthorizationAttributeTests.cs`:

  ```csharp
  using System.Reflection;
  using Infinity.WebApi.Controllers;
  using Microsoft.AspNetCore.Authorization;

  namespace Infinity.WebApi.Tests.Controllers;

  public class AuthorizationAttributeTests
  {
      [Theory]
      [InlineData(typeof(AttractionsController))]
      [InlineData(typeof(ParksController))]
      [InlineData(typeof(ImagesController))]
      public void Controller_HasAuthorizeAttribute(Type controllerType)
      {
          var attr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
          Assert.NotNull(attr);
      }
  }
  ```

- [ ] **Step 2: Run tests to verify they fail**

  ```bash
  dotnet test src/Infinity.WebApi.Tests --filter "AuthorizationAttributeTests"
  ```

  Expected: 3 failures — `attr` is null on all three controllers.

- [ ] **Step 3: Add `[Authorize]` to `AttractionsController`**

  Add `using Microsoft.AspNetCore.Authorization;` and `[Authorize]` at the class level:

  ```csharp
  using Infinity.WebApi.Data;
  using Infinity.WebApi.Models;
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.AspNetCore.Mvc;
  using Microsoft.EntityFrameworkCore;

  namespace Infinity.WebApi.Controllers;

  [ApiController]
  [Authorize]
  [Route("api/[controller]")]
  public class AttractionsController : ControllerBase, IAttractionsController
  {
      private readonly LocationsDbContext _context;

      public AttractionsController(LocationsDbContext context)
      {
          _context = context;
      }

      [HttpGet("")]
      public async Task<ActionResult<IEnumerable<Attraction>>> GetAttractions()
      {
          return await _context.Attractions.ToListAsync();
      }

      [HttpGet("{id}")]
      public async Task<ActionResult<Attraction>> GetAttraction(string id)
      {
          var attraction = await _context.Attractions.FindAsync(id);

          if (attraction == null)
          {
              return NotFound();
          }

          return attraction;
      }
  }
  ```

- [ ] **Step 4: Add `[Authorize]` to `ParksController`**

  ```csharp
  using Infinity.WebApi.Data;
  using Infinity.WebApi.Models;
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.AspNetCore.Mvc;
  using Microsoft.EntityFrameworkCore;

  namespace Infinity.WebApi.Controllers;

  [ApiController]
  [Authorize]
  [Route("api/[controller]")]
  public class ParksController : ControllerBase, IParksController
  {
      private readonly LocationsDbContext _context;

      public ParksController(LocationsDbContext context)
      {
          _context = context;
      }

      [HttpGet("")]
      public async Task<ActionResult<IEnumerable<Park>>> GetParks()
      {
          return await _context.Parks.ToListAsync();
      }

      [HttpGet("{id}")]
      public async Task<ActionResult<Park>> GetPark(string id)
      {
          var park = await _context.Parks.FindAsync(id);

          if (park == null)
          {
              return NotFound();
          }

          return park;
      }
  }
  ```

- [ ] **Step 5: Add `[Authorize]` to `ImagesController`**

  Add `using Microsoft.AspNetCore.Authorization;` and `[Authorize]` at the class level in `src/Infinity.WebApi/Controllers/ImagesController.cs`. Only the class declaration changes — all action methods remain unchanged:

  ```csharp
  [ApiController]
  [Authorize]
  [Route("api/attractions")]
  public class ImagesController : ControllerBase
  ```

- [ ] **Step 6: Run tests to verify they pass**

  ```bash
  dotnet test src/Infinity.WebApi.Tests --filter "AuthorizationAttributeTests"
  ```

  Expected: 3 passing.

- [ ] **Step 7: Run all WebApi tests to check for regressions**

  ```bash
  dotnet test src/Infinity.WebApi.Tests
  ```

  Expected: all passing. The existing controller tests bypass the auth pipeline so they are unaffected by `[Authorize]`.

- [ ] **Step 8: Commit**

  ```bash
  git add src/Infinity.WebApi.Tests/Controllers/AuthorizationAttributeTests.cs \
          src/Infinity.WebApi/Controllers/AttractionsController.cs \
          src/Infinity.WebApi/Controllers/ParksController.cs \
          src/Infinity.WebApi/Controllers/ImagesController.cs
  git commit -m "feat: require authorization on all WebApi controllers"
  ```

---

## Task 6: Implement IServiceTokenProvider

**Files:**
- Create: `src/Infinity.WebApplication/Services/Auth/IServiceTokenProvider.cs`
- Create: `src/Infinity.WebApplication/Services/Auth/ServiceTokenProvider.cs`
- Test: `src/Infinity.WebApplication.Tests/Services/Auth/ServiceTokenProviderTests.cs`

- [ ] **Step 1: Write the failing tests**

  Create `src/Infinity.WebApplication.Tests/Services/Auth/ServiceTokenProviderTests.cs`:

  ```csharp
  using System.IdentityModel.Tokens.Jwt;
  using System.Security.Claims;
  using Infinity.WebApplication.Services.Auth;
  using Microsoft.Extensions.Configuration;

  namespace Infinity.WebApplication.Tests.Services.Auth;

  public class ServiceTokenProviderTests
  {
      private const string TestKey = "test-signing-key-that-is-long-enough-32chars!";
      private const string TestIssuer = "infinity-web";
      private const string TestAudience = "infinity-api";

      private static IConfiguration BuildConfig(double expiryHours = 1) =>
          new ConfigurationBuilder()
              .AddInMemoryCollection(new Dictionary<string, string?>
              {
                  ["Jwt:Key"] = TestKey,
                  ["Jwt:Issuer"] = TestIssuer,
                  ["Jwt:ServiceAudience"] = TestAudience,
                  ["Jwt:ServiceExpiryHours"] = expiryHours.ToString()
              })
              .Build();

      [Test]
      public void GetToken_ReturnsReadableJwt()
      {
          var provider = new ServiceTokenProvider(BuildConfig());

          var token = provider.GetToken();

          Assert.That(new JwtSecurityTokenHandler().CanReadToken(token), Is.True);
      }

      [Test]
      public void GetToken_TokenHasServiceRoleClaim()
      {
          var provider = new ServiceTokenProvider(BuildConfig());

          var token = provider.GetToken();
          var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

          Assert.That(parsed.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Service"), Is.True);
      }

      [Test]
      public void GetToken_TokenHasCorrectAudience()
      {
          var provider = new ServiceTokenProvider(BuildConfig());

          var token = provider.GetToken();
          var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

          Assert.That(parsed.Audiences, Contains.Item(TestAudience));
      }

      [Test]
      public void GetToken_ReturnsCachedTokenOnSubsequentCalls()
      {
          var provider = new ServiceTokenProvider(BuildConfig());

          var first = provider.GetToken();
          var second = provider.GetToken();

          Assert.That(second, Is.EqualTo(first));
      }
  }
  ```

- [ ] **Step 2: Run tests to verify they fail**

  ```bash
  dotnet test src/Infinity.WebApplication.Tests --filter "ServiceTokenProviderTests"
  ```

  Expected: compilation failure — `ServiceTokenProvider` does not exist yet.

- [ ] **Step 3: Create the interface**

  Create `src/Infinity.WebApplication/Services/Auth/IServiceTokenProvider.cs`:

  ```csharp
  namespace Infinity.WebApplication.Services.Auth;

  public interface IServiceTokenProvider
  {
      string GetToken();
  }
  ```

- [ ] **Step 4: Create the implementation**

  Create `src/Infinity.WebApplication/Services/Auth/ServiceTokenProvider.cs`:

  ```csharp
  using System.IdentityModel.Tokens.Jwt;
  using System.Security.Claims;
  using System.Text;
  using Microsoft.IdentityModel.Tokens;

  namespace Infinity.WebApplication.Services.Auth;

  public sealed class ServiceTokenProvider : IServiceTokenProvider
  {
      private readonly IConfiguration _config;
      private string? _cachedToken;
      private DateTime _tokenExpiry = DateTime.MinValue;
      private readonly Lock _lock = new();

      public ServiceTokenProvider(IConfiguration config)
      {
          _config = config;
      }

      public string GetToken()
      {
          lock (_lock)
          {
              if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry - TimeSpan.FromSeconds(60))
                  return _cachedToken;

              var expiryHours = double.Parse(_config["Jwt:ServiceExpiryHours"]!);
              _tokenExpiry = DateTime.UtcNow.AddHours(expiryHours);
              _cachedToken = GenerateToken(_tokenExpiry);
              return _cachedToken;
          }
      }

      private string GenerateToken(DateTime expires)
      {
          var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
          var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

          var token = new JwtSecurityToken(
              issuer: _config["Jwt:Issuer"],
              audience: _config["Jwt:ServiceAudience"],
              claims: [new Claim(ClaimTypes.Role, "Service")],
              expires: expires,
              signingCredentials: creds
          );

          return new JwtSecurityTokenHandler().WriteToken(token);
      }
  }
  ```

- [ ] **Step 5: Run tests to verify they pass**

  ```bash
  dotnet test src/Infinity.WebApplication.Tests --filter "ServiceTokenProviderTests"
  ```

  Expected: 4 passing.

- [ ] **Step 6: Commit**

  ```bash
  git add src/Infinity.WebApplication/Services/Auth/IServiceTokenProvider.cs \
          src/Infinity.WebApplication/Services/Auth/ServiceTokenProvider.cs \
          src/Infinity.WebApplication.Tests/Services/Auth/ServiceTokenProviderTests.cs
  git commit -m "feat: add IServiceTokenProvider with JWT caching"
  ```

---

## Task 7: Implement ServiceTokenHandler

**Files:**
- Create: `src/Infinity.WebApplication/Services/Auth/ServiceTokenHandler.cs`
- Test: `src/Infinity.WebApplication.Tests/Services/Auth/ServiceTokenHandlerTests.cs`

- [ ] **Step 1: Write the failing test**

  Create `src/Infinity.WebApplication.Tests/Services/Auth/ServiceTokenHandlerTests.cs`:

  ```csharp
  using System.Net;
  using Infinity.WebApplication.Services.Auth;

  namespace Infinity.WebApplication.Tests.Services.Auth;

  public class ServiceTokenHandlerTests
  {
      private sealed class StubTokenProvider : IServiceTokenProvider
      {
          public string GetToken() => "test-token-value";
      }

      private sealed class CapturingHandler : HttpMessageHandler
      {
          public HttpRequestMessage? LastRequest { get; private set; }

          protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
          {
              LastRequest = request;
              return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
          }
      }

      [Test]
      public async Task SendAsync_AttachesBearerTokenToRequest()
      {
          var capturing = new CapturingHandler();
          var handler = new ServiceTokenHandler(new StubTokenProvider())
          {
              InnerHandler = capturing
          };
          var client = new HttpClient(handler);

          await client.GetAsync("http://localhost/api/parks");

          var auth = capturing.LastRequest!.Headers.Authorization;
          Assert.That(auth!.Scheme, Is.EqualTo("Bearer"));
          Assert.That(auth.Parameter, Is.EqualTo("test-token-value"));
      }
  }
  ```

- [ ] **Step 2: Run the test to verify it fails**

  ```bash
  dotnet test src/Infinity.WebApplication.Tests --filter "ServiceTokenHandlerTests"
  ```

  Expected: compilation failure — `ServiceTokenHandler` does not exist yet.

- [ ] **Step 3: Create the implementation**

  Create `src/Infinity.WebApplication/Services/Auth/ServiceTokenHandler.cs`:

  ```csharp
  using System.Net.Http.Headers;

  namespace Infinity.WebApplication.Services.Auth;

  public sealed class ServiceTokenHandler : DelegatingHandler
  {
      private readonly IServiceTokenProvider _tokenProvider;

      public ServiceTokenHandler(IServiceTokenProvider tokenProvider)
      {
          _tokenProvider = tokenProvider;
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
      {
          request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenProvider.GetToken());
          return base.SendAsync(request, ct);
      }
  }
  ```

- [ ] **Step 4: Run the test to verify it passes**

  ```bash
  dotnet test src/Infinity.WebApplication.Tests --filter "ServiceTokenHandlerTests"
  ```

  Expected: 1 passing.

- [ ] **Step 5: Run all WebApplication tests to check for regressions**

  ```bash
  dotnet test src/Infinity.WebApplication.Tests
  ```

  Expected: all passing.

- [ ] **Step 6: Commit**

  ```bash
  git add src/Infinity.WebApplication/Services/Auth/ServiceTokenHandler.cs \
          src/Infinity.WebApplication.Tests/Services/Auth/ServiceTokenHandlerTests.cs
  git commit -m "feat: add ServiceTokenHandler to attach service JWT to outgoing requests"
  ```

---

## Task 8: Wire ServiceTokenProvider into WebApplication

**Files:**
- Modify: `src/Infinity.WebApplication/Program.cs`

- [ ] **Step 1: Register services and update HttpClient in `Program.cs`**

  Add the two `using` statements and register `ServiceTokenProvider` as a singleton, `ServiceTokenHandler` as transient, and chain the handler onto the existing `HttpClient`. The full updated file:

  ```csharp
  using Infinity.WebApplication.Data;
  using Infinity.WebApplication.Services.Auth;
  using Infinity.WebApplication.Services.Home;
  using Infinity.WebApplication.Services.UserService;
  using Microsoft.EntityFrameworkCore;
  using System.Text;
  using Microsoft.AspNetCore.Authentication.JwtBearer;
  using Microsoft.IdentityModel.Tokens;

  var builder = WebApplication.CreateBuilder(args);

  builder.Services.AddControllersWithViews();

  builder.Services.AddSingleton<IServiceTokenProvider, ServiceTokenProvider>();
  builder.Services.AddTransient<ServiceTokenHandler>();

  builder.Services.AddHttpClient<IIndexContentService, IndexContentService>(client =>
  {
      client.BaseAddress = new Uri(builder.Configuration["InfinityApi:BaseUrl"]
          ?? throw new InvalidOperationException("InfinityApi:BaseUrl is not configured."));
  })
  .AddHttpMessageHandler<ServiceTokenHandler>();

  builder.Services.AddDbContext<UserDbContext>(options =>
      options.UseNpgsql(builder.Configuration.GetConnectionString("UserConnection")
          ?? throw new InvalidOperationException("UserConnection is not configured.")));

  builder.Services.AddScoped<IUserService, UserService>();

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

  var app = builder.Build();

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
  app.UseAuthentication();
  app.UseAuthorization();
  app.MapStaticAssets();

  app.MapControllerRoute(
      name: "default",
      pattern: "{controller=Home}/{action=Index}/{id?}")
      .WithStaticAssets();

  app.Run();
  ```

- [ ] **Step 2: Build to verify no compile errors**

  ```bash
  dotnet build src/Infinity.WebApplication/Infinity.WebApplication.csproj
  ```

  Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Run all tests**

  ```bash
  dotnet test src/Infinity.WebApplication.Tests && dotnet test src/Infinity.WebApi.Tests
  ```

  Expected: all passing.

- [ ] **Step 4: Commit**

  ```bash
  git add src/Infinity.WebApplication/Program.cs
  git commit -m "feat: register ServiceTokenProvider and attach to WebApi HttpClient"
  ```

---

## Note: User-Side [Authorize] (Future Sprint)

`ReviewsController`, `RatingsController`, and `AccountController` do not exist in the codebase yet — they are planned for a future sprint. When those controllers are built, they must include `[Authorize]` at the class level and scope DB queries to `ClaimTypes.NameIdentifier` from the user's token. The JWT bearer middleware in `Program.cs` is already configured to validate user tokens (audience `infinity-client`), so no middleware changes will be needed at that point.
