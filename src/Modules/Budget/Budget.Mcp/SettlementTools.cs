namespace Budget.Mcp;

using System.ComponentModel;
using Budget.Application.Queries.GetSettlement;
using Budget.Application.Queries.ListRepayments;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;

/// <summary>
/// Read-only outstanding balances and recorded repayments, owners and adults only, through the same
/// queries as REST.
/// </summary>
[McpServerToolType]
[Authorize(Policy = BudgetMcp.ReadScope)]
internal sealed class SettlementTools
{
    public const int MaxPageSize = 100;

    [McpServerTool(Name = "get_settlement", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns who owes whom across all recorded shared expenses and repayments: each adult's net balance and the suggested payments that would settle everyone. Owners and adults only. Amounts are decimal strings.")]
    public static Task<SettlementDto> GetSettlement(
        McpToolUser user,
        IQueryDispatcher dispatcher,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<GetSettlementQuery, SettlementDto>(
            new GetSettlementQuery(user.AuthSubject()), ct));

    [McpServerTool(Name = "list_repayments", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Pages recorded repayments, newest payment date first, voided ones included and marked. Each carries the revision that void_repayment needs. Owners and adults only.")]
    public static async Task<PagedList<RepaymentDto>> ListRepayments(
        McpToolUser user,
        IQueryDispatcher dispatcher,
        [Description("1-based page number.")] int page = 1,
        [Description("Items per page, 1 to 100.")] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1)
            throw new McpException("page must be 1 or greater.");
        if (pageSize is < 1 or > MaxPageSize)
            throw new McpException($"pageSize must be between 1 and {MaxPageSize}.");

        return await McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<ListRepaymentsQuery, PagedList<RepaymentDto>>(
            new ListRepaymentsQuery(user.AuthSubject(), page, pageSize), ct));
    }
}
