namespace Budget.Domain.ValueObjects;

/// <summary>The currencies a budget can use; all have two decimal places. Immutable once chosen.</summary>
public enum BudgetCurrency
{
    /// <summary>Polish złoty — the default.</summary>
    PLN,

    /// <summary>Euro.</summary>
    EUR,

    /// <summary>US dollar.</summary>
    USD,
}
