namespace Notifications.Application.Commands.BulkMarkNotificationsRead;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Marks several of the caller's notifications read in one write; ids of other users' rows are ignored.
/// </summary>
/// <param name="Ids">The notifications to mark.</param>
/// <param name="UserId">Auth subject of the caller; only their own rows are touched.</param>
public sealed record BulkMarkNotificationsReadCommand(
    IReadOnlyCollection<Guid> Ids,
    string UserId) : ICommand;
