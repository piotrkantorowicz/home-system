# Backend — API Patterns (Minimal API)

## Endpoint Organization

```csharp
// BudgetPlan.Api/BudgetPlanEndpoints.cs
public static class BudgetPlanEndpoints
{
    public static IEndpointRouteBuilder MapBudgetPlanEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/budget-plans")
            .RequireAuthorization()
            .WithTags("BudgetPlans");

        group.MapPost("/", CreateBudgetPlan);
        group.MapGet("/{id:guid}", GetBudgetPlan);
        group.MapGet("/", ListBudgetPlans);
        group.MapPost("/{id:guid}/entries", AddEntry);
        group.MapDelete("/{id:guid}", CloseBudgetPlan);

        return app;
    }

    // Each endpoint is a private static method in the same class.
    // Inject ICommandDispatcher for writes and IQueryDispatcher for reads — never inject
    // ICommandHandler<,> / IQueryHandler<,> directly.
    private static async Task<IResult> CreateBudgetPlan(
        CreateBudgetPlanRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        var id = await dispatcher.SendAsync<CreateBudgetPlanCommand, Guid>(
            new CreateBudgetPlanCommand(request.UserId, request.LimitValue, request.LimitCurrency), ct);
        return TypedResults.Created($"/api/budget-plans/{id}");
    }

    private static async Task<IResult> GetBudgetPlan(
        Guid id,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var result = await dispatcher.SendAsync<GetBudgetPlanQuery, BudgetPlanDto?>(
            new GetBudgetPlanQuery(id), ct);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> ListBudgetPlans(
        [AsParameters] ListBudgetPlansParams @params,
        IQueryDispatcher dispatcher,
        CancellationToken ct)
    {
        var result = await dispatcher.SendAsync<ListBudgetPlansQuery, PagedList<BudgetPlanSummaryDto>>(
            new ListBudgetPlansQuery(@params.UserId, @params.Page, @params.PageSize), ct);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> AddEntry(
        Guid id,
        AddBudgetEntryRequest request,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        await dispatcher.SendAsync(
            new AddBudgetEntryCommand(id, request.AmountValue, request.AmountCurrency, request.Description), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> CloseBudgetPlan(
        Guid id,
        ICommandDispatcher dispatcher,
        CancellationToken ct)
    {
        await dispatcher.SendAsync(new CloseBudgetPlanCommand(id), ct);
        return TypedResults.NoContent();
    }
}
```

## Request / Response Records

```csharp
// Requests live in the Api project (or Contracts if shared between modules)
public sealed record CreateBudgetPlanRequest(
    Guid UserId,
    decimal LimitValue,
    string LimitCurrency);

public sealed record AddBudgetEntryRequest(
    decimal AmountValue,
    string AmountCurrency,
    string Description);

// Query parameters as struct with [AsParameters]
public sealed record ListBudgetPlansParams(
    Guid UserId,
    [property: FromQuery] int Page = 1,
    [property: FromQuery] int PageSize = 20);
```

## HTTP Status Code Rules

| Scenario | Status Code | Method |
|---|---|---|
| Resource created | 201 Created | `TypedResults.Created(location)` |
| Action succeeded, no body | 204 No Content | `TypedResults.NoContent()` |
| Resource found | 200 OK | `TypedResults.Ok(dto)` |
| Resource not found | 404 Not Found | `TypedResults.NotFound()` |
| Validation failure | 400 Bad Request | handled by middleware |
| Business rule violation | 422 Unprocessable Entity | handled by middleware |
| Unauthenticated | 401 Unauthorized | handled by auth middleware |
| Unauthorized | 403 Forbidden | `TypedResults.Forbid()` |

Always use `TypedResults.*` — not `Results.*` — so OpenAPI schema is inferred automatically.

## Error Handling Middleware

```csharp
// Shared.Infrastructure.Web/ExceptionHandlingMiddleware.cs
public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
        => _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails { Title = "Validation failed", Detail = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)) });
        }
        catch (DomainException ex)
        {
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails { Title = "Business rule violation", Detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails { Title = "Internal server error" });
        }
    }
}
```

## Authorization

- **All endpoints require authorization by default** via `.RequireAuthorization()` on the group.
- Add `[AllowAnonymous]` (or `.AllowAnonymous()`) only explicitly and with a comment explaining why.
- Extract the current user ID from the JWT claims in the endpoint, not in the handler:
  ```csharp
  private static async Task<IResult> CreateBudgetPlan(
      CreateBudgetPlanRequest request,
      ClaimsPrincipal user,
      ICommandDispatcher dispatcher,
      CancellationToken ct)
  {
      var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
      await dispatcher.SendAsync(new CreateBudgetPlanCommand(userId, request.LimitValue, request.LimitCurrency), ct);
      return TypedResults.Created();
  }
  ```

## OpenAPI / Swagger

```csharp
group.MapPost("/", CreateBudgetPlan)
     .WithName("CreateBudgetPlan")
     .WithSummary("Create a new budget plan for the current month")
     .Produces<Guid>(StatusCodes.Status201Created)
     .ProducesValidationProblem()
     .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
```

Add `.WithOpenApi()` at the group level. Describe each endpoint's responses explicitly.

## Pagination

Use a consistent pagination wrapper for list endpoints:

```csharp
// Shared.Abstractions.Core/Pagination/PagedList.cs
public sealed record PagedList<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

All list query handlers return `PagedList<TDto>`. Never return unlimited lists.
