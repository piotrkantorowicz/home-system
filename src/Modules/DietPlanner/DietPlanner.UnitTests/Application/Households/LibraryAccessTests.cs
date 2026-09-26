namespace DietPlanner.UnitTests.Application.Households;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Households;
#pragma warning restore IDE0005
using DietPlanner.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>LibraryAccess</c>: who may read and edit a recipe / product by visibility (#230).</summary>
public sealed class LibraryAccessTests
{
    private const string Me = "sub-me";
    private const string Housemate = "sub-alex";
    private const string Outsider = "sub-out";

    private static LibraryAccess As(string? role) => new(Me, role, role is null ? [] : [Me, Housemate]);

    /// <summary>Read: own items always, a housemate's unless private, an outsider's only when public.</summary>
    [Theory]
    [InlineData(Visibility.Private, true, false, false)]
    [InlineData(Visibility.Household, true, true, false)]
    [InlineData(Visibility.Public, true, true, true)]
    public void CanRead_PerVisibility_MatchesRules(Visibility visibility, bool own, bool housemate, bool outsider)
    {
        var access = As("Child");

        access.CanRead(Me, visibility).ShouldBe(own);
        access.CanRead(Housemate, visibility).ShouldBe(housemate);
        access.CanRead(Outsider, visibility).ShouldBe(outsider);
    }

    /// <summary>Edit a housemate's non-private item: Owner/Adult only.</summary>
    [Theory]
    [InlineData("Owner", true)]
    [InlineData("Adult", true)]
    [InlineData("Child", false)]
    [InlineData("Guest", false)]
    public void CanEdit_HousematesHouseholdItem_OnlyAdults(string role, bool expected)
        => As(role).CanEdit(Housemate, Visibility.Household).ShouldBe(expected);

    /// <summary>Nobody but the creator edits a private item, and nobody edits an outsider's public one.</summary>
    [Fact]
    public void CanEdit_PrivateOrOutsiders_OnlyCreator()
    {
        var access = As("Owner");

        access.CanEdit(Me, Visibility.Private).ShouldBeTrue();
        access.CanEdit(Housemate, Visibility.Private).ShouldBeFalse();
        access.CanEdit(Outsider, Visibility.Public).ShouldBeFalse();
    }

    /// <summary>A Guest only reads: no edits, not even of their own items, and no new ones.</summary>
    [Fact]
    public void Guest_IsReadOnly()
    {
        var access = As("Guest");

        access.CanRead(Housemate, Visibility.Household).ShouldBeTrue();
        access.CanEdit(Me, Visibility.Household).ShouldBeFalse();
        Should.Throw<ForbiddenException>(access.DemandWrite);
    }

    /// <summary><c>DemandEdit</c>: 404 for an item the caller cannot see, 403 for one they see but may not edit.</summary>
    [Fact]
    public void DemandEdit_InvisibleOrReadOnly_Throws404Or403()
    {
        var access = As(null);

        Should.Throw<NotFoundException>(() => access.DemandEdit(Outsider, Visibility.Household, "Recipe", Guid.Empty));
        Should.Throw<ForbiddenException>(() => access.DemandEdit(Outsider, Visibility.Public, "Recipe", Guid.Empty));
    }
}
