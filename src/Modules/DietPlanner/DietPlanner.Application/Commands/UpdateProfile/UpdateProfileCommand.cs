namespace DietPlanner.Application.Commands.UpdateProfile;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Replaces every field of the caller's profile; the profile must exist.
/// </summary>
/// <param name="PersonId">Person identifier of the caller; the command only touches this user's data.</param>
/// <param name="DateOfBirth">Date of birth, used to derive age.</param>
/// <param name="Gender"><c>Male</c>, <c>Female</c> or <c>Other</c>; validated by the command validator.</param>
/// <param name="HeightCm">Height in centimetres.</param>
/// <param name="CurrentWeightKg">Current weight in kilograms.</param>
/// <param name="TargetWeightKg">Target weight in kilograms.</param>
/// <param name="ActivityLevel">One of the <c>ActivityLevel</c> names; validated by the command validator.</param>
public sealed record UpdateProfileCommand(
    Guid PersonId,
    DateOnly? DateOfBirth,
    string? Gender,
    decimal? HeightCm,
    decimal? CurrentWeightKg,
    decimal? TargetWeightKg,
    string? ActivityLevel) : ICommand;
