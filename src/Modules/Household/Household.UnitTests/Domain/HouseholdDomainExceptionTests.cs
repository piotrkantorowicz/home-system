namespace Household.UnitTests.Domain;

using global::Household.Domain.Exceptions;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>HouseholdDomainException</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class HouseholdDomainExceptionTests
{
    /// <summary><c>Ctor</c> sets message and is a domain exception.</summary>
    [Fact]
    public void Ctor_SetsMessage_AndIsADomainException()
    {
        var ex = new HouseholdDomainException("household rule broken");

        ex.Message.ShouldBe("household rule broken");
        ex.ShouldBeAssignableTo<DomainException>();
    }
}
