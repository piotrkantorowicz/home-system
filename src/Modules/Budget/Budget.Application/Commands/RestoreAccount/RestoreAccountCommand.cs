namespace Budget.Application.Commands.RestoreAccount;

using Budget.Application.Queries.GetAccount;
using Shared.Abstractions.Cqrs;

/// <summary>Restores an envelope the caller may manage.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Id">The envelope.</param>
/// <param name="ExpectedRevision">The revision the caller last saw; a mismatch is a 409.</param>

public sealed record RestoreAccountCommand(string AuthSubject, Guid Id, int ExpectedRevision)
    : ICommand<AccountDto>;
