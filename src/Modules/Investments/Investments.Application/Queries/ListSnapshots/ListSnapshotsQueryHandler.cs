namespace Investments.Application.Queries.ListSnapshots;

using System.Globalization;
using Investments.Application.Common;
using Investments.Application.Persistence;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

internal sealed class ListSnapshotsQueryHandler(InvestmentsAccessService access, IInvestmentsReadDbContext db)
    : IQueryHandler<ListSnapshotsQuery, PagedList<SnapshotDto>>
{
    private const int MaxPageSize = 100;

    public async Task<PagedList<SnapshotDto>> HandleAsync(ListSnapshotsQuery query, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        var walletId = WalletId.From(query.WalletId);
        var currency = await db.Wallets.AsNoTracking().VisibleTo(caller)
            .Where(w => w.Id == walletId)
            .Select(w => (WalletCurrency?)w.Currency)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Wallet", query.WalletId);

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var history = db.BalanceSnapshots.AsNoTracking().Where(s => s.WalletId == walletId);

        var total = await history.CountAsync(ct);
        // One extra row so the last item on the page still knows its previous balance.
        var rows = await history
            .OrderByDescending(s => s.AsOf)
            .Skip((page - 1) * pageSize).Take(pageSize + 1)
            .Select(s => new { s.AsOf, s.Amount, s.Note, s.RecordedByPersonId, s.RecordedByName, s.RecordedAt, s.UpdatedAt })
            .ToListAsync(ct);

        var rates = await db.LoadRatesAsync(rows.Select(r => (currency, r.AsOf)), ct);
        var items = rows.Take(pageSize).Select((r, i) =>
        {
            var pln = rates.ToPln(r.Amount, currency, r.AsOf);
            return new SnapshotDto(
                r.AsOf,
                Format(r.Amount),
                i + 1 < rows.Count ? Format(rows[i + 1].Amount) : null,
                r.Note,
                caller.NameOf(r.RecordedByPersonId) ?? r.RecordedByName,
                r.RecordedAt,
                r.UpdatedAt,
                WalletReadExtensions.Format(pln.Amount),
                FxRateDto.From(pln.Rate),
                pln.RateMissing);
        }).ToList();

        return new PagedList<SnapshotDto>(items, total, page, pageSize);
    }

    private static string Format(decimal amount) => amount.ToString("F2", CultureInfo.InvariantCulture);
}
