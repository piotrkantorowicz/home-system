namespace Household.Application.Commands.SyncCurrentPerson;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Ensures a <c>Person</c> exists for the signed-in Authentik account and its profile
/// reflects the latest OIDC claims. Returns the person's id. Idempotent — safe to call on
/// every login (and on every request, via the claims transformer).
/// </summary>
public sealed record SyncCurrentPersonCommand(
    string AuthSubject,
    string DisplayName,
    string? Email,
    string? AvatarUrl) : ICommand<Guid>;
