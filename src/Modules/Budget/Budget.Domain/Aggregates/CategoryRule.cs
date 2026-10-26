namespace Budget.Domain.Aggregates;

using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// The category last chosen for a merchant in one import scope (<c>bank-import.md</c> §5.1). Each
/// confirmed import relearns it, so the latest choice wins; the preview suggests it on exact match.
/// </summary>
public sealed class CategoryRule : AggregateRoot<CategoryRuleId>
{
    /// <summary>Maximum length of a merchant key.</summary>
    public const int MaxMerchantKeyLength = 100;

    private CategoryRule() { }

    /// <summary>Learns a rule for a merchant seen for the first time in this scope.</summary>
    /// <param name="id">Identifier for the new rule.</param>
    /// <param name="budgetId">The budget.</param>
    /// <param name="importScope">Same scope as <see cref="Expense.ImportScope"/>, so a private rule never reaches another adult.</param>
    /// <param name="merchantKey">Normalised merchant, 1 to <see cref="MaxMerchantKeyLength"/> characters.</param>
    /// <param name="category">The chosen category.</param>
    /// <param name="now">Current time, UTC.</param>
    /// <exception cref="BudgetDomainException">The merchant key is blank or too long.</exception>
    public static CategoryRule Create(
        CategoryRuleId id, BudgetId budgetId, string importScope, string merchantKey, ExpenseCategory category, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(merchantKey) || merchantKey.Length > MaxMerchantKeyLength)
            throw new BudgetDomainException($"A merchant key is 1 to {MaxMerchantKeyLength} characters.");

        return new CategoryRule
        {
            Id = id,
            BudgetId = budgetId,
            ImportScope = importScope,
            MerchantKey = merchantKey,
            Category = category,
            UpdatedAt = now,
        };
    }

    /// <summary>Replaces the category with the user's latest choice.</summary>
    /// <param name="category">The chosen category.</param>
    /// <param name="now">Current time, UTC.</param>
    public void Learn(ExpenseCategory category, DateTime now)
    {
        Category = category;
        UpdatedAt = now;
    }

    /// <summary>The budget.</summary>
    public BudgetId BudgetId { get; private set; } = default!;

    /// <summary><see cref="Expense.HouseholdImportScope"/> or the owner's person ID.</summary>
    public string ImportScope { get; private set; } = default!;

    /// <summary>Normalised counterparty, or the title up to its first <c>/</c>.</summary>
    public string MerchantKey { get; private set; } = default!;

    /// <summary>The category to suggest.</summary>
    public ExpenseCategory Category { get; private set; }

    /// <summary>Last time the rule was learned, UTC.</summary>
    public DateTime UpdatedAt { get; private set; }
}
