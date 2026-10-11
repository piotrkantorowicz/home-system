namespace Investments.UnitTests.Application;

using Household.Contracts.Interfaces;
using Investments.Application.Common;
using Investments.Domain.Aggregates;
using Investments.Domain.ValueObjects;

/// <summary>The design §4 access matrix as applied to a single wallet.</summary>
public sealed class InvestmentsCallerTests
{
    private static readonly Guid Household = Guid.CreateVersion7();
    private static readonly HouseholdContextMember Owner = new(Guid.CreateVersion7(), "Owner", "Owner", false, "o");
    private static readonly HouseholdContextMember Adult = new(Guid.CreateVersion7(), "Adult", "Adult", false, "a");
    private static readonly HouseholdContextMember Child = new(Guid.CreateVersion7(), "Child", "Child", false, "c");
    private static readonly HouseholdContextMember Managed = new(Guid.CreateVersion7(), "Kid", "Child", true, null);
    private static readonly HouseholdContextMember[] Roster = [Owner, Adult, Child, Managed];

    private static InvestmentsCaller As(HouseholdContextMember member, IReadOnlyList<HouseholdContextMember>? roster = null)
        => new(Household, member.PersonId, member.Role, roster ?? Roster);

    private static Wallet WalletOf(HouseholdContextMember owner, WalletVisibility visibility, Guid? household = null)
        => Wallet.Create(
            WalletId.New(), household ?? Household, owner.PersonId, owner.DisplayName, "W",
            WalletInstitution.Other, WalletKind.Cash, WalletCurrency.PLN, visibility, TestClock.Now);

    /// <summary>Who can access whose wallet, per role and visibility.</summary>
    [Theory]
    [InlineData("Owner", "Adult", WalletVisibility.Shared, true)]
    [InlineData("Owner", "Adult", WalletVisibility.Private, false)]
    [InlineData("Adult", "Kid", WalletVisibility.Private, true)]
    [InlineData("Child", "Child", WalletVisibility.Private, true)]
    [InlineData("Child", "Owner", WalletVisibility.Shared, false)]
    [InlineData("Child", "Kid", WalletVisibility.Private, false)]
    public void CanAccess_Matrix(string caller, string owner, WalletVisibility visibility, bool expected)
        => As(Member(caller)).CanAccess(WalletOf(Member(owner), visibility)).ShouldBe(expected);

    /// <summary>A wallet from another household is never accessible.</summary>
    [Fact]
    public void CanAccess_OtherHousehold_False()
        => As(Owner).CanAccess(WalletOf(Owner, WalletVisibility.Shared, Guid.CreateVersion7())).ShouldBeFalse();

    /// <summary>A departed member's shared wallet drops out of the household view.</summary>
    [Fact]
    public void CanAccess_SharedWalletOfDepartedMember_False()
        => As(Owner, [Owner]).CanAccess(WalletOf(Adult, WalletVisibility.Shared)).ShouldBeFalse();

    /// <summary>Only the owner, or an adult managing the owner, may change visibility.</summary>
    [Theory]
    [InlineData("Owner", "Owner", true)]
    [InlineData("Owner", "Adult", false)]
    [InlineData("Adult", "Kid", true)]
    [InlineData("Child", "Kid", false)]
    public void CanChangeVisibility_OwnerOrManagingAdult(string caller, string owner, bool expected)
        => As(Member(caller)).CanChangeVisibility(Member(owner).PersonId).ShouldBe(expected);

    /// <summary>Guests and unknown roles are not adults and manage no one.</summary>
    [Theory]
    [InlineData("Guest")]
    [InlineData("Superuser")]
    public void UnknownOrGuestRole_IsNotAdult(string role)
    {
        var caller = new InvestmentsCaller(Household, Guid.CreateVersion7(), role, Roster);

        caller.IsAdult.ShouldBeFalse();
        caller.CanAccess(WalletOf(Owner, WalletVisibility.Shared)).ShouldBeFalse();
    }

    private static HouseholdContextMember Member(string name) => Roster.Single(m => m.DisplayName == name);
}
