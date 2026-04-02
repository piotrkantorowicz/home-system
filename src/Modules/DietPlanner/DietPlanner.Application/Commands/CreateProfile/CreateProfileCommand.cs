namespace DietPlanner.Application.Commands.CreateProfile;

using Shared.Abstractions.CQRS;

public sealed record CreateProfileCommand(
    string UserId,
    DateOnly? DateOfBirth,
    string? Gender,
    decimal? HeightCm,
    decimal? CurrentWeightKg,
    decimal? TargetWeightKg,
    string? ActivityLevel) : ICommand<Guid>;
