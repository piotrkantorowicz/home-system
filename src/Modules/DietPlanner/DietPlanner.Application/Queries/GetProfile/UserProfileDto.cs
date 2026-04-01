namespace DietPlanner.Application.Queries.GetProfile;

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
