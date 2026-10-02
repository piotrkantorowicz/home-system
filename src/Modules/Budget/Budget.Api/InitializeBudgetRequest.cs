namespace Budget.Api;

/// <summary>Body of <c>POST /api/budget</c>.</summary>
/// <param name="Currency"><c>PLN</c> (default when omitted), <c>EUR</c> or <c>USD</c>; immutable once the budget exists.</param>
internal sealed record InitializeBudgetRequest(string? Currency);
