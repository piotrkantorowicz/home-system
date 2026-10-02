namespace Budget.Api;

using System.Security.Claims;
using Budget.Application.Commands.ArchiveAccount;
using Budget.Application.Commands.CreateAccount;
using Budget.Application.Commands.InitializeBudget;
using Budget.Application.Commands.RenameAccount;
using Budget.Application.Commands.RestoreAccount;
using Budget.Application.Queries.GetAccount;
using Budget.Application.Queries.GetBudget;
using Budget.Application.Queries.ListAccounts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Core.Pagination;
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

        group.MapGet("/accounts", ListAccounts)
            .WithName("ListBudgetAccounts")
            .WithSummary("Page the envelopes the caller may see");
        group.MapPost("/accounts", CreateAccount)
            .WithName("CreateBudgetAccount")
            .WithSummary("Create a household or personal envelope");
        group.MapGet("/accounts/{id:guid}", GetAccount)
            .WithName("GetBudgetAccount")
            .WithSummary("Get one visible envelope");
        group.MapPut("/accounts/{id:guid}", RenameAccount)
            .WithName("RenameBudgetAccount")
            .WithSummary("Rename an envelope; visibility and owner cannot change");
        group.MapPost("/accounts/{id:guid}/archive", ArchiveAccount)
            .WithName("ArchiveBudgetAccount")
            .WithSummary("Hide an envelope from new-entry choices; history stays");
        group.MapPost("/accounts/{id:guid}/restore", RestoreAccount)
            .WithName("RestoreBudgetAccount")
            .WithSummary("Make an archived envelope available again");

        return app;
    }

    private static async Task<Ok<PagedList<AccountDto>>> ListAccounts(
        ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] bool includeArchived = false)
        => TypedResults.Ok(await dispatcher.SendAsync<ListAccountsQuery, PagedList<AccountDto>>(
            new ListAccountsQuery(Sub(user), page, pageSize, includeArchived), ct));

    private static async Task<Ok<AccountDto>> GetAccount(
        Guid id, ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<GetAccountQuery, AccountDto>(new GetAccountQuery(Sub(user), id), ct));

    private static async Task<Created<AccountDto>> CreateAccount(
        CreateAccountRequest request, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        var account = await dispatcher.SendAsync<CreateAccountCommand, AccountDto>(
            new CreateAccountCommand(Sub(user), request.Name, request.Visibility, request.OwnerPersonId), ct);
        return TypedResults.Created($"/api/budget/accounts/{account.Id}", account);
    }

    private static async Task<Ok<AccountDto>> RenameAccount(
        Guid id, RenameAccountRequest request, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<RenameAccountCommand, AccountDto>(
            new RenameAccountCommand(Sub(user), id, request.ExpectedRevision, request.Name), ct));

    private static async Task<Ok<AccountDto>> ArchiveAccount(
        Guid id, AccountRevisionRequest request, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<ArchiveAccountCommand, AccountDto>(
            new ArchiveAccountCommand(Sub(user), id, request.ExpectedRevision), ct));

    private static async Task<Ok<AccountDto>> RestoreAccount(
        Guid id, AccountRevisionRequest request, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<RestoreAccountCommand, AccountDto>(
            new RestoreAccountCommand(Sub(user), id, request.ExpectedRevision), ct));

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

/// <summary>Body of <c>POST /api/budget/accounts</c>.</summary>
/// <param name="Name">Display name, 1–80 characters.</param>
/// <param name="Visibility"><c>Household</c> or <c>Personal</c>.</param>
/// <param name="OwnerPersonId">Personal envelopes only: the caller (default) or a managed member.</param>
internal sealed record CreateAccountRequest(string Name, string Visibility, Guid? OwnerPersonId);

/// <summary>Body of <c>PUT /api/budget/accounts/{id}</c>.</summary>
/// <param name="Name">New display name.</param>
/// <param name="ExpectedRevision">The revision the client last saw.</param>
internal sealed record RenameAccountRequest(string Name, int ExpectedRevision);

/// <summary>Body of the archive and restore routes.</summary>
/// <param name="ExpectedRevision">The revision the client last saw.</param>
internal sealed record AccountRevisionRequest(int ExpectedRevision);
