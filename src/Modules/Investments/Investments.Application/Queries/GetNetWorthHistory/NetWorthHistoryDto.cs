namespace Investments.Application.Queries.GetNetWorthHistory;

/// <summary>A month-end net-worth series.</summary>
/// <param name="Scope"><c>household</c> or <c>mine</c>.</param>
/// <param name="Points">One point per month end in the range, plus the last date when it is not a month end; oldest first.</param>
public sealed record NetWorthHistoryDto(string Scope, IReadOnlyList<NetWorthHistoryPointDto> Points);
