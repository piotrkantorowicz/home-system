# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Claude Code Rules

- Never add `Co-Authored-By: Claude` trailers to commit messages.

## Repository Structure

Mono-repo with two independent stacks:
- `src/apis/diet-planner/` — .NET 10 Web API
- `src/ui/` — React 19 + Vite + TypeScript SPA
- `infrastructure/` — Docker Compose (Authentik, Redis, PostgreSQL per module)

Two solution files: `HomeSystem.slnx` (root), `src/apis/diet-planner/DietPlanner.slnx` (standalone).

## Commands

### Infrastructure
```bash
cd infrastructure
cp .env.example .env          # then fill in secrets

docker compose up -d                              # Authentik + Redis (always)
docker compose --profile diet-planner up -d       # + PostgreSQL for diet-planner
docker compose down
```

### Backend (from `src/apis/diet-planner/DietPlanner.Api/`)
```bash
dotnet run                    # API on http://localhost:5000
dotnet build
dotnet test                   # all tests
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"   # single test
dotnet test src/apis/diet-planner/DietPlanner.Tests/DietPlanner.Tests.csproj
```

### Frontend (from `src/ui/`)
```bash
npm run dev                   # Vite dev server on http://localhost:5173
npm run build                 # tsc -b && vite build
npm run type-check            # tsc --noEmit
npm run lint                  # eslint .
npm run lint:fix
npm run format                # prettier --write "src/**/*.{ts,tsx,css,json}"
npm run format:check

# Regenerate OpenAPI client (API must be running)
npm run generate:api:diet-planner
```

### E2E Tests (from `src/ui/`)
```bash
npm run test:e2e              # playwright test
npm run test:e2e:ui           # with UI mode
npm run test:e2e:debug
npx playwright test e2e/diet-planner/products.spec.ts   # single file
npx playwright test -g "test name"                       # single test
```

## Key Ports

| Service | Port |
|---|---|
| Vite dev server | 5173 |
| .NET API | 5000 |
| Authentik | 9000 |
| PostgreSQL (diet-planner) | 5432 |

API docs (Scalar): http://localhost:5000/swagger
OpenAPI schema: http://localhost:5000/openapi/v1.json

## Backend Architecture

### Pattern: Feature Folders + Minimal APIs

Each feature lives in `Features/<Feature>/` with four files:
- `<Feature>Endpoints.cs` — route definitions (extension method `Map*Endpoints`)
- `<Feature>Service.cs` — business logic behind an interface `I<Feature>Service`
- `<Feature>Dtos.cs` — request/response records; response DTOs have `static FromEntity()` factory
- `<Feature>Validator.cs` — FluentValidation validators registered in DI

Endpoints are registered in `Program.cs` via `app.Map*Endpoints()`.

### Adding a New Endpoint

```csharp
public static class MealEndpoints
{
    public static void MapMealEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/diet-plans/{id}/meals")
            .WithTags("Meals")
            .RequireAuthorization();

        group.MapPost("/", async (Guid id, [FromBody] CreateMealRequest req,
            HttpContext context, [FromServices] IMealService svc) =>
        {
            var userId = context.User.GetUserId();
            var result = await svc.CreateAsync(id, userId, req);
            return Results.Created($"/api/v1/diet-plans/{id}/meals/{result.Id}", result);
        })
        .RequireRateLimiting("api")
        .WithName("CreateMeal")
        .Produces<MealEntryDto>(StatusCodes.Status201Created);
    }
}
```

### Key Infrastructure
- **Auth**: JWT via Authentik OIDC. User ID extracted via `context.User.GetUserId()` (extension in `Common/Extensions/`).
- **Errors**: Custom exceptions `NotFoundException`, `ForbiddenException`, `ValidationException` caught by `ErrorHandlingMiddleware`.
- **Soft deletes**: `DeletedAt` column with EF global query filters (`.IgnoreQueryFilters()` when needed).
- **Rate limiting**: `"api"` policy (100 req/min) or `"import"` policy (25 req/min) per user.
- **Migrations**: Applied automatically on startup in development.
- **Pagination**: `PagedResult<T>` from `Common/Models/`.

### Configuration
Sensitive values go in `appsettings.Development.json` (gitignored). Base `appsettings.json` has placeholders.

## Frontend Architecture

### Module System

New features are added as modules registered in `src/app/main.tsx` via `registerModule()`. Each module is defined in `src/modules/<module>/index.ts` and exports an `AppModule`:

```typescript
export const myModule: AppModule = {
  name: 'my-module',
  basePath: '/my-module',
  icon: SomeIcon,
  localeNamespaces: ['my-module'],
  i18nResources: { en: { 'my-module': en }, pl: { 'my-module': pl } },
  navItems: [...],
  routes: [
    { index: true, Component: Dashboard },
    { path: 'items', Component: ItemList },
    { path: 'items/:id', Component: ItemDetail },
  ],
};
```

Routes are lazy-loaded via `React.lazy()` in the router automatically.

### API Client

The typed API client is in `api/client.ts` per module, built on `openapi-fetch` with the generated schema from `api/generated/schema.ts`. JWT tokens are injected and auto-renewed via the shared interceptor in `src/shared/api/tokenInterceptor.ts`.

Regenerate the schema after backend changes: `npm run generate:api:diet-planner`.

### Hook Pattern (TanStack Query v5)

```typescript
// useItems.ts
export function useItems(params: ItemsQueryParams) {
  return useQuery({
    queryKey: ['items', params],
    queryFn: async () => {
      const response = await api.GET('/api/v1/items', { params: { query: params } });
      if (response.error) throw new Error('Failed to fetch items');
      return response.data;
    },
    placeholderData: keepPreviousData,
  });
}

export function useCreateItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (body) => {
      const response = await api.POST('/api/v1/items', { body });
      if (response.error) throw new Error('Failed to create item');
      return response.data;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['items'] }),
  });
}
```

### Path Aliases
- `@/*` → `src/*`
- `@shared/*` → `src/shared/*`
- `@modules/*` → `src/modules/*`

### Shared UI Components
Reusable components live in `src/shared/components/ui/` (Button, Card, Dialog, Input, Table, Badge, etc). Module-specific components go in `src/modules/<module>/components/`.

### i18n
Shared translation keys in `src/shared/locales/{en,pl}.json`. Module keys in `src/modules/<module>/locales/`. Module namespaces are merged at init — access module keys with `t('common.key')` or `t('diet-planner:key')`.

## Testing

### Backend
- **Unit tests**: `DietPlanner.Tests/Unit/` — pure logic, no DB
- **Integration tests**: `DietPlanner.Tests/Integration/` — use Testcontainers (spins up real PostgreSQL)
- **Builders**: `DietPlanner.Tests/Builders/` — test data builders for entities
- `DatabaseFixture` + `CustomWebApplicationFactory` handle test DB lifecycle

### Frontend E2E
- Auth state is set up once in `e2e/shared/auth.setup.ts`
- Page objects in `e2e/diet-planner/pages/`
- Global teardown cleans test data via `e2e/shared/global-teardown.ts`
