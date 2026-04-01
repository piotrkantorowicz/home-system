namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.Exceptions;
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Domain;

public sealed class WaterIntake : AggregateRoot<WaterIntakeId>
{
    private WaterIntake() { }

    public static WaterIntake Create(
        WaterIntakeId id,
        string userId,
        DateOnly date,
        int amountMl,
        string? note)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (amountMl <= 0)
            throw new DietPlannerDomainException("Water intake amount must be greater than zero.");

        return new WaterIntake
        {
            Id = id,
            UserId = userId,
            Date = date,
            AmountMl = amountMl,
            Timestamp = DateTime.UtcNow,
            Note = note
        };
    }

    public string UserId { get; private set; } = default!;
    public DateOnly Date { get; private set; }
    public int AmountMl { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? Note { get; private set; }
}
