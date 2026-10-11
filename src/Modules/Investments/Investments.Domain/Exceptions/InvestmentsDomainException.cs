namespace Investments.Domain.Exceptions;

using Shared.Abstractions.Core.Domain;

/// <summary>An Investments business rule was violated. Thrown from aggregates; mapped to HTTP 422.</summary>
public sealed class InvestmentsDomainException : DomainException
{
    /// <summary>Creates the exception with the business-facing reason the rule was violated.</summary>
    /// <param name="message">The rule that was violated, phrased for the end user.</param>
    public InvestmentsDomainException(string message) : base(message) { }
}
