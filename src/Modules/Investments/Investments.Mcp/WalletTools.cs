namespace Investments.Mcp;

using System.ComponentModel;
using Investments.Application.Queries.GetWallet;
using Investments.Application.Queries.ListSnapshots;
using Investments.Application.Queries.ListWallets;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;

/// <summary>
/// Read-only wallets and their balance history: what the caller may see in the app, through the same
/// queries as REST.
/// </summary>
[McpServerToolType]
[Authorize(Policy = InvestmentsMcp.ReadScope)]
internal sealed class WalletTools
{
    public const int MaxPageSize = 100;

    [McpServerTool(Name = "list_wallets", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Pages the investment wallets the caller can see, by kind then name, each with its latest balance and its PLN value. Amounts are decimal strings.")]
    public static async Task<PagedList<WalletDto>> ListWallets(
        McpToolUser user,
        IQueryDispatcher dispatcher,
        [Description("Also return archived wallets.")] bool includeArchived = false,
        [Description("1-based page number.")] int page = 1,
        [Description("Items per page, 1 to 100.")] int pageSize = 50,
        CancellationToken ct = default)
    {
        EnsurePage(page, pageSize);
        return await McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<ListWalletsQuery, PagedList<WalletDto>>(
            new ListWalletsQuery(user.AuthSubject(), page, pageSize, includeArchived), ct));
    }

    [McpServerTool(Name = "get_wallet", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns one investment wallet the caller can see, with its latest balance and its PLN value.")]
    public static Task<WalletDto> GetWallet(
        McpToolUser user,
        IQueryDispatcher dispatcher,
        [Description("Wallet id from list_wallets.")] Guid walletId,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<GetWalletQuery, WalletDto>(
            new GetWalletQuery(user.AuthSubject(), walletId), ct));

    [McpServerTool(Name = "list_snapshots", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Pages a wallet's recorded balances, newest first, each with the previous balance and its PLN value. Amounts are decimal strings in the wallet's currency.")]
    public static async Task<PagedList<SnapshotDto>> ListSnapshots(
        McpToolUser user,
        IQueryDispatcher dispatcher,
        [Description("Wallet id from list_wallets.")] Guid walletId,
        [Description("1-based page number.")] int page = 1,
        [Description("Items per page, 1 to 100.")] int pageSize = 20,
        CancellationToken ct = default)
    {
        EnsurePage(page, pageSize);
        return await McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<ListSnapshotsQuery, PagedList<SnapshotDto>>(
            new ListSnapshotsQuery(user.AuthSubject(), walletId, page, pageSize), ct));
    }

    private static void EnsurePage(int page, int pageSize)
    {
        if (page < 1)
            throw new McpException("page must be 1 or greater.");
        if (pageSize is < 1 or > MaxPageSize)
            throw new McpException($"pageSize must be between 1 and {MaxPageSize}.");
    }
}
