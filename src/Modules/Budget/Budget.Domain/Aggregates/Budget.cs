namespace Budget.Domain.Aggregates;

using global::Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// One household's spending workspace. Holds only the household identity and the immutable
/// currency; envelopes and transactions are separate aggregates so a budget never loads its history.
/// </summary>
public sealed class Budget : AggregateRoot<BudgetId>
{
    private Budget() { }

    /// <summary>Creates the household's budget.</summary>
    /// <param name="id">Identifier for the new budget.</param>
    /// <param name="householdId">The owning household; at most one budget exists per household.</param>
    /// <param name="currency">The budget's currency, fixed for its lifetime.</param>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public static Budget Create(BudgetId id, Guid householdId, BudgetCurrency currency, DateTime now)
        => new() { Id = id, HouseholdId = householdId, Currency = currency, CreatedAt = now };

    /// <summary>The household this budget belongs to.</summary>
    public Guid HouseholdId { get; private set; }

    /// <summary>The currency every amount in this budget uses.</summary>
    public BudgetCurrency Currency { get; private set; }

    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
}
