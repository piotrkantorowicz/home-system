namespace Budget.Domain.ValueObjects;

/// <summary>A person and the display name they had when a row was written (kept as a snapshot).</summary>
/// <param name="PersonId">The person's identifier (no cross-database foreign key).</param>
/// <param name="DisplayName">Name at write time.</param>
public sealed record PersonRef(Guid PersonId, string DisplayName);
