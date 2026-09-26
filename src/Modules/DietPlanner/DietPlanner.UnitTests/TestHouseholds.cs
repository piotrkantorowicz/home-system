namespace DietPlanner.UnitTests;

#pragma warning disable IDE0005 // REASON: InternalsVisibleTo prevents Roslyn from resolving internal test types.
using DietPlanner.Application.Households;
#pragma warning restore IDE0005
using Household.Contracts.Interfaces;

/// <summary><see cref="HouseholdRosterProvider"/> instances for handler tests.</summary>
internal static class TestHouseholds
{
    /// <summary>Every caller is alone, without a household — may do everything for themselves only.</summary>
    public static HouseholdRosterProvider Solo() => With(null);

    /// <summary>Every caller resolves to <paramref name="context"/>.</summary>
    public static HouseholdRosterProvider With(HouseholdContext? context)
    {
        var households = Substitute.For<IHouseholdQueryService>();
        households.GetHouseholdContextForUserAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(context);
        return new HouseholdRosterProvider(households);
    }
}
