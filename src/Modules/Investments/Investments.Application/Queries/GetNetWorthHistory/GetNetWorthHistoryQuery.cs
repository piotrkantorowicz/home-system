namespace Investments.Application.Queries.GetNetWorthHistory;

using Shared.Abstractions.Cqrs;

/// <summary>Month-end net worth over a range, at most ten years.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="From">First date; twelve months before <paramref name="To"/> when omitted.</param>
/// <param name="To">Last date; today (Europe/Warsaw) when omitted; not in the future.</param>
/// <param name="Scope"><c>household</c> (default) or <c>mine</c>.</param>
public sealed record GetNetWorthHistoryQuery(string AuthSubject, DateOnly? From, DateOnly? To, string? Scope)
    : IQuery<NetWorthHistoryDto>;
