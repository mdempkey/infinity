# Bearer Token Authentication Design

**Date:** 2026-04-28
**Scope:** Infinity.WebApi (LocationsDB) and Infinity.WebApplication (UserDB)

---

## Overview

Two distinct authentication concerns are addressed:

1. **Service-to-service auth** — WebApplication authenticates to WebApi using a machine-to-machine (M2M) JWT service token.
2. **User action auth** — User-specific actions on WebApplication (reviews, ratings, profile management) require the logged-in user's JWT bearer token.

---

## Token Architecture

Both apps share the signing key (sourced from the `JWT_SIGNING_KEY` environment variable) and `Jwt:Issuer` in configuration. Audiences distinguish token types:

| Token Type | Issued By | Audience | Used By |
|---|---|---|---|
| User token | WebApplication AuthController | `infinity-app` | Browser clients |
| Service token | WebApplication IServiceTokenProvider | `infinity-api` | WebApplication HttpClient → WebApi |

WebApi only validates service tokens (audience `infinity-api`). It never sees user tokens.
WebApplication validates user tokens against audience `infinity-app`.

---

## Configuration Changes

### Signing Key

The signing key is provided via the `JWT_SIGNING_KEY` environment variable in both apps. `Jwt:Key` in `appsettings.json` is intentionally left empty — the env var takes precedence at runtime. Both apps must be started with the same `JWT_SIGNING_KEY` value; if they differ, WebApi will reject service tokens.

### WebApplication `appsettings.json` — add two keys (existing `Jwt` block already present)

`Jwt:Key` is already present but empty (supplied by env var). Add `ServiceAudience` and `ServiceExpiryHours`:

```json
"Jwt": {
  "Key": "",
  "Issuer": "infinity-web",
  "Audience": "infinity-client",
  "ExpiryHours": 24,
  "ServiceAudience": "infinity-api",
  "ServiceExpiryHours": 1
}
```

### WebApplication `Program.cs` — no changes needed

JWT bearer auth middleware (`AddAuthentication`, `UseAuthentication`, `UseAuthorization`) is already fully configured. The existing code reads `Jwt:Key` from configuration, which resolves to `JWT_SIGNING_KEY` at runtime.

### WebApi `appsettings.json` — new `Jwt` block (nothing exists today)

`Jwt:Key` is left empty here too — supplied by the shared `JWT_SIGNING_KEY` env var:

```json
"Jwt": {
  "Key": "",
  "Issuer": "infinity-web",
  "Audience": "infinity-api"
}
```

### WebApi `Program.cs` — JWT middleware to be added (nothing exists today)

```csharp
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

// In pipeline:
app.UseAuthentication();
app.UseAuthorization();
```

---

## Service Token Generation & Injection (WebApplication)

### IServiceTokenProvider

A new `IServiceTokenProvider` singleton generates short-lived JWTs for the service-to-service call:

- Audience: `infinity-api`
- Expiry: 1 hour
- Claims: `ClaimTypes.Role = "Service"`
- Caches the token in memory; regenerates when within 60 seconds of expiry

### ServiceTokenHandler (DelegatingHandler)

A `DelegatingHandler` registered on the `HttpClient` for `IIndexContentService`. On every outgoing request it calls `IServiceTokenProvider.GetTokenAsync()` and attaches the result as `Authorization: Bearer <token>`. No manual token management needed in service code.

### Registration in Program.cs

```csharp
builder.Services.AddSingleton<IServiceTokenProvider, ServiceTokenProvider>();
builder.Services.AddTransient<ServiceTokenHandler>();

builder.Services.AddHttpClient<IIndexContentService, IndexContentService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["InfinityApi:BaseUrl"]!);
})
.AddHttpMessageHandler<ServiceTokenHandler>();
```

---

## WebApi Authentication Setup

JWT bearer auth middleware is added to `Infinity.WebApi/Program.cs` (see Configuration Changes above for the code). All existing controllers (`AttractionsController`, `ParksController`, `ImagesController`) receive `[Authorize]` at the class level.

---

## User Action Authorization (WebApplication)

The following endpoints require `[Authorize]`:

| Controller | Actions Protected | Actions Exempt |
|---|---|---|
| AuthController | — | Register, Login |
| ReviewsController | All (create, read-own, update, delete) | — |
| RatingsController | All | — |
| AccountController | UpdateProfile, DeleteAccount | — |

The authenticated user's ID is read from `ClaimTypes.NameIdentifier` in the token and used to scope all DB queries, ensuring users can only modify their own data.

---

## Non-Goals

- OAuth2 / external identity provider
- Role-based access beyond `"Service"` claim on service tokens
- User photo uploads or admin roles
