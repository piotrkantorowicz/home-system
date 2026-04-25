namespace DietPlanner.Application.Commands.UpdateProfile;

using Shared.Abstractions.Cqrs;

public sealed record UpdateProfileCommand(
    string UserId,
    DateOnly? DateOfBirth,
    string? Gender,
    decimal? HeightCm,
    decimal? CurrentWeightKg,
    decimal? TargetWeightKg,
    string? ActivityLevel) : ICommand;
