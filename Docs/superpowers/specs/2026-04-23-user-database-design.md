# User Database Design

**Date:** 2026-04-23
**Scope:** WebApp-owned user database — containerized, isolated from the WebAPI

---

## Overview

A dedicated PostgreSQL container (`userdb`) stores user account data. It is accessible only by the `infinity.webapp` container via a private Docker network. The WebAPI has no route to it. EF Core (Npgsql) is used for data access within the WebApp, consistent with the project's existing ORM choice.

---

## 1. Docker Infrastructure

Two named Docker networks replace the default compose network:

| Network | Members |
|---|---|
| `api-net` | `infinity.webapi`, `db` (locations) |
| `webapp-net` | `infinity.webapp`, `userdb` |

`infinity.webapp` joins **both** networks — `api-net` to reach the WebAPI and `webapp-net` to reach `userdb`.

`userdb` has **no `ports:` mapping** — it is unreachable from the host or any service not on `webapp-net`. Docker enforces this at the network layer.

The existing `db` and `infinity.webapi` services are unchanged except for the addition of `networks: [api-net]`.

### New services in compose.yaml

**`infinity.webapp`**
- Built from `Infinity.WebApplication/Dockerfile`
- Networks: `api-net`, `webapp-net`
- Environment: `InfinityApi__BaseUrl=http://infinity.webapi:8080`, `ConnectionStrings__UserConnection=Host=userdb;...`
- Depends on: `infinity.webapi`, `userdb`

**`userdb`**
- Image: `postgres:latest`
- Networks: `webapp-net` only
- No host port exposed
- Health check: `pg_isready -U postgres`

---

## 2. EF Core Setup in the WebApp

New files added to `Infinity.WebApplication`:

```
Data/
  UserDbContext.cs       — DbContext with Users DbSet
Models/
  User.cs                — entity definition
Migrations/              — EF-generated migrations
```

### User model

| Field | Type | Constraints |
|---|---|---|
| `Id` | `Guid` | PK, generated on insert |
| `Username` | `string` | unique index, required |
| `Email` | `string` | unique index, required |
| `PasswordHash` | `string` | bcrypt output, required |

### Registration in Program.cs

`UserDbContext` registered as a scoped service with the Npgsql provider using the `UserConnection` connection string.

### Connection strings

- `appsettings.json` — `Host=userdb;Database=infinity_users;Username=postgres;Password=postgres`
- `appsettings.Development.json` — `Host=localhost;...` (for running WebApp outside Docker locally)

### Migrations strategy

Migrations are generated via the EF CLI (`dotnet ef migrations add`) during development. On container startup, `Program.cs` calls `db.Database.Migrate()` so the schema is applied automatically without a manual step — matching the pattern used by the WebAPI.

### Cleanup

`Infinity.WebApi/Models/User.cs` (stub with id/name/age) is deleted as part of this work — no API code references it.

---

## 3. Service Layer

Controllers never access `UserDbContext` directly. All user DB operations go through `UserService`.

```
Services/
  IUserService.cs
  UserService.cs
```

### IUserService interface

```csharp
Task<User?> RegisterAsync(string username, string email, string password);
Task<User?> LoginAsync(string username, string password);
Task<User?> GetByIdAsync(Guid id);
```

### UserService behavior

- **RegisterAsync** — verifies username and email uniqueness, hashes password with BCrypt.Net-Next, inserts user, returns created entity. Returns `null` if username or email is already taken.
- **LoginAsync** — fetches user by username, verifies BCrypt hash. Returns user on success, `null` on failure.
- **GetByIdAsync** — fetches user by primary key for session hydration.

The raw password never reaches the database. Hashing occurs inside `UserService` before any EF call.

**NuGet dependency:** `BCrypt.Net-Next`

`UserService` is registered as scoped in `Program.cs` and injected into auth controllers.

---

## 4. Testing

Tests live in `Infinity.WebApplication.Tests` (dedicated project, already created).

- **Unit tests for `UserService`** — use EF Core's in-memory provider or mock `UserDbContext` to test registration, login, and uniqueness enforcement without a real database.
- **BCrypt work factor** — tests that invoke `RegisterAsync` or `LoginAsync` use a low work factor (e.g., `workFactor: 4`) to keep the suite fast.

---

## Non-Goals

- Session/cookie management (separate concern, future work)
- User profile editing
- Password reset flow
- Admin roles or authorization beyond authentication
