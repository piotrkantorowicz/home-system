namespace Budget.Mcp;

using System.ComponentModel;
using Budget.Application.Commands.RecordRepayment;
using Budget.Application.Commands.VoidRepayment;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;

/// <summary>
/// Recording and voiding repayments through the same commands as REST, owners and adults only. The
/// operation key is the command's <c>ClientRequestId</c>, so a retry returns the original result.
/// </summary>
[McpServerToolType]
[Authorize(Policy = BudgetMcp.WriteScope)]
internal sealed class SettlementWriteTools
{
    [McpServerTool(Name = "record_repayment", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Records that one adult actually paid another; no money moves through the app. Owners and adults only, and either party may be named. Idempotent per operationKey: repeat the same call with the same key after a lost response and you get the original repayment (created=false), never a duplicate. A payment larger than the suggestion is recorded as stated. Person ids come from get_settlement or get_budget_context.")]
    public static Task<RepaymentMutationResult> RecordRepayment(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Idempotency key: a fresh GUID per logical repayment, reused only when retrying that same call.")] Guid operationKey,
        [Description("Person id of who paid.")] Guid fromPersonId,
        [Description("Person id of who received; must differ from the payer.")] Guid toPersonId,
        [Description("Positive decimal string with at most two decimals, e.g. \"40.00\".")] string amount,
        [Description("Date the payment was made, YYYY-MM-DD.")] DateOnly paidOn,
        [Description("Optional note, up to 200 characters.")] string? note = null,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<RecordRepaymentCommand, RepaymentMutationResult>(
            new RecordRepaymentCommand(user.AuthSubject(), OperationKey.Require(operationKey), fromPersonId, toPersonId, amount, paidOn, note), ct));

    [McpServerTool(Name = "void_repayment", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Voids a recorded repayment so the debt it reduced is restored; it cannot be undone (record a new repayment instead). Needs the revision from list_repayments and a reason. Repeating a completed void adds nothing. Owners and adults only.")]
    public static Task<RepaymentMutationResult> VoidRepayment(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Repayment id from list_repayments.")] Guid repaymentId,
        [Description("The repayment's revision from list_repayments.")] int expectedRevision,
        [Description("Why it is voided, 1-200 characters.")] string reason,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<VoidRepaymentCommand, RepaymentMutationResult>(
            new VoidRepaymentCommand(user.AuthSubject(), repaymentId, expectedRevision, reason), ct));
}
