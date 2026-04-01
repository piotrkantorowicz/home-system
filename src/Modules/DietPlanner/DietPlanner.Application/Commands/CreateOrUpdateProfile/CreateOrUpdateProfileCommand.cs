namespace DietPlanner.Application.Commands.CreateOrUpdateProfile;

using Shared.Abstractions.CQRS;

public sealed record CreateOrUpdateProfileCommand(
    string UserId,
    DateOnly? DateOfBirth,
    string? Gender,
    decimal? HeightCm,
    decimal? CurrentWeightKg,
    decimal? TargetWeightKg,
    string? ActivityLevel) : ICommand<Guid>;
