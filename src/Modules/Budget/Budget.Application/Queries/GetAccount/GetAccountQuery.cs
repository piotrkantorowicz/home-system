namespace Budget.Application.Queries.GetAccount;

using Shared.Abstractions.Cqrs;

/// <summary>Reads one envelope the caller may see; not-found when it is missing or invisible.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Id">The envelope.</param>
public sealed record GetAccountQuery(string AuthSubject, Guid Id) : IQuery<AccountDto>;
