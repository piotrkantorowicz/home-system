namespace Budget.Application.Queries.GetAccount;

/// <summary>An envelope as seen by an authorised caller.</summary>
/// <param name="Id">Envelope identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Visibility"><c>Household</c> or <c>Personal</c>.</param>
/// <param name="OwnerPersonId">Owner of a personal envelope; <see langword="null"/> for a household one.</param>
/// <param name="IsArchived">Hidden from new-entry choices.</param>
/// <param name="Revision">Send back on rename/archive/restore to detect stale edits.</param>
/// <param name="CreatedAt">Creation time, UTC.</param>
public sealed record AccountDto(
    Guid Id,
    string Name,
    string Visibility,
    Guid? OwnerPersonId,
    bool IsArchived,
    int Revision,
    DateTime CreatedAt);
