namespace Investments.Application.Commands.RecordContribution;

using Investments.Application.Queries.ListContributions;
using Shared.Abstractions.Cqrs;

/// <summary>Records money put into or taken out of a wallet.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="WalletId">A wallet the caller may see.</param>
/// <param name="Date">When the money moved; not in the future.</param>
/// <param name="Direction"><c>Deposit</c> or <c>Withdrawal</c>.</param>
/// <param name="Amount">Decimal string, &gt; 0, at most two decimals, e.g. <c>"500.00"</c>.</param>
/// <param name="Note">Optional note, at most 200 characters.</param>
public sealed record RecordContributionCommand(
    string AuthSubject, Guid WalletId, DateOnly Date, string Direction, string Amount, string? Note)
    : ICommand<ContributionDto>;
