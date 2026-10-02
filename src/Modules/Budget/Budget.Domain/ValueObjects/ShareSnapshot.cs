namespace Budget.Domain.ValueObjects;

/// <summary>One stored share inside an expense revision snapshot.</summary>
public sealed record ShareSnapshot
{
    /// <summary>Who owes the share.</summary>
    public required Guid PersonId { get; init; }

    /// <summary>Name snapshot.</summary>
    public required string PersonDisplayName { get; init; }

    /// <summary>Exact amount, two decimals.</summary>
    public required decimal Amount { get; init; }
}
