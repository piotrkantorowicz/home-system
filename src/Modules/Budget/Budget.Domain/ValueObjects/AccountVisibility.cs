namespace Budget.Domain.ValueObjects;

/// <summary>Who can see an envelope. Immutable once created.</summary>
public enum AccountVisibility
{
    /// <summary>Shared by the household's adults; has no personal owner.</summary>
    Household,

    /// <summary>Private to one person; requires an owner.</summary>
    Personal,
}
