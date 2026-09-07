namespace Household.Domain.ValueObjects;

/// <summary>
/// A member's authority within a household. Ordered least-to-most privileged for readability;
/// capability checks are explicit, not ordinal comparisons.
/// </summary>
public enum HouseholdRole
{
    /// <summary>Reads shared resources only.</summary>
    Guest,

    /// <summary>Reads shared resources; writes own personal resources; managed by an adult.</summary>
    Child,

    /// <summary>Full read/write of shared resources.</summary>
    Adult,

    /// <summary>Adult plus management of members, roles, and the household itself.</summary>
    Owner,
}
