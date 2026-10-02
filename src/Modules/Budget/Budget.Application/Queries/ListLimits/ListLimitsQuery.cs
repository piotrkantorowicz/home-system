namespace Budget.Application.Queries.ListLimits;

using Shared.Abstractions.Cqrs;

/// <summary>Reads the limits set for the month on envelopes the caller may see.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Month"><c>YYYY-MM</c>.</param>
public sealed record ListLimitsQuery(string AuthSubject, string Month) : IQuery<IReadOnlyList<MonthlyLimitDto>>;
