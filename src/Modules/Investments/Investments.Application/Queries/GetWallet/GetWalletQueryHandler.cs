namespace Investments.Application.Queries.GetWallet;

using Investments.Application.Common;
using Investments.Application.Persistence;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class GetWalletQueryHandler(InvestmentsAccessService access, IInvestmentsReadDbContext db)
    : IQueryHandler<GetWalletQuery, WalletDto>
{
    public async Task<WalletDto> HandleAsync(GetWalletQuery query, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        var id = WalletId.From(query.Id);

        var row = await db.Wallets.AsNoTracking()
            .Where(w => w.Id == id)
            .VisibleTo(caller)
            .Select(WalletReadExtensions.ToRow)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Wallet", query.Id);

        return row.ToDto(caller);
    }
}
