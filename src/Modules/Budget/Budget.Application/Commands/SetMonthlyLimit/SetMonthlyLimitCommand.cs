namespace Budget.Application.Commands.SetMonthlyLimit;

using Budget.Application.Queries.ListLimits;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Sets (or changes) an envelope's limit for one month. A first-time set sends no revision; changing
/// an existing limit needs the revision the caller saw. Not allowed on archived envelopes.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="AccountId">The envelope.</param>
/// <param name="Month"><c>YYYY-MM</c>.</param>
/// <param name="Amount">Decimal string, at most two decimals; zero is allowed.</param>
/// <param name="ExpectedRevision">The existing limit's revision; <see langword="null"/> when setting for the first time.</param>
public sealed record SetMonthlyLimitCommand(string AuthSubject, Guid AccountId, string Month, string Amount, int? ExpectedRevision)
    : ICommand<MonthlyLimitDto>;
