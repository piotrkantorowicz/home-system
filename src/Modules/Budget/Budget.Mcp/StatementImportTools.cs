namespace Budget.Mcp;

using System.ComponentModel;
using System.Text;
using Budget.Application.Commands.ImportExpenses;
using Budget.Application.Queries.PreviewStatementImport;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;

/// <summary>
/// Bank statement import: preview the file's rows first, then record exactly the rows the user
/// confirmed. Same query and command as REST; the statement arrives as text and is never stored.
/// </summary>
[McpServerToolType]
internal sealed class StatementImportTools
{
    [McpServerTool(Name = "preview_expense_import", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Parses a bank statement (CSV or XML export, passed as its full text) and classifies each row for import into one envelope, without changing anything. Candidates are outgoing booked rows with a sourceKey, a suggestedCategory when that merchant was imported before, and a status: New, AlreadyImported (cannot be imported again) or PossibleDuplicate (matches lists the similar expenses). Skipped rows and unparseable issues are listed. Show the candidates to the user, settle categories (ask when unsure), then call execute_expense_import with only the rows they confirmed.")]
    [Authorize(Policy = BudgetMcp.ReadScope)]
    public static Task<StatementPreviewDto> PreviewExpenseImport(
        McpToolUser user,
        IQueryDispatcher dispatcher,
        [Description("Envelope id from get_budget_context that the rows would be imported into.")] Guid accountId,
        [Description("The statement file's full text, exactly as exported.")] string statement,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => dispatcher.SendAsync<PreviewStatementImportQuery, StatementPreviewDto>(
            new PreviewStatementImportQuery(user.AuthSubject(), accountId, Encoding.UTF8.GetBytes(statement)), ct));

    [McpServerTool(Name = "execute_expense_import", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Records the confirmed statement rows as expenses in one envelope, all or none (1 to 1000 rows). Only call it after the user has seen the preview and agreed. Each row needs the sourceKey and merchantKey from preview_expense_import plus the confirmed amount, date and category code (the same fields as create_expense). Idempotent per operationKey: repeat the same call after a lost response and you get the original expenses (created=false). Rows already imported are rejected.")]
    [Authorize(Policy = BudgetMcp.WriteScope)]
    [Authorize(Policy = BudgetMcp.ImportScope)]
    public static Task<ImportExpensesResult> ExecuteExpenseImport(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Idempotency key for this whole import: a fresh GUID, reused only when retrying this same call.")] Guid operationKey,
        [Description("Envelope id the rows are recorded in; the one previewed.")] Guid accountId,
        [Description("The confirmed rows, in file order.")] ImportExpenseRow[] rows,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<ImportExpensesCommand, ImportExpensesResult>(
            new ImportExpensesCommand(user.AuthSubject(), OperationKey.Require(operationKey), accountId, rows), ct));
}
