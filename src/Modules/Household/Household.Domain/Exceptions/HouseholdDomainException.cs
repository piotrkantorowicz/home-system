namespace Household.Domain.Exceptions;

using Shared.Abstractions.Core.Domain;

/// <summary>
/// A Household business rule was violated (e.g. removing the last owner, inviting someone as owner).
/// Thrown from aggregates; mapped to HTTP 422 at the API boundary.
/// </summary>
public sealed class HouseholdDomainException : DomainException
{
    /// <summary>Creates the exception with the business-facing reason the rule was violated.</summary>
    /// <param name="message">The rule that was violated, phrased for the end user.</param>
    public HouseholdDomainException(string message) : base(message) { }
}
