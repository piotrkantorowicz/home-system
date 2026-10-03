namespace Budget.Domain.ValueObjects;

/// <summary>
/// The full financial state an expense revision produced, including every exact share and name
/// snapshot. Property-initialised (not positional) so EF can bind the nested share collection.
/// </summary>
public sealed record ExpenseSnapshot
{
    /// <summary>Expense amount.</summary>
    public required decimal Amount { get; init; }

    /// <summary>Category code.</summary>
    public required ExpenseCategory Category { get; init; }

    /// <summary>Purchase date.</summary>
    public required DateOnly OccurredOn { get; init; }

    /// <summary>Description in this state; <see langword="null"/> when none (and for revisions written before descriptions existed).</summary>
    public string? Description { get; init; }

    /// <summary>Funding source.</summary>
    public required FundingSource FundingSource { get; init; }

    /// <summary>Payer, or <see langword="null"/> for household funds.</summary>
    public Guid? PaidByPersonId { get; init; }

    /// <summary>Payer name snapshot.</summary>
    public string? PaidByDisplayName { get; init; }

    /// <summary>Recorder.</summary>
    public required Guid AddedByPersonId { get; init; }

    /// <summary>Recorder name snapshot.</summary>
    public required string AddedByDisplayName { get; init; }

    /// <summary>Whether the expense was void in this state.</summary>
    public bool IsVoided { get; init; }

    /// <summary>The stored shares.</summary>
    public required List<ShareSnapshot> Shares { get; init; }
}
