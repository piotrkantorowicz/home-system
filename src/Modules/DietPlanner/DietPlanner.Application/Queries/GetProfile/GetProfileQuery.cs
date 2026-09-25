namespace DietPlanner.Application.Queries.GetProfile;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Reads the caller's body profile; <see langword="null"/> when none has been created.
/// </summary>
/// <param name="PersonId">Person identifier of the caller.</param>
public sealed record GetProfileQuery(Guid PersonId) : IQuery<UserProfileDto?>;
