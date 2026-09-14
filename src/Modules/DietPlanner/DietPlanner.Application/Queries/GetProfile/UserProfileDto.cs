namespace DietPlanner.Application.Queries.GetProfile;

/// <summary>
/// A user's body profile; every field may be unknown.
/// </summary>
/// <param name="Id">Identifier of the profile.</param>
/// <param name="UserId">Auth subject of the owner.</param>
/// <param name="DateOfBirth">Date of birth, if provided.</param>
/// <param name="Gender"><c>Male</c>, <c>Female</c> or <c>Other</c>, if provided.</param>
/// <param name="HeightCm">Height in centimetres, if provided.</param>
/// <param name="CurrentWeightKg">Latest known weight in kilograms, if provided.</param>
/// <param name="TargetWeightKg">Target weight in kilograms, if set.</param>
/// <param name="ActivityLevel">Activity level name, if provided.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
/// <param name="UpdatedAt">Time of the last change, UTC; <see langword="null"/> if never changed.</param>
public sealed record UserProfileDto(
    Guid Id,
    string UserId,
    DateOnly? DateOfBirth,
    string? Gender,
    decimal? HeightCm,
    decimal? CurrentWeightKg,
    decimal? TargetWeightKg,
    string? ActivityLevel,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
