namespace DietPlanner.UnitTests.Application.Households;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Households;
#pragma warning restore IDE0005
using Household.Contracts.Interfaces;
using Shared.Abstractions.Core.Domain;

/// <summary>Unit tests for <c>HouseholdRoster</c>: who may read, plan for and log personal data for whom (design §3).</summary>
public sealed class HouseholdRosterTests
{
    private static readonly Guid Caller = Guid.Parse("0199a000-0000-7000-8000-000000000001");
    private static readonly Guid OtherAdult = Guid.Parse("0199a000-0000-7000-8000-000000000002");
    private static readonly Guid ManagedChild = Guid.Parse("0199a000-0000-7000-8000-000000000003");
    private static readonly Guid Outsider = Guid.Parse("0199a000-0000-7000-8000-000000000009");

    private static HouseholdRoster RosterAs(string role)
        => new(Caller, role,
        [
            new HouseholdContextMember(Caller, "Me", role, false, "sub-me"),
            new HouseholdContextMember(OtherAdult, "Alex", "Adult", false, "sub-alex"),
            new HouseholdContextMember(ManagedChild, "Kid", "Child", true, null),
        ]);

    /// <summary>Per role: <c>CanPlanFor</c> self, another adult, a managed child.</summary>
    [Theory]
    [InlineData("Owner", true, true, true)]
    [InlineData("Adult", true, true, true)]
    [InlineData("Child", true, false, false)]
    [InlineData("Guest", false, false, false)]
    public void CanPlanFor_PerRole_MatchesDesign(string role, bool self, bool otherAdult, bool managedChild)
    {
        var roster = RosterAs(role);

        roster.CanPlanFor(Caller).ShouldBe(self);
        roster.CanPlanFor(OtherAdult).ShouldBe(otherAdult);
        roster.CanPlanFor(ManagedChild).ShouldBe(managedChild);
    }

    /// <summary>Per role: <c>CanLogFor</c> self, another adult (never), a managed child.</summary>
    [Theory]
    [InlineData("Owner", true, true)]
    [InlineData("Adult", true, true)]
    [InlineData("Child", true, false)]
    [InlineData("Guest", false, false)]
    public void CanLogFor_PerRole_MatchesDesign(string role, bool self, bool managedChild)
    {
        var roster = RosterAs(role);

        roster.CanLogFor(Caller).ShouldBe(self);
        roster.CanLogFor(OtherAdult).ShouldBeFalse();
        roster.CanLogFor(ManagedChild).ShouldBe(managedChild);
    }

    /// <summary>Outside the household: nothing is allowed.</summary>
    [Fact]
    public void Outsider_AsOwner_IsNeitherMemberNorPlannable()
    {
        var roster = RosterAs("Owner");

        roster.IsMember(Outsider).ShouldBeFalse();
        roster.CanPlanFor(Outsider).ShouldBeFalse();
        roster.CanLogFor(Outsider).ShouldBeFalse();
    }

    /// <summary>Without a household: the caller may do everything for themselves only.</summary>
    [Fact]
    public void Solo_WithoutHousehold_AllowsOnlySelf()
    {
        var roster = new HouseholdRoster(Caller, null, []);

        roster.CanPlanFor(Caller).ShouldBeTrue();
        roster.CanLogFor(Caller).ShouldBeTrue();
        roster.IsMember(OtherAdult).ShouldBeFalse();
        roster.NameOf(Caller).ShouldBeNull();
    }

    /// <summary><c>Demand</c>: 404 outside the household, 403 inside when not allowed.</summary>
    [Fact]
    public void Demand_OutsiderOrDisallowed_ThrowsNotFoundOrForbidden()
    {
        var roster = RosterAs("Child");

        Should.Throw<NotFoundException>(() => roster.Demand(Outsider, true, "MealEntry", Guid.Empty));
        Should.Throw<ForbiddenException>(() => roster.Demand(OtherAdult, roster.CanPlanFor(OtherAdult), "MealEntry", Guid.Empty));
    }
}
