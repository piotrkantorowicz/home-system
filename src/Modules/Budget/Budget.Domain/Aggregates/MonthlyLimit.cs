namespace Budget.Domain.Aggregates;

using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// An optional spending target for one envelope and calendar month. A missing limit means "no
/// limit"; <c>0.00</c> is a real limit that any spending exceeds. Exceeding it only warns.
/// </summary>
public sealed class MonthlyLimit : AggregateRoot<MonthlyLimitId>
{
    private MonthlyLimit() { }

    /// <summary>Sets a new limit.</summary>
    /// <param name="id">Identifier for the new limit.</param>
    /// <param name="account">The envelope; must not be archived.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="amount">The target; zero is allowed.</param>
    /// <param name="now">Current time, UTC.</param>
    /// <exception cref="BudgetDomainException">The envelope is archived.</exception>
    public static MonthlyLimit Create(MonthlyLimitId id, BudgetAccount account, BudgetMonth month, Money amount, DateTime now)
    {
        EnsureActive(account);
        return new MonthlyLimit
        {
            Id = id,
            BudgetId = account.BudgetId,
            BudgetAccountId = account.Id,
            MonthStart = month.Start,
            Amount = amount.Amount,
            Revision = 1,
            UpdatedAt = now,
        };
    }

    /// <summary>Changes the target.</summary>
    /// <param name="account">The envelope; must not be archived.</param>
    /// <param name="amount">The new target; zero is allowed.</param>
    /// <param name="expectedRevision">The revision the caller last saw.</param>
    /// <param name="now">Current time, UTC.</param>
    /// <exception cref="ConflictException">The limit changed since <paramref name="expectedRevision"/>.</exception>
    /// <exception cref="BudgetDomainException">The envelope is archived.</exception>
    public void Change(BudgetAccount account, Money amount, int expectedRevision, DateTime now)
    {
        EnsureActive(account);
        EnsureRevision(expectedRevision);
        Amount = amount.Amount;
        Revision++;
        UpdatedAt = now;
    }

    /// <summary>Checks the caller's revision before the limit is removed.</summary>
    /// <param name="expectedRevision">The revision the caller last saw.</param>
    /// <exception cref="ConflictException">The limit changed since <paramref name="expectedRevision"/>.</exception>
    public void EnsureRevision(int expectedRevision)
    {
        if (Revision != expectedRevision)
            throw new ConflictException("This limit was changed by someone else. Reload and try again.");
    }

    private static void EnsureActive(BudgetAccount account)
    {
        if (account.IsArchived)
            throw new BudgetDomainException("An archived envelope cannot get a new limit.");
    }

    /// <summary>The budget (repeated for local consistency).</summary>
    public BudgetId BudgetId { get; private set; } = default!;

    /// <summary>The envelope.</summary>
    public BudgetAccountId BudgetAccountId { get; private set; } = default!;

    /// <summary>First day of the month the limit applies to.</summary>
    public DateOnly MonthStart { get; private set; }

    /// <summary>The target, two decimals; may be zero.</summary>
    public decimal Amount { get; private set; }

    /// <summary>Increases by one on every change.</summary>
    public int Revision { get; private set; }

    /// <summary>Last change, UTC.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Concurrency token mapped to PostgreSQL's <c>xmin</c>; never set by domain logic.</summary>
    public uint Version { get; private set; }
}
