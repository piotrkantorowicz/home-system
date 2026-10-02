namespace Budget.Application.Commands.ArchiveAccount;

using Budget.Application.Queries.GetAccount;
using Shared.Abstractions.Cqrs;

/// <summary>Archives an envelope the caller may manage.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Id">The envelope.</param>
/// <param name="ExpectedRevision">The revision the caller last saw; a mismatch is a 409.</param>

public sealed record ArchiveAccountCommand(string AuthSubject, Guid Id, int ExpectedRevision)
    : ICommand<AccountDto>;
