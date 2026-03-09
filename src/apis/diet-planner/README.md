# Diet Planner API

.NET 10 Web API for diet planning — products, recipes, diet plans, and nutrition tracking.

## Requirements

- .NET 10 SDK
- PostgreSQL 16 (via Docker: `docker compose --profile diet-planner up -d` from `infrastructure/`)
- Authentik running locally (see `infrastructure/README.md`)

## Configuration

Sensitive values go in `appsettings.Development.json` (gitignored). Copy and fill in:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=dietplanner;Username=dietplanner;Password=<DIETPLANNER_DB_PASSWORD>"
  },
  "Authentication": {
    "Authentik": {
      "Authority": "http://localhost:9000/application/o/diet-planner-ui",
      "Audience": "<OIDC_CLIENT_ID>",
      "ClientId": "<OIDC_CLIENT_ID>",
      "ClientSecret": "<OIDC_CLIENT_SECRET>"
    }
  }
}
```

## Running

```bash
cd src/apis/diet-planner/DietPlanner.Api
dotnet run
```

API starts on http://localhost:5000. Database migrations are applied automatically on startup in development.

- **API docs (Scalar)**: http://localhost:5000/swagger
- **OpenAPI schema**: http://localhost:5000/openapi/v1.json
- **Health check**: http://localhost:5000/health

## Testing

```bash
# All tests
dotnet test src/apis/diet-planner/DietPlanner.Tests/DietPlanner.Tests.csproj

# Single test
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"
```

Integration tests use Testcontainers — they spin up a real PostgreSQL instance automatically, no manual DB setup needed.

**Test structure:**
- `Unit/` — pure logic tests (validators, calculators, utils)
- `Integration/` — full service tests against real PostgreSQL
- `Builders/` — test data builders for domain entities

## Architecture

Feature-folder layout under `Features/<Feature>/`:

| File | Purpose |
|---|---|
| `<Feature>Endpoints.cs` | Minimal API route definitions (`Map*Endpoints` extension method) |
| `<Feature>Service.cs` | Business logic behind `I<Feature>Service` interface |
| `<Feature>Dtos.cs` | Request/response records; responses have `static FromEntity()` factory |
| `<Feature>Validator.cs` | FluentValidation validators |

Each feature's endpoints are registered in `Program.cs` via `app.Map*Endpoints()`.

**Common infrastructure:**

| Component | Location |
|---|---|
| Custom exceptions | `Common/Exceptions/` |
| Global error handling | `Common/Middleware/ErrorHandlingMiddleware.cs` |
| User ID extraction | `Common/Extensions/ClaimsPrincipalExtensions.cs` → `context.User.GetUserId()` |
| Pagination model | `Common/Models/PagedResult<T>` |
| Unit conversion | `Common/Utils/UnitConverter.cs` |

## Key Behaviours

- **Auth**: JWT Bearer via Authentik OIDC. All `/api/v1/*` routes require authorization.
- **Soft deletes**: `DeletedAt` column with EF global query filters. Use `.IgnoreQueryFilters()` to include deleted records.
- **Rate limiting**: `"api"` policy (100 req/min per user), `"import"` policy (25 req/min per user).
- **Migrations**: Applied automatically on startup in development.
- **Request size**: Max 5 MB (configurable via `RateLimiting:MaxImportSizeMB`).

## API Endpoints

| Method | Path | Description |
|---|---|---|
| GET | `/api/v1/products` | List products (search, pagination, ownership filter) |
| POST | `/api/v1/products` | Create product |
| GET | `/api/v1/products/{id}` | Get product |
| PUT | `/api/v1/products/{id}` | Update product |
| DELETE | `/api/v1/products/{id}` | Soft delete product |
| GET | `/api/v1/recipes` | List recipes |
| POST | `/api/v1/recipes` | Create recipe with ingredients |
| GET | `/api/v1/recipes/{id}` | Get recipe with nutrition |
| PUT | `/api/v1/recipes/{id}` | Update recipe |
| DELETE | `/api/v1/recipes/{id}` | Soft delete recipe |
| GET | `/api/v1/diet-plans` | List user's diet plans |
| GET | `/api/v1/diet-plans/{id}` | Get plan details |
| DELETE | `/api/v1/diet-plans/{id}` | Delete plan |
| GET | `/api/v1/diet-plans/{id}/meals` | Get meals for date range |
| POST | `/api/v1/diet-plans/validate` | Validate import JSON (dry run) |
| POST | `/api/v1/diet-plans/import` | Execute import |
