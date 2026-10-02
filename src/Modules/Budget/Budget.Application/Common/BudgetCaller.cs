namespace Budget.Application.Common;

/// <summary>The authenticated caller resolved against their current household.</summary>
/// <param name="HouseholdId">The caller's household.</param>
/// <param name="PersonId">The caller's person identifier — never taken from a request body.</param>
/// <param name="Role">The caller's household role name.</param>
internal sealed record BudgetCaller(Guid HouseholdId, Guid PersonId, string Role)
{
    /// <summary>Whether the caller is an Owner or Adult — the only roles with shared-money access.</summary>
    public bool IsAdult => Role is "Owner" or "Adult";
}
