namespace DietPlanner.Application.Commands.CreateProfile;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Creates the caller's body profile; fails if one already exists. Returns the new profile's id.
/// </summary>
/// <param name="UserId">Auth subject of the caller; the command only touches this user's data.</param>
/// <param name="DateOfBirth">Date of birth, used to derive age.</param>
/// <param name="Gender"><c>Male</c>, <c>Female</c> or <c>Other</c>; validated by the command validator.</param>
/// <param name="HeightCm">Height in centimetres.</param>
/// <param name="CurrentWeightKg">Current weight in kilograms.</param>
/// <param name="TargetWeightKg">Target weight in kilograms.</param>
/// <param name="ActivityLevel">One of the <c>ActivityLevel</c> names; validated by the command validator.</param>
public sealed record CreateProfileCommand(
    string UserId,
    DateOnly? DateOfBirth,
    string? Gender,
    decimal? HeightCm,
    decimal? CurrentWeightKg,
    decimal? TargetWeightKg,
    string? ActivityLevel) : ICommand<Guid>;
