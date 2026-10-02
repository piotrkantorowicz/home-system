namespace Budget.Application.Queries.GetExpense;

/// <summary>One participant's stored share.</summary>
/// <param name="PersonId">Who owes it.</param>
/// <param name="PersonDisplayName">Current roster name for a member, else the stored snapshot.</param>
/// <param name="Amount">Decimal string with two fractional digits.</param>
public sealed record ExpenseShareDto(Guid PersonId, string PersonDisplayName, string Amount);
