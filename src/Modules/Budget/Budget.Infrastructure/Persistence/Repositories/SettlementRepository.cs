namespace Budget.Infrastructure.Persistence.Repositories;

using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class SettlementRepository(BudgetDbContext dbContext) : ISettlementRepository
{
    public async Task AddAsync(Settlement settlement, CancellationToken ct)
        => await dbContext.Settlements.AddAsync(settlement, ct);

    public async Task LockRequestAsync(BudgetId budgetId, Guid actorPersonId, Guid clientRequestId, CancellationToken ct)
        => await LockAsync($"settlement-request:{budgetId.Value}:{actorPersonId}:{clientRequestId}", ct);

    public async Task LockSettlementAsync(Guid settlementId, CancellationToken ct)
        => await LockAsync($"settlement:{settlementId}", ct);

    public async Task<Settlement?> FindByRequestAsync(BudgetId budgetId, Guid actorPersonId, Guid clientRequestId, CancellationToken ct)
        => await dbContext.Settlements.AsNoTracking().FirstOrDefaultAsync(
            s => s.BudgetId == budgetId && s.AddedByPersonId == actorPersonId && s.ClientRequestId == clientRequestId, ct);

    public async Task<Settlement?> GetAsync(SettlementId id, BudgetId budgetId, CancellationToken ct)
        => await dbContext.Settlements.FirstOrDefaultAsync(s => s.Id == id && s.BudgetId == budgetId, ct);

    public async Task<string?> FindLedgerNameAsync(BudgetId budgetId, Guid personId, CancellationToken ct)
    {
        var sharedAccountIds = dbContext.BudgetAccounts
            .Where(a => a.BudgetId == budgetId && a.Visibility == AccountVisibility.Household)
            .Select(a => a.Id);
        var ledger = dbContext.Expenses.AsNoTracking()
            .Where(e => sharedAccountIds.Contains(e.BudgetAccountId) && !e.IsVoided && e.FundingSource == FundingSource.Individual);

        var paid = await ledger.Where(e => e.PaidByPersonId == personId)
            .Select(e => new { Name = e.PaidByDisplayName!, e.CreatedAt, e.Id })
            .ToListAsync(ct);
        var shared = await ledger.SelectMany(e => e.Shares.Where(s => s.PersonId == personId).Select(s => new { Name = s.PersonDisplayName, e.CreatedAt, e.Id }))
            .ToListAsync(ct);
        var sent = await dbContext.Settlements.AsNoTracking()
            .Where(s => s.BudgetId == budgetId && !s.IsVoided && s.FromPersonId == personId)
            .Select(s => new { Name = s.FromDisplayName, s.CreatedAt, Id = s.Id.Value })
            .ToListAsync(ct);
        var received = await dbContext.Settlements.AsNoTracking()
            .Where(s => s.BudgetId == budgetId && !s.IsVoided && s.ToPersonId == personId)
            .Select(s => new { Name = s.ToDisplayName, s.CreatedAt, Id = s.Id.Value })
            .ToListAsync(ct);

        return paid.Select(x => (x.Name, x.CreatedAt, Id: x.Id.Value))
            .Concat(shared.Select(x => (x.Name, x.CreatedAt, Id: x.Id.Value)))
            .Concat(sent.Select(x => (x.Name, x.CreatedAt, x.Id)))
            .Concat(received.Select(x => (x.Name, x.CreatedAt, x.Id)))
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Select(x => x.Name)
            .FirstOrDefault();
    }

    private async Task LockAsync(string key, CancellationToken ct)
        => await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", ct);
}
