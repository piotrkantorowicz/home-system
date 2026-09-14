namespace Shared.Abstractions.Core.Domain;

/// <summary>
/// Base exception for a violated business rule. Modules derive their own subtype (e.g.
/// <c>HouseholdDomainException</c>) and throw it from aggregates and domain services. The message
/// carries business language only — no stack traces or storage detail — because the API boundary
/// maps it to HTTP 422 and returns the message to the client.
/// </summary>
public class DomainException : Exception
{
    /// <summary>Creates the exception with the business-facing reason the rule was violated.</summary>
    /// <param name="message">The rule that was violated, phrased for the end user.</param>
    public DomainException(string message) : base(message) { }
}
