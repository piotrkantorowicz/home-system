namespace Budget.Domain.Aggregates;

using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>A named spending bucket ("envelope") with household or personal visibility.</summary>
public sealed class BudgetAccount : AggregateRoot<BudgetAccountId>
{
    /// <summary>Maximum length of an envelope name.</summary>
    public const int MaxNameLength = 80;

    private BudgetAccount() { }

    /// <summary>Creates an envelope.</summary>
    /// <param name="id">Identifier for the new envelope.</param>
    /// <param name="budgetId">The budget it belongs to.</param>
    /// <param name="name">Display name; trimmed, 1–80 characters.</param>
    /// <param name="visibility">Who can see it.</param>
    /// <param name="ownerPersonId">The owner of a <see cref="AccountVisibility.Personal"/> envelope; must be <see langword="null"/> for a household one.</param>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    /// <exception cref="BudgetDomainException">The name is blank/too long, or the owner does not match the visibility.</exception>
    public static BudgetAccount Create(
        BudgetAccountId id,
        BudgetId budgetId,
        string name,
        AccountVisibility visibility,
        Guid? ownerPersonId,
        DateTime now)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxNameLength)
            throw new BudgetDomainException($"An envelope name must be 1–{MaxNameLength} characters.");

        if ((visibility == AccountVisibility.Personal) != ownerPersonId.HasValue)
            throw new BudgetDomainException("A personal envelope needs an owner; a household envelope has none.");

        return new BudgetAccount
        {
            Id = id,
            BudgetId = budgetId,
            Name = trimmed,
            Visibility = visibility,
            OwnerPersonId = ownerPersonId,
            CreatedAt = now,
        };
    }

    /// <summary>The budget this envelope belongs to.</summary>
    public BudgetId BudgetId { get; private set; } = default!;

    /// <summary>Display name.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Who can see the envelope.</summary>
    public AccountVisibility Visibility { get; private set; }

    /// <summary>The owner of a personal envelope; <see langword="null"/> for a household one.</summary>
    public Guid? OwnerPersonId { get; private set; }

    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
}
