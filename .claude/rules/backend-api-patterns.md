# Backend — API Patterns (Minimal API)

## Endpoint Organization

One `internal static class {Aggregate}Endpoints` per resource, one `Map…Endpoints` extension,
one private static method per operation. The module's `Api/DependencyInjection.cs` exposes a
single public `Map{Module}Endpoints(this IEndpointRouteBuilder app)` that calls them all.

```csharp
namespace Household.Api;

using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.Abstractions.Cqrs;

internal static class HouseholdEndpoints
{
    internal static IEndpointRouteBuilder MapHouseholdEndpointsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/households")
            .RequireAuthorization()
            .WithTags("Households");

        group.MapPost("/", Create).WithName("CreateHousehold");
        group.MapGet("/me", GetMine).WithName("GetMyHousehold");
        group.MapPut("/{id:guid}", Rename).WithName("RenameHousehold");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteHousehold");

        return app;
    }

    private static async Task<Created> Create(
        CreateHouseholdRequest request, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        var id = await dispatcher.SendAsync<CreateHouseholdCommand, Guid>(
            new CreateHouseholdCommand(user.Subject(), request.Name), ct);
        return TypedResults.Created($"/api/households/{id}");
    }

    private static async Task<Results<Ok<MyHouseholdDto>, NotFound>> GetMine(
        ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct)
    {
        var result = await dispatcher.SendAsync<GetMyHouseholdQuery, MyHouseholdDto?>(
            new GetMyHouseholdQuery(user.Subject()), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<NoContent> Rename(
        Guid id, RenameHouseholdRequest request, ClaimsPrincipal user,
        ICommandDispatcher dispatcher, CancellationToken ct)
    {
        await dispatcher.SendAsync(new RenameHouseholdCommand(user.Subject(), id, request.Name), ct);
        return TypedResults.NoContent();
    }
}
```

### Typed results, not `IResult`

Return types are the concrete `Microsoft.AspNetCore.Http.HttpResults` types
(`Ok<T>`, `Created`, `NoContent`, `NotFound`, …) or `Results<A, B, …>` when more than one is
possible. The OpenAPI document is **inferred from the signature**, so:

- No `.Produces<T>()` / `.Produces(404)` / `.ProducesValidationProblem()` chains — they are
  redundant with typed results and drift from the code.
- The compiler rejects an endpoint that returns a result type it did not declare.
- Legacy `Task<IResult>` + `.Produces` endpoints (DietPlanner, Notifications) are being migrated
  under #269; new endpoints never use `IResult`.

Status codes produced by middleware (400 validation, 403, 404 from `NotFoundException`, 422
domain, 401) are documented once via the global `ProblemDetails` OpenAPI transformer in the host,
not per endpoint.

### Metadata that still belongs on the endpoint

```csharp
group.MapPost("/", Create)
     .WithName("CreateHousehold")                       // operationId → generated TS client method name
     .WithSummary("Create a household owned by the caller");
```

`.WithName` is mandatory (the frontend generator keys on it). `.WithSummary` when the route
alone does not explain the operation. Never `.WithOpenApi()` — obsolete since .NET 9; the
document comes from `AddOpenApi()` + transformers.

## Request / Response Records

Requests live in the Api project as `sealed record`s. Query strings bind through
`[AsParameters]` records with defaults.

```csharp
public sealed record CreateHouseholdRequest(string Name);

public sealed record ListProductsParams(
    string? Search = null,
    bool OnlyMine = false,
    int Page = 1,
    int PageSize = 20);
```

Request shape validation (required, ranges) can use the built-in minimal-API validation
(`builder.Services.AddValidation()` + `DataAnnotations`, .NET 10). Business validation stays in
`ICommandValidator<T>` — the dispatcher runs it regardless of the entry point.

## HTTP Status Code Rules

