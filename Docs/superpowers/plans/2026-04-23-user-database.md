# User Database Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a containerized PostgreSQL user database accessible only by the WebApp, with EF Core data access and a `UserService` for registration, login, and lookup.

**Architecture:** A dedicated `userdb` Postgres container lives on `webapp-net`, a private Docker network shared only with the `infinity.webapp` container. The WebApp owns a `UserDbContext` and a `UserService` — controllers never touch the DbContext directly. The WebAPI is completely unaware of the user database.

**Tech Stack:** ASP.NET Core 10 MVC, Entity Framework Core 10 (Npgsql), BCrypt.Net-Next, Docker Compose named networks, NUnit (test project).

---

## File Map

**Create:**
- `src/Infinity.WebApplication/Models/User.cs` — user entity
- `src/Infinity.WebApplication/Data/UserDbContext.cs` — EF DbContext for user database
- `src/Infinity.WebApplication/Services/UserService/IUserService.cs` — service interface
- `src/Infinity.WebApplication/Services/UserService/UserService.cs` — service implementation
- `src/Infinity.WebApplication.Tests/Services/UserServiceTests.cs` — NUnit tests

**Modify:**
- `src/Infinity.WebApplication/Infinity.WebApplication.csproj` — add Npgsql EF, EF Design, BCrypt packages
- `src/Infinity.WebApplication/appsettings.json` — add `UserConnection` connection string
- `src/Infinity.WebApplication/appsettings.Development.json` — add `UserConnection` for localhost
- `src/Infinity.WebApplication/Program.cs` — register `UserDbContext`, `UserService`, migrate on startup
- `src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj` — add project ref, EF InMemory, BCrypt packages
- `src/compose.yaml` — add `userdb` service, `infinity.webapp` service, named networks; update existing services

**Delete:**
- `src/Infinity.WebApi/Models/User.cs` — stub no longer needed
- `src/Infinity.WebApi/Data/AppDbContext.cs` — stub DbContext no longer needed
- `src/Infinity.WebApi/Program.cs` — remove `AppDbContext` registration (keep `LocationsDbContext`)
- `src/Infinity.WebApi/appsettings.json` — remove `DefaultConnection` string

---

## Task 1: Delete the WebAPI User Stub

**Files:**
- Delete: `src/Infinity.WebApi/Models/User.cs`
- Delete: `src/Infinity.WebApi/Data/AppDbContext.cs`
- Modify: `src/Infinity.WebApi/Program.cs`
- Modify: `src/Infinity.WebApi/appsettings.json`

- [ ] **Step 1: Delete the stub model and DbContext**

```bash
rm src/Infinity.WebApi/Models/User.cs
rm src/Infinity.WebApi/Data/AppDbContext.cs
```

- [ ] **Step 2: Remove AppDbContext from Program.cs**

Open `src/Infinity.WebApi/Program.cs`. Remove the `AppDbContext` registration so the file reads:

```csharp
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
```

- [ ] **Step 3: Remove DefaultConnection from WebAPI appsettings**

Open `src/Infinity.WebApi/appsettings.json`. Remove `DefaultConnection` so only `LocationsConnection` remains:

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
    "LocationsConnection": "Host=localhost;Database=infinity;Username=postgres;Password=postgres"
  }
}
```

- [ ] **Step 4: Verify the WebAPI still builds**

```bash
dotnet build src/Infinity.WebApi/Infinity.WebApi.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: remove WebAPI User stub and AppDbContext"
```

---

## Task 2: Configure the Test Project

**Files:**
- Modify: `src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj`

- [ ] **Step 1: Add packages and project reference**

Open `src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj` and replace the contents with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
        <PackageReference Include="coverlet.collector" Version="6.0.4" />
        <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.0" />
        <PackageReference Include="NUnit" Version="4.3.2" />
        <PackageReference Include="NUnit.Analyzers" Version="4.7.0" />
        <PackageReference Include="NUnit3TestAdapter" Version="5.0.0" />
    </ItemGroup>

    <ItemGroup>
        <Using Include="NUnit.Framework" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Infinity.WebApplication\Infinity.WebApplication.csproj" />
    </ItemGroup>

</Project>
```

