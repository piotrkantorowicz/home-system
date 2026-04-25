namespace DietPlanner.Domain.Exceptions;

using Shared.Abstractions.Core.Domain;

public sealed class DietPlannerDomainException : DomainException
{
    public DietPlannerDomainException(string message) : base(message) { }
}
