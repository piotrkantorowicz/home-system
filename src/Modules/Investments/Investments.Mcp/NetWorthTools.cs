namespace Investments.Mcp;

using System.ComponentModel;
using Investments.Application.Queries.GetNetWorth;
using Investments.Application.Queries.GetNetWorthHistory;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Read-only net worth in PLN for the caller's household or the caller alone, through the same queries as REST.
/// </summary>
[McpServerToolType]
[Authorize(Policy = InvestmentsMcp.ReadScope)]
internal sealed class NetWorthTools
{
    [McpServerTool(Name = "get_net_worth", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns net worth in PLN on a date, broken down by wallet kind, owner and wallet, with the NBP rates used. Incomplete is true when a foreign balance had no rate and is left out of the total.")]
    public static Task<NetWorthDto> GetNetWorth(
        RequestContext<CallToolRequestParams> context,
        IQueryDispatcher dispatcher,
        [Description("Reference date, YYYY-MM-DD; today when omitted; not in the future.")] DateOnly? asOf = null,
        [Description("\"household\" (default) or \"mine\".")] string? scope = null,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<GetNetWorthQuery, NetWorthDto>(
            new GetNetWorthQuery(McpCaller.AuthSubject(context), asOf, scope), ct));

    [McpServerTool(Name = "get_net_worth_history", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns month-end net worth in PLN over a date range (default the last 12 months, at most 10 years), oldest first, plus the last date itself.")]
    public static Task<NetWorthHistoryDto> GetNetWorthHistory(
        RequestContext<CallToolRequestParams> context,
        IQueryDispatcher dispatcher,
        [Description("First date, YYYY-MM-DD; twelve months before 'to' when omitted.")] DateOnly? from = null,
        [Description("Last date, YYYY-MM-DD; today when omitted; not in the future.")] DateOnly? to = null,
        [Description("\"household\" (default) or \"mine\".")] string? scope = null,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<GetNetWorthHistoryQuery, NetWorthHistoryDto>(
            new GetNetWorthHistoryQuery(McpCaller.AuthSubject(context), from, to, scope), ct));
}