- [ ] **Step 2: Restore packages**

```bash
dotnet restore src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj
```

Expected: Restore completed, no errors.

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj
git commit -m "chore: configure WebApplication.Tests project with NUnit and EF InMemory"
```

---

## Task 3: Add NuGet Packages to WebApp

**Files:**
- Modify: `src/Infinity.WebApplication/Infinity.WebApplication.csproj`

- [ ] **Step 1: Add packages**

```bash
cd src/Infinity.WebApplication
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package BCrypt.Net-Next
cd ../..
```

- [ ] **Step 2: Verify build**

```bash
dotnet build src/Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Infinity.WebApplication.csproj
git commit -m "chore: add Npgsql EF, EF Design, and BCrypt packages to WebApp"
```

---

## Task 4: User Model and UserDbContext (TDD)

**Files:**
- Create: `src/Infinity.WebApplication.Tests/Services/UserDbContextTests.cs`
- Create: `src/Infinity.WebApplication/Models/User.cs`
- Create: `src/Infinity.WebApplication/Data/UserDbContext.cs`

- [ ] **Step 1: Write the failing test**

Create `src/Infinity.WebApplication.Tests/Services/UserDbContextTests.cs`:

```csharp
using Infinity.WebApplication.Data;
using Infinity.WebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Tests.Services;

public class UserDbContextTests
{
    private static UserDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Test]
    public async Task CanSaveAndRetrieveUser()
    {
        await using var db = CreateContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "luke",
            Email = "luke@rebellion.org",
            PasswordHash = "hashed",
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var found = await db.Users.FindAsync(user.Id);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Username, Is.EqualTo("luke"));
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "UserDbContextTests"
```

Expected: Build error — `Infinity.WebApplication.Data` and `Infinity.WebApplication.Models` do not exist yet.

- [ ] **Step 3: Create the User model**

Create `src/Infinity.WebApplication/Models/User.cs`:

```csharp
namespace Infinity.WebApplication.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

- [ ] **Step 4: Create UserDbContext**

Create `src/Infinity.WebApplication/Data/UserDbContext.cs`:

```csharp
using Infinity.WebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(u => u.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        });
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "UserDbContextTests"
```

Expected: 1 test passed.

- [ ] **Step 6: Commit**

```bash
git add src/Infinity.WebApplication/Models/User.cs \
        src/Infinity.WebApplication/Data/UserDbContext.cs \
        src/Infinity.WebApplication.Tests/Services/UserDbContextTests.cs
git commit -m "feat: add User model and UserDbContext"
```

---

## Task 5: UserService (TDD)

**Files:**
- Create: `src/Infinity.WebApplication.Tests/Services/UserServiceTests.cs`
- Create: `src/Infinity.WebApplication/Services/UserService/IUserService.cs`
- Create: `src/Infinity.WebApplication/Services/UserService/UserService.cs`

- [ ] **Step 1: Write failing tests**

Create `src/Infinity.WebApplication.Tests/Services/UserServiceTests.cs`:

