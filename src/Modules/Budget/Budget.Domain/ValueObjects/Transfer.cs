namespace Budget.Domain.ValueObjects;

/// <summary>A suggested payment that moves balances towards zero.</summary>
/// <param name="From">Who pays.</param>
/// <param name="To">Who receives.</param>
/// <param name="MinorUnits">Amount in hundredths of the currency unit, positive.</param>
public sealed record Transfer(Guid From, Guid To, long MinorUnits);
