namespace Investments.Application.Queries.GetNetWorth;

using Shared.Abstractions.Cqrs;

/// <summary>Net worth on a date for the household or the caller.</summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="AsOf">Reference date; today (Europe/Warsaw) when omitted; not in the future.</param>
/// <param name="Scope"><c>household</c> (default) or <c>mine</c>.</param>
public sealed record GetNetWorthQuery(string AuthSubject, DateOnly? AsOf, string? Scope) : IQuery<NetWorthDto>;
