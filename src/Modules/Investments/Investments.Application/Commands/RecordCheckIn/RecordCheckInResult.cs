namespace Investments.Application.Commands.RecordCheckIn;

/// <summary>What a check-in stored.</summary>
/// <param name="AsOf">The date.</param>
/// <param name="Saved">Entries stored (new or corrected; repeats count too).</param>
/// <param name="Created">Entries that were new for their wallet and date.</param>
public sealed record RecordCheckInResult(DateOnly AsOf, int Saved, int Created);