| Scenario | Status | Typed result |
|---|---|---|
| Resource created | 201 | `TypedResults.Created(location)` / `Created<T>` |
| Action succeeded, no body | 204 | `TypedResults.NoContent()` |
| Resource found | 200 | `TypedResults.Ok(dto)` |
| Resource not found (query) | 404 | `TypedResults.NotFound()` |
| Resource not found (command) | 404 | throw `NotFoundException` in the handler |
| Validation failure | 400 | `CommandValidationException` → handler |
| Business rule violation | 422 | `DomainException` → handler |
| Not allowed for this user | 403 | `ForbiddenException` → handler |
| Unauthenticated | 401 | auth middleware |

Always `TypedResults.*`, never `Results.*`.

## Error Handling — `IExceptionHandler`

Exception → HTTP mapping lives in **one** `IExceptionHandler` in `Shared.Infrastructure.Web`,
registered with the framework's `AddProblemDetails()` / `UseExceptionHandler()`. This is the
.NET 8+ standard: content-negotiated `application/problem+json`, `traceId` included, works with
the OpenAPI `ProblemDetails` schema.

```csharp
namespace Shared.Infrastructure.Web;

using Microsoft.AspNetCore.Diagnostics;

public sealed class ApplicationExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        (int status, string title) = exception switch
        {
            CommandValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            NotFoundException          => (StatusCodes.Status404NotFound, "Not found"),
            ForbiddenException         => (StatusCodes.Status403Forbidden, "Forbidden"),
            DomainException            => (StatusCodes.Status422UnprocessableEntity, "Business rule violation"),
            _                          => (StatusCodes.Status500InternalServerError, "Internal server error"),
        };

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = new() { Status = status, Title = title, Detail = status == 500 ? null : exception.Message },
        });
    }
}

// Program.cs
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApplicationExceptionHandler>();
app.UseExceptionHandler();
```

Validation errors go into `ProblemDetails.Extensions["errors"]` grouped by property, matching
`ValidationProblemDetails`. Unhandled exceptions are logged by the framework's exception handler
middleware with the request path and trace id — do not log them again in the handler.

The handler owns the status code and the body only. Logging stays with the framework: the host passes
`ApplicationExceptionHandler.ShouldSuppressDiagnostics` as `ExceptionHandlerOptions.SuppressDiagnosticsCallback`,
so the mapped 4xx/499 exceptions are never logged as errors and an unhandled one is logged exactly once.
`OperationCanceledException` answers 499 with an empty body. Security headers come from
`SecurityHeadersMiddleware` (`app.UseMiddleware<SecurityHeadersMiddleware>()`), which writes them in
`Response.OnStarting` so they survive the exception handler's response reset.

## Authorization

- `.RequireAuthorization()` on every group. `.AllowAnonymous()` only with a comment saying why
  (`/health` is the only case today).
- The caller's identity comes from `ClaimsPrincipal` in the endpoint, through a small extension
  (`user.Subject()` → Authentik `sub`), and is passed **into** the command/query. Handlers never
  touch `HttpContext`.
- Household / role rules are enforced in the application layer (`HouseholdAccessService`),
  never by comparing IDs in the endpoint.

## OpenAPI

```csharp
// Program.cs
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<HomeSystemDocumentTransformer>());
app.MapOpenApi();                                   // /openapi/v1.json — consumed by openapi-typescript
if (app.Environment.IsDevelopment()) app.MapScalarApiReference();
```

- One document for the whole host; each module contributes tags via `.WithTags`.
- Document-level info, tags, bearer security scheme and the shared `ProblemDetails` responses
  live in a single `IOpenApiDocumentTransformer` class, not inline in `Program.cs`.
- Frontend types are generated from `/openapi/v1.json` (`npm run generate:api:<module>`). Any
  change to a request/response record must be followed by regenerating and committing the schema.

## Pagination

```csharp
// Shared.Abstractions.Core/Pagination/PagedList.cs
public sealed record PagedList<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

All list queries return `PagedList<TDto>`. Never an unbounded list. `PageSize` is clamped in
the query validator (max 100).
