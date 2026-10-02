namespace Budget.Application.Commands.RenameAccount;

using Budget.Application.Queries.GetAccount;
using Shared.Abstractions.Cqrs;

/// <summary>Renames an envelope the caller may manage.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Id">The envelope.</param>
/// <param name="ExpectedRevision">The revision the caller last saw; a mismatch is a 409.</param>
/// <param name="Name">New display name, 1–80 characters.</param>
public sealed record RenameAccountCommand(string AuthSubject, Guid Id, int ExpectedRevision, string Name)
    : ICommand<AccountDto>;