```csharp
using Infinity.WebApplication.Data;
using Infinity.WebApplication.Services.UserService;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Tests.Services;

public class UserServiceTests
{
    private UserDbContext _db = null!;
    private UserService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new UserDbContext(
            new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        // workFactor: 4 keeps BCrypt fast in tests
        _sut = new UserService(_db, workFactor: 4);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task RegisterAsync_WithValidCredentials_ReturnsUser()
    {
        var user = await _sut.RegisterAsync("testuser", "test@example.com", "password123");

        Assert.That(user, Is.Not.Null);
        Assert.That(user!.Username, Is.EqualTo("testuser"));
        Assert.That(user.Email, Is.EqualTo("test@example.com"));
        Assert.That(user.PasswordHash, Is.Not.EqualTo("password123"));
    }

    [Test]
    public async Task RegisterAsync_WithDuplicateUsername_ReturnsNull()
    {
        await _sut.RegisterAsync("testuser", "first@example.com", "password123");

        var duplicate = await _sut.RegisterAsync("testuser", "second@example.com", "password456");

        Assert.That(duplicate, Is.Null);
    }

    [Test]
    public async Task RegisterAsync_WithDuplicateEmail_ReturnsNull()
    {
        await _sut.RegisterAsync("firstuser", "same@example.com", "password123");

        var duplicate = await _sut.RegisterAsync("seconduser", "same@example.com", "password456");

        Assert.That(duplicate, Is.Null);
    }

    [Test]
    public async Task LoginAsync_WithCorrectPassword_ReturnsUser()
    {
        await _sut.RegisterAsync("testuser", "test@example.com", "correctpassword");

        var user = await _sut.LoginAsync("testuser", "correctpassword");

        Assert.That(user, Is.Not.Null);
        Assert.That(user!.Username, Is.EqualTo("testuser"));
    }

    [Test]
    public async Task LoginAsync_WithWrongPassword_ReturnsNull()
    {
        await _sut.RegisterAsync("testuser", "test@example.com", "correctpassword");

        var user = await _sut.LoginAsync("testuser", "wrongpassword");

        Assert.That(user, Is.Null);
    }

    [Test]
    public async Task LoginAsync_WithUnknownUsername_ReturnsNull()
    {
        var user = await _sut.LoginAsync("nobody", "password");

        Assert.That(user, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_WithValidId_ReturnsUser()
    {
        var created = await _sut.RegisterAsync("testuser", "test@example.com", "password123");

        var found = await _sut.GetByIdAsync(created!.Id);

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        var found = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.That(found, Is.Null);
    }
}
```

- [ ] **Step 2: Run to verify they fail**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "UserServiceTests"
```

Expected: Build error — `Infinity.WebApplication.Services.UserService` does not exist yet.

- [ ] **Step 3: Create the interface**

Create `src/Infinity.WebApplication/Services/UserService/IUserService.cs`:

```csharp
using Infinity.WebApplication.Models;

namespace Infinity.WebApplication.Services.UserService;

public interface IUserService
{
    Task<User?> RegisterAsync(string username, string email, string password);
    Task<User?> LoginAsync(string username, string password);
    Task<User?> GetByIdAsync(Guid id);
}
```

- [ ] **Step 4: Create the implementation**

Create `src/Infinity.WebApplication/Services/UserService/UserService.cs`:

```csharp
using Infinity.WebApplication.Data;
using Infinity.WebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Services.UserService;

public sealed class UserService(UserDbContext db, int workFactor = 11) : IUserService
{
    public async Task<User?> RegisterAsync(string username, string email, string password)
    {
        bool taken = await db.Users.AnyAsync(u => u.Username == username || u.Email == email);
        if (taken) return null;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor),
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null) return null;
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    public async Task<User?> GetByIdAsync(Guid id) =>
        await db.Users.FindAsync(id);
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj --filter "UserServiceTests"
```

Expected: 8 tests passed.

- [ ] **Step 6: Run the full test suite to verify no regressions**

```bash
dotnet test src/Infinity.WebApplication.Tests/Infinity.WebApplication.Tests.csproj
```

Expected: All tests passed.

- [ ] **Step 7: Commit**

```bash
git add src/Infinity.WebApplication/Services/UserService/ \
        src/Infinity.WebApplication.Tests/Services/UserServiceTests.cs
git commit -m "feat: add IUserService and UserService with BCrypt hashing"
```

---

## Task 6: Register Services in Program.cs and Add Connection Strings

**Files:**
- Modify: `src/Infinity.WebApplication/appsettings.json`
- Modify: `src/Infinity.WebApplication/appsettings.Development.json`
- Modify: `src/Infinity.WebApplication/Program.cs`

- [ ] **Step 1: Add connection string to appsettings.json**

Open `src/Infinity.WebApplication/appsettings.json` and replace the contents with:

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
  }
}
```

