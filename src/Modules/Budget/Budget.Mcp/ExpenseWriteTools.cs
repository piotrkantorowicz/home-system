namespace Budget.Mcp;

using System.ComponentModel;
using Budget.Application.Commands.CreateExpense;
using Budget.Application.Commands.UpdateExpense;
using Budget.Application.Commands.VoidExpense;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;

/// <summary>
/// Recording, correcting and voiding expenses through the same commands as REST. The operation key is
/// the command's <c>ClientRequestId</c>, so a retry with the same key and payload returns the original
/// result instead of a second expense.
/// </summary>
[McpServerToolType]
[Authorize(Policy = BudgetMcp.WriteScope)]
internal sealed class ExpenseWriteTools
{
    [McpServerTool(Name = "create_expense", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Records an expense in an envelope the caller can use. Idempotent per operationKey: repeat the same call with the same key after a lost response and you get the original expense (created=false), never a duplicate. Use a new key for a new expense. Validation errors name the field, e.g. \"Amount: ...\". Read get_budget_context first for envelope ids, category codes, funding sources and adults.")]
    public static Task<ExpenseMutationResult> CreateExpense(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Idempotency key: a fresh GUID per logical expense, reused only when retrying that same call.")] Guid operationKey,
        [Description("Envelope id from get_budget_context.")] Guid accountId,
        [Description("Positive decimal string with at most two decimals, e.g. \"123.45\".")] string amount,
        [Description("Purchase date, YYYY-MM-DD.")] DateOnly occurredOn,
        [Description("Category code from get_budget_context.")] string category,
        [Description("Individual (default) or HouseholdFunds.")] string? fundingSource = null,
        [Description("Payer person id; only for an individually funded shared expense.")] Guid? paidByPersonId = null,
        [Description("Adult person ids sharing the cost equally; required for an individually funded shared expense, omit otherwise.")] Guid[]? participantIds = null,
        [Description("Optional short note, at most 80 characters.")] string? description = null,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<CreateExpenseCommand, ExpenseMutationResult>(
            new CreateExpenseCommand(user.AuthSubject(), operationKey, accountId, amount, occurredOn, category, fundingSource,
                paidByPersonId, participantIds, description), ct));

    [McpServerTool(Name = "update_expense", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Corrects an expense in place; envelope and recorder cannot change. Send every field (current values to keep them), the revision from get_expense and a reason. A revision conflict means it changed since: call get_expense and decide again. Idempotent per operationKey. Voided expenses cannot be edited.")]
    public static Task<ExpenseMutationResult> UpdateExpense(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Expense id from search_expenses.")] Guid expenseId,
        [Description("Idempotency key: a fresh GUID per correction, reused only when retrying that same call.")] Guid operationKey,
        [Description("The expense's current revision from get_expense.")] int expectedRevision,
        [Description("Why it is corrected, 1-200 characters.")] string reason,
        [Description("Positive decimal string with at most two decimals.")] string amount,
        [Description("Purchase date, YYYY-MM-DD.")] DateOnly occurredOn,
        [Description("Category code from get_budget_context.")] string category,
        [Description("Individual (default) or HouseholdFunds.")] string? fundingSource = null,
        [Description("Payer person id; only for an individually funded shared expense.")] Guid? paidByPersonId = null,
        [Description("Adult person ids sharing the cost equally.")] Guid[]? participantIds = null,
        [Description("Short note, at most 80 characters; omitted or blank clears it.")] string? description = null,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<UpdateExpenseCommand, ExpenseMutationResult>(
            new UpdateExpenseCommand(user.AuthSubject(), expenseId, operationKey, expectedRevision, reason, amount, occurredOn,
                category, fundingSource, paidByPersonId, participantIds, description), ct));

    [McpServerTool(Name = "void_expense", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Voids an expense: it stops counting towards totals and can no longer be edited; its history stays and this cannot be undone. Needs the revision from get_expense and a reason. Repeating a completed void adds no revision.")]
    public static Task<ExpenseMutationResult> VoidExpense(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Expense id from search_expenses.")] Guid expenseId,
        [Description("Idempotency key: a fresh GUID per void, reused only when retrying that same call.")] Guid operationKey,
        [Description("The expense's current revision from get_expense.")] int expectedRevision,
        [Description("Why it is voided, 1-200 characters.")] string reason,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<VoidExpenseCommand, ExpenseMutationResult>(
            new VoidExpenseCommand(user.AuthSubject(), expenseId, operationKey, expectedRevision, reason), ct));
}
