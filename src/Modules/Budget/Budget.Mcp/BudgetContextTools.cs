namespace Budget.Mcp;

using System.ComponentModel;
using Budget.Application.Queries.GetAccount;
using Budget.Application.Queries.GetBudgetContext;
using Budget.Application.Queries.GetSummary;
using Budget.Application.Queries.ListLimits;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;

/// <summary>
/// Read-only budget setup and monthly spending for the caller's household, through the same queries as REST.
/// </summary>
[McpServerToolType]
[Authorize(Policy = BudgetMcp.ReadScope)]
internal sealed class BudgetContextTools
{
    [McpServerTool(Name = "get_budget_context", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns the household budget (null until it is set up in the app), the active envelopes the caller can see, the category and funding source codes, and the owners and adults who can pay for or share a household expense. Call it first: other budget tools take ids and codes from it.")]
    public static Task<BudgetContextDto> GetBudgetContext(
        McpToolUser user,
        IQueryDispatcher dispatcher,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<GetBudgetContextQuery, BudgetContextDto>(
            new GetBudgetContextQuery(user.AuthSubject()), ct));

    [McpServerTool(Name = "get_budget_summary", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns one month's spending by envelope and by category, with exact totals as decimal strings. \"Shared\" covers household envelopes (owners and adults only); \"Personal\" covers one person's envelopes. Voided expenses never count.")]
    public static Task<SummaryDto> GetBudgetSummary(
        McpToolUser user,
        IQueryDispatcher dispatcher,
        [Description("Month, YYYY-MM.")] string month,
        [Description("\"Shared\" (default) or \"Personal\".")] string scope = "Shared",
        [Description("Personal scope only: whose envelopes, the ownerPersonId of a personal envelope from get_budget_context; the caller when omitted.")] Guid? ownerPersonId = null,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<GetSummaryQuery, SummaryDto>(
            new GetSummaryQuery(user.AuthSubject(), month, scope, ownerPersonId), ct));

    [McpServerTool(Name = "get_account", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns one envelope the caller can see, archived or not, with the revision that rename_account, archive_account and restore_account need. get_budget_context lists only active envelopes.")]
    public static Task<AccountDto> GetAccount(
        McpToolUser user,
        IQueryDispatcher dispatcher,
        [Description("Envelope id from get_budget_context.")] Guid accountId,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<GetAccountQuery, AccountDto>(
            new GetAccountQuery(user.AuthSubject(), accountId), ct));

    [McpServerTool(Name = "list_limits", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Lists the monthly limits set for one month on envelopes the caller can see, each with the revision that set_limit (when changing) and clear_limit need. Envelopes without a limit are absent; \"0.00\" is a real limit.")]
    public static Task<IReadOnlyList<MonthlyLimitDto>> ListLimits(
        McpToolUser user,
        IQueryDispatcher dispatcher,
        [Description("Month, YYYY-MM.")] string month,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<ListLimitsQuery, IReadOnlyList<MonthlyLimitDto>>(
            new ListLimitsQuery(user.AuthSubject(), month), ct));
}
