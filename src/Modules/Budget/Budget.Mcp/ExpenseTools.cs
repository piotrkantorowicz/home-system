namespace Budget.Mcp;

using System.ComponentModel;
using Budget.Application.Queries.GetExpense;
using Budget.Application.Queries.ListExpenses;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;

/// <summary>
/// Read-only expenses the caller may see in the app, through the same queries as REST.
/// </summary>
[McpServerToolType]
[Authorize(Policy = BudgetMcp.ReadScope)]
internal sealed class ExpenseTools
{
    [McpServerTool(Name = "search_expenses", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Pages the expenses the caller can see, newest purchase first, with exact totals over the whole filter (not just the page). Every filter is optional. Amounts are decimal strings.")]
    public static async Task<ExpenseListDto> SearchExpenses(
        McpToolUser user,
        IQueryDispatcher dispatcher,
        [Description("Only this envelope, an account id from get_budget_context.")] Guid? accountId = null,
        [Description("Only this category code from get_budget_context.")] string? category = null,
        [Description("Only this exact amount, decimal string such as \"123.45\".")] string? amount = null,
        [Description("Earliest purchase date, YYYY-MM-DD, inclusive.")] DateOnly? from = null,
        [Description("Latest purchase date, YYYY-MM-DD, inclusive.")] DateOnly? to = null,
        [Description("Case-insensitive text matched against the description.")] string? search = null,
        [Description("Also return voided expenses; they never count towards totals.")] bool includeVoided = false,
        [Description("1-based page number.")] int page = 1,
        [Description("Items per page, 1 to 100.")] int pageSize = 25,
        CancellationToken ct = default)
    {
        if (page < 1)
            throw new McpException("page must be 1 or greater.");
        if (pageSize is < 1 or > ListExpensesQuery.MaxPageSize)
            throw new McpException($"pageSize must be between 1 and {ListExpensesQuery.MaxPageSize}.");

        return await McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<ListExpensesQuery, ExpenseListDto>(
            new ListExpensesQuery(user.AuthSubject(), page, pageSize, accountId, category, amount, from, to,
                IncludeVoided: includeVoided, Search: search), ct));
    }

    [McpServerTool(Name = "get_expense", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Returns one expense the caller can see, with its stored shares and its full correction history, oldest first.")]
    public static Task<ExpenseDto> GetExpense(
        McpToolUser user,
        IQueryDispatcher dispatcher,
        [Description("Expense id from search_expenses.")] Guid expenseId,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<GetExpenseQuery, ExpenseDto>(
            new GetExpenseQuery(user.AuthSubject(), expenseId), ct));
}
