namespace Budget.Application.StatementImports;

/// <summary>Why a statement data row could not become a transaction.</summary>
internal enum StatementIssueCode
{
    /// <summary>The row has fewer fields than a column the profile reads.</summary>
    MissingColumn,

    /// <summary>A date cell is empty or not in the profile's date format.</summary>
    InvalidDate,

    /// <summary>An amount cell is empty or not a number in the profile's format.</summary>
    InvalidAmount,

    /// <summary>An amount has more than two decimal places; it is reported, never rounded.</summary>
    ExcessPrecision,
}
