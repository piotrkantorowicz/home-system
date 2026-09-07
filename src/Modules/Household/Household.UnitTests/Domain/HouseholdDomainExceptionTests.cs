namespace Household.UnitTests.Domain;

using global::Household.Domain.Exceptions;
using Shared.Abstractions.Core.Domain;

public sealed class HouseholdDomainExceptionTests
{
    [Fact]
    public void Ctor_SetsMessage_AndIsADomainException()
    {
        var ex = new HouseholdDomainException("household rule broken");

        ex.Message.ShouldBe("household rule broken");
        ex.ShouldBeAssignableTo<DomainException>();
    }
}
