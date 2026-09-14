namespace DietPlanner.Application.Queries.GetProfile;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads the caller's body profile; <see langword="null"/> when none has been created.
/// </summary>
/// <param name="UserId">Auth subject of the caller.</param>
public sealed record GetProfileQuery(string UserId) : IQuery<UserProfileDto?>;
