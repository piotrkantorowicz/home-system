namespace DietPlanner.Domain.Aggregates;

using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// A user's body facts (age, gender, height, weights, activity level) — the inputs to BMR/TDEE and
/// weight prediction. One profile per user; every field is optional because users fill it in over
/// time, and calculations that need a missing field simply return nothing.
/// </summary>
public sealed class UserProfile : AggregateRoot<UserProfileId>
{
    private UserProfile() { }

    /// <summary>Creates a profile; any field may be left unknown.</summary>
    /// <param name="id">Identifier for the new profile.</param>
    /// <param name="userId">Auth subject of the owner; required.</param>
    /// <param name="dateOfBirth">Used to derive age for the BMR formula.</param>
    /// <param name="gender">Selects the BMR constant.</param>
    /// <param name="heightCm">Height in centimetres.</param>
    /// <param name="currentWeightKg">Latest known weight in kilograms; kept in sync by weight entries.</param>
    /// <param name="targetWeightKg">Weight the user is aiming for, in kilograms.</param>
    /// <param name="activityLevel">Selects the TDEE multiplier.</param>
    /// <exception cref="ArgumentException"><paramref name="userId"/> is blank.</exception>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public static UserProfile Create(
        UserProfileId id,
        string userId,
        DateOnly? dateOfBirth,
        Gender? gender,
        decimal? heightCm,
        decimal? currentWeightKg,
        decimal? targetWeightKg,
        ActivityLevel? activityLevel,
        DateTime now)
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
            CreatedAt = now
        };
    }

    /// <summary>Auth subject of the owner.</summary>
    public string UserId { get; private set; } = default!;
    /// <summary>Date of birth, if provided; age is derived from it.</summary>
    public DateOnly? DateOfBirth { get; private set; }
    /// <summary>Gender for the BMR formula, if provided.</summary>
    public Gender? Gender { get; private set; }
    /// <summary>Height in centimetres, if provided.</summary>
    public decimal? HeightCm { get; private set; }
    /// <summary>Latest known weight in kilograms; updated whenever a newer weight entry is logged.</summary>
    public decimal? CurrentWeightKg { get; private set; }
    /// <summary>Weight the user is aiming for, in kilograms, if set.</summary>
    public decimal? TargetWeightKg { get; private set; }
    /// <summary>Activity level for the TDEE multiplier, if provided.</summary>
    public ActivityLevel? ActivityLevel { get; private set; }
    /// <summary>Creation time, UTC.</summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>Time of the last change, UTC; <see langword="null"/> if never changed.</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Replaces every field at once; pass <see langword="null"/> to clear one.</summary>
    /// <param name="dateOfBirth">New date of birth.</param>
    /// <param name="gender">New gender.</param>
    /// <param name="heightCm">New height in centimetres.</param>
    /// <param name="currentWeightKg">New current weight in kilograms.</param>
    /// <param name="targetWeightKg">New target weight in kilograms.</param>
    /// <param name="activityLevel">New activity level.</param>
    /// <param name="now">Current time, UTC; supplied by the caller.</param>
    public void Update(
        DateOnly? dateOfBirth,
        Gender? gender,
        decimal? heightCm,
        decimal? currentWeightKg,
        decimal? targetWeightKg,
        ActivityLevel? activityLevel,
        DateTime now)
    {
        DateOfBirth = dateOfBirth;
        Gender = gender;
        HeightCm = heightCm;
        CurrentWeightKg = currentWeightKg;
        TargetWeightKg = targetWeightKg;
        ActivityLevel = activityLevel;
        UpdatedAt = now;
    }

    /// <summary>Sets only the current weight — called when the newest weight entry changes.</summary>
    /// <param name="weightKg">The latest weight, or <see langword="null"/> when no entries remain.</param>
    public void UpdateCurrentWeight(decimal? weightKg, DateTime now)
    {
        CurrentWeightKg = weightKg;
        UpdatedAt = now;
    }
}
