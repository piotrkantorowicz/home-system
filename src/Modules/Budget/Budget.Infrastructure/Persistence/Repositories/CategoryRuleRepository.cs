namespace Budget.Infrastructure.Persistence.Repositories;

using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class CategoryRuleRepository(BudgetDbContext dbContext) : ICategoryRuleRepository
{
    public async Task<IReadOnlyList<CategoryRule>> GetAsync(
        BudgetId budgetId, string importScope, IReadOnlyCollection<string> merchantKeys, CancellationToken ct)
        => await dbContext.CategoryRules
            .Where(r => r.BudgetId == budgetId && r.ImportScope == importScope && merchantKeys.Contains(r.MerchantKey))
            .ToListAsync(ct);

    public async Task AddAsync(CategoryRule rule, CancellationToken ct)
        => await dbContext.CategoryRules.AddAsync(rule, ct);
}
