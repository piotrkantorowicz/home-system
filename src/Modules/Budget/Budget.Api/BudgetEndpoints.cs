namespace Budget.Api;

using System.Security.Claims;
using System.Text.Json.Serialization;
using Budget.Application.Commands.ArchiveAccount;
using Budget.Application.Commands.ClearMonthlyLimit;
using Budget.Application.Commands.CreateAccount;
using Budget.Application.Commands.CreateExpense;
using Budget.Application.Commands.InitializeBudget;
using Budget.Application.Commands.RenameAccount;
using Budget.Application.Commands.RestoreAccount;
using Budget.Application.Commands.SetMonthlyLimit;
using Budget.Application.Commands.UpdateExpense;
using Budget.Application.Commands.VoidExpense;
using Budget.Application.Queries.GetAccount;
using Budget.Application.Queries.GetBudget;
using Budget.Application.Queries.GetExpense;
using Budget.Application.Queries.GetSettlement;
using Budget.Application.Queries.GetSummary;
using Budget.Application.Queries.ListAccounts;
using Budget.Application.Queries.ListExpenses;
using Budget.Application.Queries.ListLimits;
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

        group.MapGet("/expenses", ListExpenses)
            .WithName("ListBudgetExpenses")
            .WithSummary("Page visible expenses; filter by envelope, category, exact amount and date range");
        group.MapGet("/expenses/{id:guid}", GetExpense)
            .WithName("GetBudgetExpense")
            .WithSummary("Get one visible expense with its stored shares");
        group.MapPost("/expenses", CreateExpense)
            .WithName("CreateBudgetExpense")
            .WithSummary("Record an expense; a retry with the same clientRequestId returns the original result");
        group.MapPut("/expenses/{id:guid}", UpdateExpense)
            .WithName("UpdateBudgetExpense")
            .WithSummary("Correct an expense in place with a reason and expected revision; appends a history revision");
        group.MapPost("/expenses/{id:guid}/void", VoidExpense)
            .WithName("VoidBudgetExpense")
            .WithSummary("Void an expense with a reason and expected revision; repeating adds no revision");
        group.MapGet("/summary", GetSummary)
            .WithName("GetBudgetSummary")
            .WithSummary("Monthly spending by envelope and category for the shared or a personal scope");
        group.MapGet("/limits", ListLimits)
            .WithName("ListBudgetLimits")
            .WithSummary("Monthly limits on the envelopes the caller can see");
        group.MapPut("/accounts/{id:guid}/limits/{month}", SetLimit)
            .WithName("SetBudgetLimit")
            .WithSummary("Set or change an envelope's limit for a month (expected revision when changing)");
        group.MapDelete("/accounts/{id:guid}/limits/{month}", ClearLimit)
            .WithName("ClearBudgetLimit")
            .WithSummary("Remove an envelope's limit for a month");
        group.MapGet("/settlement", GetSettlement)
            .WithName("GetBudgetSettlement")
            .WithSummary("Outstanding balances and suggested payments across all recorded entries (Owner/Adult only)");

        return app;
    }

    private static async Task<Ok<PagedList<ExpenseDto>>> ListExpenses(
        ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? accountId = null,
        [FromQuery] string? category = null, [FromQuery] string? amount = null,
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null, [FromQuery] Guid? excludeId = null, [FromQuery] bool includeVoided = false)
        => TypedResults.Ok(await dispatcher.SendAsync<ListExpensesQuery, PagedList<ExpenseDto>>(
            new ListExpensesQuery(Sub(user), page, pageSize, accountId, category, amount, from, to, excludeId, includeVoided), ct));

    private static async Task<Ok<ExpenseDto>> GetExpense(
        Guid id, ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<GetExpenseQuery, ExpenseDto>(new GetExpenseQuery(Sub(user), id), ct));

    private static async Task<Ok<SettlementDto>> GetSettlement(
        ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<GetSettlementQuery, SettlementDto>(new GetSettlementQuery(Sub(user)), ct));

    private static async Task<Ok<SummaryDto>> GetSummary(
        ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct,
        [FromQuery] string month = "", [FromQuery] string scope = "", [FromQuery] Guid? ownerPersonId = null)
        => TypedResults.Ok(await dispatcher.SendAsync<GetSummaryQuery, SummaryDto>(
            new GetSummaryQuery(Sub(user), month, scope, ownerPersonId), ct));

    private static async Task<Ok<IReadOnlyList<MonthlyLimitDto>>> ListLimits(
        ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct, [FromQuery] string month = "")
        => TypedResults.Ok(await dispatcher.SendAsync<ListLimitsQuery, IReadOnlyList<MonthlyLimitDto>>(
            new ListLimitsQuery(Sub(user), month), ct));

    private static async Task<Ok<MonthlyLimitDto>> SetLimit(
        Guid id, string month, SetLimitRequest request, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<SetMonthlyLimitCommand, MonthlyLimitDto>(
            new SetMonthlyLimitCommand(Sub(user), id, month, request.Amount, request.ExpectedRevision), ct));

    private static async Task<NoContent> ClearLimit(
        Guid id, string month, [FromQuery] int expectedRevision, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        await dispatcher.SendAsync(new ClearMonthlyLimitCommand(Sub(user), id, month, expectedRevision), ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<ExpenseMutationResult>> UpdateExpense(
        Guid id, UpdateExpenseRequest request, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<UpdateExpenseCommand, ExpenseMutationResult>(
            new UpdateExpenseCommand(
                Sub(user), id, request.ClientRequestId, request.ExpectedRevision, request.Reason, request.Amount, request.OccurredOn,
                request.Category, request.FundingSource, request.PaidByPersonId, request.ParticipantIds), ct));

    private static async Task<Ok<ExpenseMutationResult>> VoidExpense(
        Guid id, VoidExpenseRequest request, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<VoidExpenseCommand, ExpenseMutationResult>(
            new VoidExpenseCommand(Sub(user), id, request.ClientRequestId, request.ExpectedRevision, request.Reason), ct));

    private static async Task<Results<Created<ExpenseMutationResult>, Ok<ExpenseMutationResult>>> CreateExpense(
        CreateExpenseRequest request, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        var result = await dispatcher.SendAsync<CreateExpenseCommand, ExpenseMutationResult>(
            new CreateExpenseCommand(
                Sub(user), request.ClientRequestId, request.AccountId, request.Amount, request.OccurredOn, request.Category,
                request.FundingSource, request.PaidByPersonId, request.ParticipantIds), ct);

        return result.Created
            ? TypedResults.Created($"/api/budget/expenses/{result.ExpenseId}", result)
            : TypedResults.Ok(result);
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

/// <summary>
/// Body of <c>POST /api/budget/expenses</c>. Unknown members — an actor, a share amount — are
/// rejected with 400: the server decides who acts and computes the equal shares itself.
/// </summary>
/// <param name="ClientRequestId">Idempotency key; reuse it when retrying the same submission.</param>
/// <param name="AccountId">The envelope.</param>
/// <param name="Amount">Positive decimal string such as <c>"123.45"</c>, at most two decimals.</param>
/// <param name="OccurredOn">Purchase date, <c>YYYY-MM-DD</c>.</param>
/// <param name="Category">Category code.</param>
/// <param name="FundingSource"><c>Individual</c> (default) or <c>HouseholdFunds</c>.</param>
/// <param name="PaidByPersonId">Payer of an individually funded shared expense.</param>
/// <param name="ParticipantIds">Adults sharing the cost equally.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record CreateExpenseRequest(
    Guid ClientRequestId,
    Guid AccountId,
    string Amount,
    DateOnly OccurredOn,
    string Category,
    string? FundingSource,
    Guid? PaidByPersonId,
    IReadOnlyList<Guid>? ParticipantIds);

/// <summary>Body of <c>PUT /api/budget/expenses/{id}</c>; the envelope and recorder cannot be changed.</summary>
/// <param name="ClientRequestId">Idempotency key; reuse it when retrying the same submission.</param>
/// <param name="ExpectedRevision">The revision the client last saw.</param>
/// <param name="Reason">Short reason, 1–200 characters.</param>
/// <param name="Amount">Positive decimal string such as <c>"123.45"</c>.</param>
/// <param name="OccurredOn">Purchase date, <c>YYYY-MM-DD</c>.</param>
/// <param name="Category">Category code.</param>
/// <param name="FundingSource"><c>Individual</c> (default) or <c>HouseholdFunds</c>.</param>
/// <param name="PaidByPersonId">Payer of an individually funded shared expense.</param>
/// <param name="ParticipantIds">Adults sharing the cost equally.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record UpdateExpenseRequest(
    Guid ClientRequestId,
    int ExpectedRevision,
    string Reason,
    string Amount,
    DateOnly OccurredOn,
    string Category,
    string? FundingSource,
    Guid? PaidByPersonId,
    IReadOnlyList<Guid>? ParticipantIds);

/// <summary>Body of <c>POST /api/budget/expenses/{id}/void</c>.</summary>
/// <param name="ClientRequestId">Idempotency key; reuse it when retrying.</param>
/// <param name="ExpectedRevision">The revision the client last saw.</param>
/// <param name="Reason">Short reason, 1–200 characters.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record VoidExpenseRequest(Guid ClientRequestId, int ExpectedRevision, string Reason);

/// <summary>Body of <c>PUT /api/budget/accounts/{id}/limits/{month}</c>.</summary>
/// <param name="Amount">Decimal string, at most two decimals; <c>"0.00"</c> is allowed and differs from no limit.</param>
/// <param name="ExpectedRevision">Revision of the existing limit when changing it; omit when setting for the first time.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record SetLimitRequest(string Amount, int? ExpectedRevision);
