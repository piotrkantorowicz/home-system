namespace Budget.Api;

using System.Security.Claims;
using Budget.Application.Commands.InitializeBudget;
using Budget.Application.Queries.GetBudget;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Cqrs;

internal static class BudgetEndpoints
{
    internal static IEndpointRouteBuilder MapBudgetEndpointsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/budget")
            .RequireAuthorization()
            .WithTags("Budget");

        group.MapGet("/", Get)
            .WithName("GetBudget")
            .WithSummary("Get the household's budget; 404 until it is initialised");
        group.MapPost("/", Initialize)
            .WithName("InitializeBudget")
            .WithSummary("Create the household's budget and default envelope, or return the existing one");

        return app;
    }

    private static async Task<Results<Ok<BudgetDto>, NotFound>> Get(
        ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct)
    {
        var budget = await dispatcher.SendAsync<GetBudgetQuery, BudgetDto?>(new GetBudgetQuery(Sub(user)), ct);
        return budget is null ? TypedResults.NotFound() : TypedResults.Ok(budget);
    }

    private static async Task<Results<Created<BudgetDto>, Ok<BudgetDto>>> Initialize(
        InitializeBudgetRequest request, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        var result = await dispatcher.SendAsync<InitializeBudgetCommand, InitializeBudgetResult>(
            new InitializeBudgetCommand(Sub(user), request.Currency), ct);

        return result.Created
            ? TypedResults.Created("/api/budget", result.Budget)
            : TypedResults.Ok(result.Budget);
    }

    private static string Sub(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("Missing subject claim.");
}

/// <summary>Body of <c>POST /api/budget</c>.</summary>
/// <param name="Currency"><c>PLN</c> (default when omitted), <c>EUR</c> or <c>USD</c>; immutable once the budget exists.</param>
internal sealed record InitializeBudgetRequest(string? Currency);
