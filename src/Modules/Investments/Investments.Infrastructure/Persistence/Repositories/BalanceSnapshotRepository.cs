namespace Investments.Infrastructure.Persistence.Repositories;

using Investments.Domain.Abstractions;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class BalanceSnapshotRepository(InvestmentsDbContext dbContext) : IBalanceSnapshotRepository
{
    public async Task AddAsync(BalanceSnapshot snapshot, CancellationToken ct)
        => await dbContext.BalanceSnapshots.AddAsync(snapshot, ct);

    public async Task<BalanceSnapshot?> GetAsync(WalletId walletId, DateOnly asOf, CancellationToken ct)
        => await dbContext.BalanceSnapshots.FirstOrDefaultAsync(s => s.WalletId == walletId && s.AsOf == asOf, ct);

    public void Remove(BalanceSnapshot snapshot) => dbContext.BalanceSnapshots.Remove(snapshot);
}
