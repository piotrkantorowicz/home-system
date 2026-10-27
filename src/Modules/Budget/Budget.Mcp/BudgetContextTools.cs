namespace Budget.Mcp;

using System.ComponentModel;
using Budget.Application.Queries.GetBudgetContext;
using Budget.Application.Queries.GetSummary;
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
}
