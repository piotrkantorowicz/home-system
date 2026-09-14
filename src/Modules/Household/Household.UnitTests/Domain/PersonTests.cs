namespace Household.UnitTests.Domain;

using global::Household.Domain.Aggregates;
using global::Household.Domain.Events;
using global::Household.Domain.Exceptions;
using global::Household.Domain.ValueObjects;

/// <summary>Unit tests for <c>Person</c> domain rules: in-memory only, no infrastructure and no mocks.</summary>
public sealed class PersonTests
{
    /// <summary><c>RegisterFromLogin</c> sets linked profile and raises registered event.</summary>
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

    /// <summary>With blank display name: <c>RegisterFromLogin</c> falls back to subject.</summary>
    [Fact]
    public void RegisterFromLogin_WithBlankDisplayName_FallsBackToSubject()
    {
        var person = Person.RegisterFromLogin(PersonId.New(), "auth|123", "   ", null, null);

        person.DisplayName.ShouldBe("auth|123");
    }

    /// <summary><c>CreateManaged</c> is unlinked and managed.</summary>
    [Fact]
    public void CreateManaged_IsUnlinkedAndManaged()
    {
        var person = Person.CreateManaged(PersonId.New(), "Adam", null);

        person.AuthSubject.ShouldBeNull();
        person.IsLinked.ShouldBeFalse();
        person.IsManaged.ShouldBeTrue();
    }

    /// <summary>With no name: <c>CreateManaged</c> throws.</summary>
    [Fact]
    public void CreateManaged_WithNoName_Throws()
        => Should.Throw<HouseholdDomainException>(() => Person.CreateManaged(PersonId.New(), " ", null));

    /// <summary>When nothing changed: <c>RefreshProfile</c> does not stamp updated at.</summary>
    [Fact]
    public void RefreshProfile_WhenNothingChanged_DoesNotStampUpdatedAt()
    {
        var person = Person.RegisterFromLogin(
            PersonId.New(), "s", "Name", PersonEmail.Create("a@b.com"), "u");

        person.RefreshProfile("Name", PersonEmail.Create("a@b.com"), "u");

        person.UpdatedAt.ShouldBeNull();
    }

    /// <summary>When changed: <c>RefreshProfile</c> updates and stamps updated at.</summary>
    [Fact]
    public void RefreshProfile_WhenChanged_UpdatesAndStampsUpdatedAt()
    {
        var person = Person.RegisterFromLogin(PersonId.New(), "s", "Old", null, null);

        person.RefreshProfile("New", PersonEmail.Create("a@b.com"), null);

        person.DisplayName.ShouldBe("New");
        person.Email!.Value.ShouldBe("a@b.com");
        person.UpdatedAt.ShouldNotBeNull();
    }

    /// <summary>On managed person: <c>LinkAuthSubject</c> links and raises event.</summary>
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

    /// <summary>On already linked person: <c>LinkAuthSubject</c> throws.</summary>
    [Fact]
    public void LinkAuthSubject_OnAlreadyLinkedPerson_Throws()
    {
        var person = Person.RegisterFromLogin(PersonId.New(), "auth|1", "N", null, null);

        Should.Throw<HouseholdDomainException>(() => person.LinkAuthSubject("auth|2"))
            .Message.ShouldContain("already linked");
    }

    /// <summary>On managed person: mark pending account link sets the match email.</summary>
    [Fact]
    public void MarkPendingAccountLink_OnManagedPerson_SetsTheMatchEmail()
    {
        var person = Person.CreateManaged(PersonId.New(), "Kiddo", null);

        person.MarkPendingAccountLink(PersonEmail.Create("kiddo@b.com"));

        person.Email!.Value.ShouldBe("kiddo@b.com");
        person.IsManaged.ShouldBeTrue();
        person.UpdatedAt.ShouldNotBeNull();
    }

    /// <summary>On linked person: mark pending account link throws.</summary>
    [Fact]
    public void MarkPendingAccountLink_OnLinkedPerson_Throws()
    {
        var person = Person.RegisterFromLogin(PersonId.New(), "auth|1", "N", null, null);

        Should.Throw<HouseholdDomainException>(
            () => person.MarkPendingAccountLink(PersonEmail.Create("n@b.com")))
            .Message.ShouldContain("Only a managed person");
    }
}
