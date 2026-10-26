namespace Budget.Domain.Abstractions;

using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;

/// <summary>
/// Write-side access to the <c>CategoryRule</c> aggregate. Callers hold the import-scope lock
/// (<see cref="IExpenseRepository.LockImportScopeAsync"/>), so two imports never insert the same rule.
/// </summary>
public interface ICategoryRuleRepository
{
    /// <summary>Loads the scope's rules for these merchants for modification.</summary>
    /// <param name="budgetId">The budget.</param>
    /// <param name="importScope">The import scope.</param>
    /// <param name="merchantKeys">Normalised merchant keys.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<CategoryRule>> GetAsync(
        BudgetId budgetId, string importScope, IReadOnlyCollection<string> merchantKeys, CancellationToken ct);

    /// <summary>Stages a new rule for the next commit.</summary>
    /// <param name="rule">The rule to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(CategoryRule rule, CancellationToken ct);
}
