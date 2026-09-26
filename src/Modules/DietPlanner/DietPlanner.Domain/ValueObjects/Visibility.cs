namespace DietPlanner.Domain.ValueObjects;

/// <summary>
/// Who can see a library item (recipe or product) besides its creator.
/// </summary>
public enum Visibility
{
    /// <summary>Only the creator.</summary>
    Private,

    /// <summary>Members of the creator's household; the default.</summary>
    Household,

    /// <summary>Every user.</summary>
    Public
}
