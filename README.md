# Infinity Site

Infinity Site is a full-stack web application for rating and reviewing Star Wars-themed 
attractions across theme parks worldwide. This capstone project demonstrates development 
practices including SCRUM methodology, microservices patterns, JWT authentication, and 
unit testing.

**Features:** Interactive map discovery, user accounts with JWT auth, 1-5 star ratings, reviews, 
attraction search & filter.

Created for the Software Engineering Capstone course at Chapman University.

### Pokemon Sounds??

This project also includes a horrendous yet hilarious integration with the class' resident
[Pokemon Sounds API](https://github.com/HBnax/se-498), allowing users to listen to Pokemon
cries singing the star wars main theme across the site.

## Technology Stack

### Backend & API

- **Language:** C# (.NET 10.0)
- **Framework:** ASP.NET Core (Web API + MVC)
- **ORM:** Entity Framework Core 10.0 with PostgreSQL provider
- **API Documentation:** Swagger/OpenAPI with Swashbuckle
- **Authentication:** JWT Bearer tokens
- **Security:** BCrypt.Net-Next for password hashing

### Frontend

- **Framework:** ASP.NET Core MVC with Razor views
- **Mapping:** Mapbox SDK for interactive attraction discovery
- **Static Assets:** Image serving through API endpoints

### Data & Persistence

- **Database Type:** PostgreSQL
  - **Attractions DB:** Theme parks, attractions, coordinates, descriptions, images
  - **Users DB:** User accounts, authentication, ratings, reviews
- **Migrations:** Entity Framework Core code-first migrations

### Testing & Quality

- **Unit Test Framework:** XUnit
- **Mocking Library:** Moq
- **Test Database:** Entity Framework Core InMemory provider
- **Test Coverage:** Controllers, Services, Data layers
- **Test SDK:** Microsoft.NET.Test.Sdk
- **Code Coverage:** Coverlet collector integration

### Deployment & Infrastructure

- **Containerization:** Docker (Podman-compatible)
- **Orchestration:** Docker Compose/Podman Compose
- **Networking:** Custom bridge networks for service isolation
- **Port Mapping:** API (8084), Web App (8082), Databases (5434/5433)

### Development & DevOps

- **Project Management:** SCRUM methodology with two-week sprints
- **Issue Tracking:** Backlog management in Jira
- **Version Control:** Git with feature branches and pull requests
- **CI/CD:** GitHub Actions (automated builds & tests on push to main or pull request)

## Architecture Overview

This project implements a microservices-oriented architecture:

```
┌──────────────────────────────────┐         ┌──────────────────────────────┐
│ Infinity.WebApplication          │         │ Infinity.WebApi              │
│ (ASP.NET Core MVC - Port 8082)   │         │ (ASP.NET Core API - 8084)    │
│ ├── Controllers (Page routing)   │         │ ├── Controllers (OpenAPI)    │
│ ├── Services (Business logic)    │ HTTP    │ ├── Services (Domain logic)  │
│ ├── Views (Razor templates)      │◄──────► │ ├── Data (Entity Framework)  │
│ └── Models (ViewModels)          │ (JWT)   │ └── Models (DTOs & entities) │
└────────────┬─────────────────────┘         └──────┬───────────────────────┘
             │                                      │
             │ Direct DB Access                     │ Direct DB Access
             │ (User auth, ratings, reviews)        │ (Parks, attractions, images)
             │                                      │
             ↓                                      ↓
    ┌────────────────────────┐          ┌────────────────────────┐
    │ Users Database         │          │ Attractions Database   │
    │ (PostgreSQL on 5433)   │          │ (PostgreSQL on 5434)   │
    │ - User accounts        │          │ - Theme parks          │
    │ - JWT sessions         │          │ - Attractions          │
    │ - Ratings & Reviews    │          │ - Images               │
    └────────────────────────┘          └────────────────────────┘
```

**Key Architectural Patterns:**
- **Microservices Pattern:** Separate API and Web layers
- **Database Architecture:** Logical separation of concerns (user data vs. attraction catalog)
- **Separation of Concerns:** Clear boundaries between Controllers, Services, and Data layers
- **Dependency Injection:** ASP.NET Core built-in DI container for loose coupling
- **Repository Pattern:** Data access abstraction via Entity Framework DbContext
- **JWT-based Authentication:** Stateless auth across microservices with bearer tokens

## Quick Start

### Prerequisites
- Docker & Docker Compose (or Podman & Podman Compose)
- `.env` file with API keys (see below)

### Environment Setup

Create a `.env` file in `/src`:
```env
# Mapbox integration
MAPBOX_ACCESS_TOKEN=your_mapbox_access_token_here

# JWT signing key for API authentication (any 32 character string)
JWT_SIGNING_KEY=your_jwt_signing_key_here_must_be_32_chars_or_longer

# Pokemon Sounds API Bearer token (can be any string)
BearerToken=your_api_bearer_token_here
```

### Build & Run

First, run the Pokemon API, then start this application:
```bash
cd src/
podman compose up --build
```

This starts the four containerized services:
- **Infinity API** (`localhost:8084`) - REST API with Swagger docs
- **Infinity Web App** (`localhost:8082`) - User-facing web interface
- **Attractions Database** (PostgreSQL on 5434) - Parks, rides, amenities
- **Users Database** (PostgreSQL on 5433) - Accounts, ratings, reviews

## Key Features Implemented

**User Authentication & Authorization**
- JWT bearer token authentication
- Secure password hashing with BCrypt
- Session management

**Attraction Discovery**
- Interactive map with Mapbox integration
- Multi-park filtering and search
- Category-based browsing (Rides, Restaurants, Shops, Shows)

**Rating & Review System**
- 1-5 star ratings per attraction
- Written reviews
- User review history and management (edit/delete)

**API Design**
- OpenAPI/Swagger documentation
- RESTful endpoints
- Service-to-service communication
- JWT-based authentication for API access

## Testing & Quality Assurance

The project maintains high code quality through:

- **Unit Tests:** Controller and service tests with XUnit
- **Integration Tests:** Database-level testing with InMemory provider
- **Mocking:** Moq for dependency isolation
- **CI/CD Pipeline:** Automated tests on every commit
- **Code Review:** Pull request verification process

## Future Enhancements

- Mobile applications (iOS/Android)
- User photo uploads
- Crowd forecasting
- Administrator dashboard for attraction management and review moderation
- Advanced analytics dashboard
