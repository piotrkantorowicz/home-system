namespace Investments.Application.Commands.RecordSnapshot;

/// <summary>The stored balance.</summary>
/// <param name="AsOf">The date.</param>
/// <param name="Amount">Normalised decimal string, e.g. <c>"12450.10"</c>.</param>
/// <param name="Note">The stored note.</param>
/// <param name="Created">True when the date had no balance before; false for a correction or repeat.</param>
public sealed record RecordSnapshotResult(DateOnly AsOf, string Amount, string? Note, bool Created);
