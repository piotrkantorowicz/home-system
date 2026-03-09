# Diet Planner API

A .NET 10 Web API for diet planning with JSON import capabilities, Authentik authentication, and PostgreSQL database.

## Features

- **Import diet plans from JSON** with comprehensive pre-validation
- **Global products and recipes** with creator tracking
- **On-the-fly nutrition calculation** with unit conversion
- **Authentik OIDC authentication** for secure access
- **Soft delete support** for products and recipes
- **Rate limiting and security hardening**
- **Comprehensive validation** using FluentValidation

## Project Structure

```
diet-planner/
├── infrastructure/              # Docker Compose setup for Authentik + PostgreSQL
├── DietPlanner.Api/            # Main API project
│   ├── Domain/                 # Entity models
│   ├── Data/                   # DbContext and migrations
│   ├── Features/               # Feature-based organization
│   ├── Common/                 # Shared utilities and middleware
│   └── Program.cs
├── DietPlanner.Tests/          # Test project
└── IMPLEMENTATION-PLAN.md      # Detailed implementation guide
```

## Quick Start

### Prerequisites

- .NET 10 SDK
- Docker and Docker Compose
- PostgreSQL client tools (optional, for database access)

### 1. Start Infrastructure

```bash
cd infrastructure
cp .env.example .env

# Generate secure secrets
echo "AUTHENTIK_SECRET_KEY=$(openssl rand -base64 36)" > .env
echo "AUTHENTIK_DB_PASSWORD=$(openssl rand -base64 24)" >> .env
echo "DIETPLANNER_DB_PASSWORD=$(openssl rand -base64 24)" >> .env

# Start services
docker compose up -d

# Wait ~30 seconds, then check health
docker compose ps

# Create Authentik admin user
docker compose exec authentik-server ak create_admin_user
```

### 2. Configure Authentik

1. Open http://localhost:9000/if/admin/
2. Login with the admin credentials you just created
3. Follow the setup guide in `infrastructure/README.md` to:
   - Create an OAuth2/OIDC Provider
   - Create an Application
   - Note the Client ID and Client Secret

### 3. Configure API

Update `DietPlanner.Api/appsettings.Development.json`:

```json
{
  "Authentication": {
    "Authentik": {
      "Authority": "http://localhost:9000/application/o/diet-planner/",
      "Audience": "diet-planner-api",
      "ClientId": "diet-planner-api",
      "ClientSecret": "YOUR_CLIENT_SECRET_FROM_AUTHENTIK",
      "RequireHttpsMetadata": false
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=dietplanner;Username=dietplanner;Password=YOUR_DB_PASSWORD_FROM_ENV"
  }
}
```

### 4. Run Database Migrations

```bash
cd DietPlanner.Api
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. Run the API

```bash
dotnet run
```

The API will be available at http://localhost:5000

### 6. Test the API

Access the Swagger UI at http://localhost:5000/swagger

Or test with curl:

```bash
# Health check (no auth required)
curl http://localhost:5000/health

# Get a token
TOKEN=$(curl -X POST http://localhost:9000/application/o/token/ \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=diet-planner-api" \
  -d "client_secret=YOUR_CLIENT_SECRET" \
  -d "username=YOUR_ADMIN_USERNAME" \
  -d "password=YOUR_ADMIN_PASSWORD" \
  -d "scope=openid profile email" | jq -r '.access_token')

# Use the token to access protected endpoints
curl http://localhost:5000/api/v1/products \
  -H "Authorization: Bearer $TOKEN"
```

## Development

### Run Tests

```bash
dotnet test
```

### Build

```bash
dotnet build
```

### Database Migrations

```bash
# Create a new migration
dotnet ef migrations add MigrationName --project DietPlanner.Api

# Apply migrations
dotnet ef database update --project DietPlanner.Api

# Remove last migration (if not applied)
dotnet ef migrations remove --project DietPlanner.Api
```

## API Endpoints

### Products

- `GET /api/v1/products` - List products (with pagination and search)
- `GET /api/v1/products/{id}` - Get product details
- `POST /api/v1/products` - Create product
- `PUT /api/v1/products/{id}` - Update product (owner only)
- `DELETE /api/v1/products/{id}` - Soft delete product (owner only)

### Recipes

- `GET /api/v1/recipes` - List recipes (with pagination and search)
- `GET /api/v1/recipes/{id}` - Get recipe with ingredients & nutrition
- `POST /api/v1/recipes` - Create recipe
- `PUT /api/v1/recipes/{id}` - Update recipe (owner only)
- `DELETE /api/v1/recipes/{id}` - Soft delete recipe (owner only)

### Diet Plans

- `GET /api/v1/diet-plans` - List user's diet plans
- `GET /api/v1/diet-plans/{id}` - Get plan with all meals
- `POST /api/v1/diet-plans` - Create empty plan
- `DELETE /api/v1/diet-plans/{id}` - Delete plan
- `POST /api/v1/diet-plans/validate` - Validate import JSON (dry run)
- `POST /api/v1/diet-plans/import` - Import full plan from JSON
- `GET /api/v1/diet-plans/{id}/meals` - Get meals by date range
- `GET /api/v1/diet-plans/{id}/nutrition` - Get nutrition summary
- `GET /api/v1/diet-plans/{id}/shopping` - Get shopping list

### Health

- `GET /health` - Health check endpoint

## Import JSON Format

See `IMPLEMENTATION-PLAN.md` for detailed JSON format documentation and examples.

Basic structure:

```json
{
  "planName": "January 2025 Diet",
  "startDate": "2025-01-01",
  "endDate": "2025-01-31",
  "products": [...],
  "recipes": [...],
  "schedule": [...]
}
```

## Technology Stack

- **.NET 10** - Latest .NET framework
- **PostgreSQL 16** - Database
- **Entity Framework Core 10** - ORM
- **Authentik** - OIDC authentication
- **FluentValidation** - Input validation
- **Swagger/OpenAPI** - API documentation
- **xUnit** - Testing framework

## Documentation

- [Implementation Plan](IMPLEMENTATION-PLAN.md) - Detailed technical specification
- [Infrastructure Setup](infrastructure/README.md) - Docker and Authentik configuration

## Security Features

- JWT Bearer authentication with Authentik
- Rate limiting on import endpoints (5 requests/minute)
- Request size limits (5MB max)
- Ownership-based authorization
- Soft delete for data retention
- Input validation on all endpoints

## Performance Optimizations

- In-memory caching for products and recipes
- Bulk operations for import
- Database query optimization with eager loading
- GIN indexes for full-text search

## License

MIT License

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## Support

For issues and questions, please open an issue on the GitHub repository.
