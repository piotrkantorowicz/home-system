namespace Investments.Application.Queries.GetWallet;

using Investments.Application.Common;
using Investments.Application.Persistence;
using Shared.Abstractions.Cqrs;

internal sealed class GetWalletQueryHandler(InvestmentsAccessService access, IInvestmentsReadDbContext db)
    : IQueryHandler<GetWalletQuery, WalletDto>
{
    public async Task<WalletDto> HandleAsync(GetWalletQuery query, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        return await db.ReadWalletAsync(caller, query.Id, ct);
    }
}
