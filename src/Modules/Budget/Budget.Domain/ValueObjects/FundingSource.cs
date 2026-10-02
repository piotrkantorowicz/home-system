namespace Budget.Domain.ValueObjects;

/// <summary>Where the money for an expense came from.</summary>
public enum FundingSource
{
    /// <summary>One person's own money; they get personal credit and the cost may be split among adults.</summary>
    Individual,

    /// <summary>The household's joint funds: nobody is credited and nothing is split.</summary>
    HouseholdFunds,
}
