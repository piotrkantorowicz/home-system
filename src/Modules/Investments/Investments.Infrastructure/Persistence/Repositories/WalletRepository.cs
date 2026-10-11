namespace Investments.Infrastructure.Persistence.Repositories;

using Investments.Domain.Abstractions;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class WalletRepository(InvestmentsDbContext dbContext) : IWalletRepository
{
    public async Task AddAsync(Wallet wallet, CancellationToken ct)
        => await dbContext.Wallets.AddAsync(wallet, ct);

    public async Task<Wallet?> GetAsync(WalletId id, Guid householdId, CancellationToken ct)
        => await dbContext.Wallets.FirstOrDefaultAsync(w => w.Id == id && w.HouseholdId == householdId, ct);
}
