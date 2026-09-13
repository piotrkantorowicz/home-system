namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.Events;
using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// One weigh-in: a user's weight on a calendar day (at most one per day, enforced by the command).
/// Weight must be between 0.1 and 999 kg and the day cannot be in the future. Creating or changing
/// an entry raises <see cref="WeightEntryAddedDomainEvent"/> so the profile's current weight and the
/// goal milestone are re-evaluated.
/// </summary>
public sealed class WeightEntry : AggregateRoot<WeightEntryId>
{
    private const decimal MinWeightKg = 0.1m;
    private const decimal MaxWeightKg = 999m;

    private WeightEntry() { }

    /// <summary>Records a weigh-in and raises <see cref="WeightEntryAddedDomainEvent"/>.</summary>
    /// <param name="id">Identifier for the new entry.</param>
    /// <param name="userId">Auth subject of the owner; required.</param>
    /// <param name="date">The day of the weigh-in; today or earlier (UTC).</param>
    /// <param name="weightKg">Weight in kilograms, 0.1–999.</param>
    /// <exception cref="ArgumentException"><paramref name="userId"/> is blank.</exception>
    /// <exception cref="DietPlannerDomainException">The weight is out of range or the date is in the future.</exception>
    public static WeightEntry Create(WeightEntryId id, string userId, DateOnly date, decimal weightKg)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        EnsureValidWeight(weightKg);
        EnsureNotFutureDate(date);

        var entry = new WeightEntry
        {
            Id = id,
            UserId = userId,
            Date = date,
            WeightKg = weightKg,
            CreatedAt = DateTime.UtcNow,
        };

        entry.RaiseDomainEvent(new WeightEntryAddedDomainEvent(userId, weightKg, date));
        return entry;
    }

    /// <summary>Auth subject of the owner.</summary>
    public string UserId { get; private set; } = default!;
    /// <summary>The day of the weigh-in.</summary>
    public DateOnly Date { get; private set; }
    /// <summary>Weight in kilograms.</summary>
    public decimal WeightKg { get; private set; }
    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>Time of the last <see cref="ChangeWeight"/>, UTC; <see langword="null"/> if never changed.</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Corrects the recorded weight and raises <see cref="WeightEntryAddedDomainEvent"/> again.</summary>
    /// <param name="newWeightKg">Corrected weight in kilograms, 0.1–999.</param>
    /// <exception cref="DietPlannerDomainException">The weight is out of range.</exception>
    public void ChangeWeight(decimal newWeightKg)
    {
        EnsureValidWeight(newWeightKg);
        WeightKg = newWeightKg;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new WeightEntryAddedDomainEvent(UserId, newWeightKg, Date));
    }

    private static void EnsureValidWeight(decimal weightKg)
    {
        if (weightKg < MinWeightKg || weightKg > MaxWeightKg)
            throw new DietPlannerDomainException(
                $"Weight must be between {MinWeightKg} and {MaxWeightKg} kg.");
    }

    private static void EnsureNotFutureDate(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (date > today)
            throw new DietPlannerDomainException("Weight entry date cannot be in the future.");
    }
}
