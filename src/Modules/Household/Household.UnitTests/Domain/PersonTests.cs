namespace Household.UnitTests.Domain;

using global::Household.Domain.Aggregates;
using global::Household.Domain.Events;
using global::Household.Domain.Exceptions;
using global::Household.Domain.ValueObjects;

public sealed class PersonTests
{
    [Fact]
    public void RegisterFromLogin_SetsLinkedProfile_AndRaisesRegisteredEvent()
    {
        var person = Person.RegisterFromLogin(
            PersonId.New(), "auth|123", "Ada Lovelace", PersonEmail.Create("ADA@example.com"), " https://img/a.png ");

        person.AuthSubject.ShouldBe("auth|123");
        person.IsLinked.ShouldBeTrue();
        person.IsManaged.ShouldBeFalse();
        person.DisplayName.ShouldBe("Ada Lovelace");
        person.Email!.Value.ShouldBe("ada@example.com");
        person.AvatarUrl.ShouldBe("https://img/a.png");
        person.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<PersonRegisteredDomainEvent>();
    }

    [Fact]
    public void RegisterFromLogin_WithBlankDisplayName_FallsBackToSubject()
    {
        var person = Person.RegisterFromLogin(PersonId.New(), "auth|123", "   ", null, null);

        person.DisplayName.ShouldBe("auth|123");
    }

    [Fact]
    public void CreateManaged_IsUnlinkedAndManaged()
    {
        var person = Person.CreateManaged(PersonId.New(), "Adam", null);

        person.AuthSubject.ShouldBeNull();
        person.IsLinked.ShouldBeFalse();
        person.IsManaged.ShouldBeTrue();
    }

    [Fact]
    public void CreateManaged_WithNoName_Throws()
        => Should.Throw<HouseholdDomainException>(() => Person.CreateManaged(PersonId.New(), " ", null));

    [Fact]
    public void RefreshProfile_WhenNothingChanged_DoesNotStampUpdatedAt()
    {
        var person = Person.RegisterFromLogin(
            PersonId.New(), "s", "Name", PersonEmail.Create("a@b.com"), "u");

        person.RefreshProfile("Name", PersonEmail.Create("a@b.com"), "u");

        person.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void RefreshProfile_WhenChanged_UpdatesAndStampsUpdatedAt()
    {
        var person = Person.RegisterFromLogin(PersonId.New(), "s", "Old", null, null);

        person.RefreshProfile("New", PersonEmail.Create("a@b.com"), null);

        person.DisplayName.ShouldBe("New");
        person.Email!.Value.ShouldBe("a@b.com");
        person.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void LinkAuthSubject_OnManagedPerson_LinksAndRaisesEvent()
    {
        var person = Person.CreateManaged(PersonId.New(), "Adam", PersonEmail.Create("adam@b.com"));
        person.ClearDomainEvents();

        person.LinkAuthSubject("auth|adam");

        person.AuthSubject.ShouldBe("auth|adam");
        person.IsManaged.ShouldBeFalse();
        person.IsLinked.ShouldBeTrue();
        person.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<PersonLinkedToAccountDomainEvent>();
    }

    [Fact]
    public void LinkAuthSubject_OnAlreadyLinkedPerson_Throws()
    {
        var person = Person.RegisterFromLogin(PersonId.New(), "auth|1", "N", null, null);

        Should.Throw<HouseholdDomainException>(() => person.LinkAuthSubject("auth|2"))
            .Message.ShouldContain("already linked");
    }
}
