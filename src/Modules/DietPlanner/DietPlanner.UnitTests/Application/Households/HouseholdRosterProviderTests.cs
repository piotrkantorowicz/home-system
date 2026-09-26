namespace DietPlanner.UnitTests.Application.Households;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Households;
#pragma warning restore IDE0005
using Household.Contracts.Interfaces;

/// <summary>Unit tests for <c>HouseholdRosterProvider</c>: only a confirmed "no household" yields the caller-alone roster.</summary>
public sealed class HouseholdRosterProviderTests
{
    /// <summary>A failed lookup propagates instead of producing a role-less roster that would let a Guest write.</summary>
    [Fact]
    public async Task GetAsync_WhenLookupFails_Throws()
    {
        var households = Substitute.For<IHouseholdQueryService>();
        households.GetHouseholdContextForUserAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<HouseholdContext?>(new InvalidOperationException("Household DB down")));
        var sut = new HouseholdRosterProvider(households);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.GetAsync(Guid.CreateVersion7(), "sub-guest", TestContext.Current.CancellationToken));
    }
}
