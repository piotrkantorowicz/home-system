namespace Budget.Application.Commands.ImportExpenses;

/// <summary>Outcome of an import (or its replay).</summary>
/// <param name="ExpenseIds">The created expenses, in row order.</param>
/// <param name="Created"><see langword="true"/> when this call created them; <see langword="false"/> for a recognised retry.</param>
public sealed record ImportExpensesResult(IReadOnlyList<Guid> ExpenseIds, bool Created);
