namespace Household.Domain.Exceptions;

using Shared.Abstractions.Core.Domain;

public sealed class HouseholdDomainException : DomainException
{
    public HouseholdDomainException(string message) : base(message) { }
}
