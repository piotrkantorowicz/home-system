namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// One logged drink: an amount of water on a calendar day. Immutable after creation — a mistake is
/// fixed by deleting the entry. Daily totals are compared against the user's
/// <see cref="HydrationConfig"/> target.
/// </summary>
public sealed class WaterIntake : AggregateRoot<WaterIntakeId>
{
    private WaterIntake() { }

    /// <summary>Logs a drink at the current UTC time.</summary>
    /// <param name="id">Identifier for the new entry.</param>
    /// <param name="personId">Person identifier of the owner; required.</param>
    /// <param name="date">The calendar day the drink counts towards.</param>
    /// <param name="amountMl">Volume in millilitres; must be positive.</param>
    /// <param name="note">Optional free-text note.</param>
    /// <exception cref="ArgumentException"><paramref name="personId"/> is empty.</exception>
    /// <exception cref="DietPlannerDomainException"><paramref name="amountMl"/> is not positive.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public static WaterIntake Create(
        WaterIntakeId id,
        Guid personId,
        DateOnly date,
        int amountMl,
        string? note,
        DateTime now)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        if (amountMl <= 0)
            throw new DietPlannerDomainException("Water intake amount must be greater than zero.");

        return new WaterIntake
        {
            Id = id,
            PersonId = personId,
            Date = date,
            AmountMl = amountMl,
            Timestamp = now,
            Note = note
        };
    }

    /// <summary>Person identifier of the owner.</summary>
    public Guid PersonId { get; private set; }
    /// <summary>The calendar day the drink counts towards.</summary>
    public DateOnly Date { get; private set; }
    /// <summary>Volume in millilitres; always positive.</summary>
    public int AmountMl { get; private set; }
    /// <summary>When the drink was logged, UTC.</summary>
    public DateTime Timestamp { get; private set; }
    /// <summary>Optional free-text note.</summary>
    public string? Note { get; private set; }
}
