namespace Budget.Application.Queries.GetSummary;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Monthly spending by envelope and category. <c>Shared</c> covers household envelopes (Owner/Adult
/// only); <c>Personal</c> covers one person's envelopes — the caller's, or for an adult a managed member's.
/// Personal spending never enters Shared totals.
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="Month"><c>YYYY-MM</c>.</param>
/// <param name="Scope"><c>Shared</c> or <c>Personal</c>.</param>
/// <param name="OwnerPersonId">Personal scope only: whose envelopes; defaults to the caller.</param>
public sealed record GetSummaryQuery(string AuthSubject, string Month, string Scope, Guid? OwnerPersonId = null) : IQuery<SummaryDto>;
