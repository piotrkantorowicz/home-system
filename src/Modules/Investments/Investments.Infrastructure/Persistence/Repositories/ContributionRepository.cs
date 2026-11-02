namespace Investments.Infrastructure.Persistence.Repositories;

using Investments.Domain.Abstractions;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class ContributionRepository(InvestmentsDbContext dbContext) : IContributionRepository
{
    public async Task AddAsync(Contribution contribution, CancellationToken ct)
        => await dbContext.Contributions.AddAsync(contribution, ct);

    public async Task<Contribution?> GetAsync(WalletId walletId, ContributionId id, CancellationToken ct)
        => await dbContext.Contributions.FirstOrDefaultAsync(c => c.Id == id && c.WalletId == walletId, ct);

    public void Remove(Contribution contribution) => dbContext.Contributions.Remove(contribution);
}
