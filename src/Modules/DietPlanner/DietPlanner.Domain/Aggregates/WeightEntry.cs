namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed class WeightEntry : AggregateRoot<WeightEntryId>
{
    private const decimal MinWeightKg = 0.1m;
    private const decimal MaxWeightKg = 999m;

    private WeightEntry() { }

    public static WeightEntry Create(WeightEntryId id, string userId, DateOnly date, decimal weightKg)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        EnsureValidWeight(weightKg);
        EnsureNotFutureDate(date);

        return new WeightEntry
        {
            Id = id,
            UserId = userId,
            Date = date,
            WeightKg = weightKg,
            CreatedAt = DateTime.UtcNow
        };
    }

    public string UserId { get; private set; } = default!;
    public DateOnly Date { get; private set; }
    public decimal WeightKg { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public void ChangeWeight(decimal newWeightKg)
    {
        EnsureValidWeight(newWeightKg);
        WeightKg = newWeightKg;
        UpdatedAt = DateTime.UtcNow;
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