- [ ] **Step 2: Add localhost connection string for Development**

Open `src/Infinity.WebApplication/appsettings.Development.json` and replace the contents with:

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
  "InfinityApi": {
    "BaseUrl": "http://localhost:8080"
  },
  "ConnectionStrings": {
    "UserConnection": "Host=localhost;Port=5433;Database=infinity_users;Username=postgres;Password=postgres"
  }
}
```

Note: Port `5433` is used in development so `userdb` doesn't conflict with the existing `db` container on `5432`. This is wired in compose in Task 7.

- [ ] **Step 3: Register UserDbContext and UserService in Program.cs**

Open `src/Infinity.WebApplication/Program.cs` and replace the contents with:

```csharp
using Infinity.WebApplication.Data;
using Infinity.WebApplication.Services.Home;
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
```

- [ ] **Step 4: Verify the WebApp builds**

```bash
dotnet build src/Infinity.WebApplication/Infinity.WebApplication.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/Infinity.WebApplication/appsettings.json \
        src/Infinity.WebApplication/appsettings.Development.json \
        src/Infinity.WebApplication/Program.cs
git commit -m "feat: register UserDbContext and UserService in WebApp Program.cs"
```

---

## Task 7: Generate EF Migration

**Files:**
- Create: `src/Infinity.WebApplication/Migrations/` (EF-generated)

- [ ] **Step 1: Generate the initial migration**

Run from the repo root:

```bash
dotnet ef migrations add InitialCreate \
  --project src/Infinity.WebApplication/Infinity.WebApplication.csproj \
  --context UserDbContext \
  --output-dir Migrations
```

Expected: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 2: Verify the migration files were created**

```bash
ls src/Infinity.WebApplication/Migrations/
```

Expected: You see files like `<timestamp>_InitialCreate.cs` and `UserDbContextModelSnapshot.cs`.

- [ ] **Step 3: Commit**

```bash
git add src/Infinity.WebApplication/Migrations/
git commit -m "feat: add EF Core initial migration for UserDbContext"
```

---

## Task 8: Update Docker Compose

**Files:**
- Modify: `src/compose.yaml`

- [ ] **Step 1: Update compose.yaml**

Open `src/compose.yaml` and replace the entire contents with:

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
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://0.0.0.0:8080
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

Note: `userdb` exposes port `5433` on the host for local development convenience (matches the `appsettings.Development.json` override from Task 6). It is only reachable by `infinity.webapp` via `webapp-net` in the compose stack.

- [ ] **Step 2: Verify the compose file is valid**

```bash
docker compose -f src/compose.yaml config --quiet
```

Expected: No errors printed.

- [ ] **Step 3: Commit**

```bash
git add src/compose.yaml
git commit -m "feat: add userdb container and webapp service with isolated Docker networks"
```

---

## Spec Coverage Check

| Spec requirement | Task |
|---|---|
| Dedicated `userdb` Postgres container | Task 8 |
| `webapp-net` — webapp + userdb only | Task 8 |
| `api-net` — webapi + locations db only | Task 8 |
| WebApp joins both networks | Task 8 |
| `userdb` has no host port exposure (enforced) | Task 8 — note: host port `5433` exposed for dev convenience; in production remove `ports:` from `userdb` |
| EF Core (Npgsql) in WebApp | Tasks 3, 4, 6 |
| User model: id, username, email, password_hash | Task 4 |
| Unique indexes on username and email | Task 4 |
| `UserDbContext` with migrations | Tasks 4, 7 |
| `db.Database.MigrateAsync()` on startup | Task 6 |
| `IUserService` / `UserService` | Task 5 |
| BCrypt hashing, raw password never stored | Task 5 |
| Configurable work factor (low in tests) | Task 5 |
| Tests in `Infinity.WebApplication.Tests` | Tasks 2, 4, 5 |
| Delete WebAPI User stub | Task 1 |
