namespace Investments.Application.Queries.ListContributions;

using Investments.Application.Common;
using Investments.Application.Persistence;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

internal sealed class ListContributionsQueryHandler(InvestmentsAccessService access, IInvestmentsReadDbContext db)
    : IQueryHandler<ListContributionsQuery, PagedList<ContributionDto>>
{
    private const int MaxPageSize = 100;

    public async Task<PagedList<ContributionDto>> HandleAsync(ListContributionsQuery query, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        var walletId = WalletId.From(query.WalletId);
        if (!await db.Wallets.AsNoTracking().VisibleTo(caller).AnyAsync(w => w.Id == walletId, ct))
            throw new NotFoundException("Wallet", query.WalletId);

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var contributions = db.Contributions.AsNoTracking().Where(c => c.WalletId == walletId);

        var total = await contributions.CountAsync(ct);
        var rows = await contributions
            .OrderByDescending(c => c.Date).ThenByDescending(c => c.RecordedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new { c.Id, c.Date, c.Direction, c.Amount, c.Note, c.RecordedByPersonId, c.RecordedByName, c.RecordedAt })
            .ToListAsync(ct);

        var items = rows.Select(r => new ContributionDto(
            r.Id.Value, r.Date, r.Direction.ToString(), WalletReadExtensions.Format(r.Amount), r.Note,
            caller.NameOf(r.RecordedByPersonId) ?? r.RecordedByName, r.RecordedAt)).ToList();

        return new PagedList<ContributionDto>(items, total, page, pageSize);
    }
}
