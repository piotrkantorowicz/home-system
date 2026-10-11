namespace Investments.Application.Queries.ListWallets;

using Investments.Application.Common;
using Investments.Application.Persistence;
using Investments.Application.Queries.GetWallet;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

internal sealed class ListWalletsQueryHandler(InvestmentsAccessService access, IInvestmentsReadDbContext db)
    : IQueryHandler<ListWalletsQuery, PagedList<WalletDto>>
{
    private const int MaxPageSize = 100;

    public async Task<PagedList<WalletDto>> HandleAsync(ListWalletsQuery query, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        // Visibility is applied before the count, so hidden wallets never leak through totals.
        var visible = db.Wallets.AsNoTracking()
            .VisibleTo(caller)
            .Where(w => query.IncludeArchived || !w.IsArchived);

        var total = await visible.CountAsync(ct);
        var rows = await visible
            .OrderBy(w => w.Kind).ThenBy(w => w.Name).ThenBy(w => w.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToRows(db.BalanceSnapshots)
            .ToListAsync(ct);

        var rates = await db.LoadLatestRatesAsync(rows.Select(r => r.Currency), ct);
        return new PagedList<WalletDto>([.. rows.Select(r => r.ToDto(caller, rates))], total, page, pageSize);
    }
}
