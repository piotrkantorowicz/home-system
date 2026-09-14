namespace DietPlanner.Domain.Exceptions;

using Shared.Abstractions.Core.Domain;

/// <summary>
/// A Diet Planner business rule was violated (e.g. completing a meal twice, a recipe without
/// ingredients). Thrown from aggregates and domain services; mapped to HTTP 422 at the API boundary.
/// </summary>
public sealed class DietPlannerDomainException : DomainException
{
    /// <summary>Creates the exception with the business-facing reason the rule was violated.</summary>
    /// <param name="message">The rule that was violated, phrased for the end user.</param>
    public DietPlannerDomainException(string message) : base(message) { }
}
