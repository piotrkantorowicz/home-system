namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

public sealed class UserProfile : AggregateRoot<UserProfileId>
{
    private UserProfile() { }

    public static UserProfile Create(
        UserProfileId id,
        string userId,
        DateOnly? dateOfBirth,
        Gender? gender,
        decimal? heightCm,
        decimal? currentWeightKg,
        decimal? targetWeightKg,
        ActivityLevel? activityLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return new UserProfile
        {
            Id = id,
            UserId = userId,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            HeightCm = heightCm,
            CurrentWeightKg = currentWeightKg,
            TargetWeightKg = targetWeightKg,
            ActivityLevel = activityLevel,
            CreatedAt = DateTime.UtcNow
        };
    }

    public string UserId { get; private set; } = default!;
    public DateOnly? DateOfBirth { get; private set; }
    public Gender? Gender { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? CurrentWeightKg { get; private set; }
    public decimal? TargetWeightKg { get; private set; }
    public ActivityLevel? ActivityLevel { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public void Update(
        DateOnly? dateOfBirth,
        Gender? gender,
        decimal? heightCm,
        decimal? currentWeightKg,
        decimal? targetWeightKg,
        ActivityLevel? activityLevel)
    {
        DateOfBirth = dateOfBirth;
        Gender = gender;
        HeightCm = heightCm;
        CurrentWeightKg = currentWeightKg;
        TargetWeightKg = targetWeightKg;
        ActivityLevel = activityLevel;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCurrentWeight(decimal? weightKg)
    {
        CurrentWeightKg = weightKg;
        UpdatedAt = DateTime.UtcNow;
    }
}
