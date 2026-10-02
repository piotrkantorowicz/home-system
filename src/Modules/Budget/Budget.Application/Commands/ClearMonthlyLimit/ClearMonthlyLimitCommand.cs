namespace Budget.Application.Commands.ClearMonthlyLimit;

using Shared.Abstractions.Cqrs;

/// <summary>Removes an envelope's limit for one month (back to "no limit"). Clearing a limit that is already gone succeeds.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="AccountId">The envelope.</param>
/// <param name="Month"><c>YYYY-MM</c>.</param>
/// <param name="ExpectedRevision">The revision the caller last saw.</param>
public sealed record ClearMonthlyLimitCommand(string AuthSubject, Guid AccountId, string Month, int ExpectedRevision) : ICommand;
