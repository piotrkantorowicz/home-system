namespace Investments.Application.Queries.ListContributions;

/// <summary>One deposit or withdrawal.</summary>
/// <param name="Id">The contribution.</param>
/// <param name="Date">When the money moved.</param>
/// <param name="Direction"><c>Deposit</c> or <c>Withdrawal</c>.</param>
/// <param name="Amount">Positive decimal string in the wallet's currency, e.g. <c>"500.00"</c>.</param>
/// <param name="Note">Optional note.</param>
/// <param name="RecordedByName">Who recorded it (current name while they are a member).</param>
/// <param name="RecordedAt">When it was recorded, UTC.</param>
public sealed record ContributionDto(
    Guid Id,
    DateOnly Date,
    string Direction,
    string Amount,
    string? Note,
    string RecordedByName,
    DateTimeOffset RecordedAt);
