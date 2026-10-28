namespace Budget.Mcp;

using System.ComponentModel;
using Budget.Application.Commands.ArchiveAccount;
using Budget.Application.Commands.ClearMonthlyLimit;
using Budget.Application.Commands.CreateAccount;
using Budget.Application.Commands.RenameAccount;
using Budget.Application.Commands.RestoreAccount;
using Budget.Application.Commands.SetMonthlyLimit;
using Budget.Application.Queries.GetAccount;
using Budget.Application.Queries.ListLimits;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;

/// <summary>
/// Envelope and monthly-limit management through the same commands as REST, returning the envelope or
/// limit as <c>get_account</c> / <c>list_limits</c> show it.
/// </summary>
[McpServerToolType]
[Authorize(Policy = BudgetMcp.WriteScope)]
internal sealed class AccountWriteTools
{
    // ponytail: no operation key, so a blind retry can add a second envelope with the same name (empty, no
    // effect on totals; archive it). Add durable replay like DietPlanner's OperationReplay if agents duplicate.
    [McpServerTool(Name = "create_account", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Creates an envelope in the caller's household budget. Not idempotent: after a lost response, check get_budget_context before calling again. Household envelopes are for owners and adults; personal ones belong to the caller or a member the caller manages.")]
    public static Task<AccountDto> CreateAccount(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Display name, 1-80 characters.")] string name,
        [Description("Household (shared) or Personal.")] string visibility,
        [Description("Personal only: owner person id, for a member the caller manages; the caller when omitted. Omit for Household.")] Guid? ownerPersonId = null,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<CreateAccountCommand, AccountDto>(
            new CreateAccountCommand(user.AuthSubject(), name, visibility, ownerPersonId), ct));

    [McpServerTool(Name = "rename_account", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Renames an envelope the caller may manage. Needs the revision from get_account; a revision conflict means it changed since: call get_account and decide again.")]
    public static Task<AccountDto> RenameAccount(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Envelope id from get_budget_context.")] Guid accountId,
        [Description("The envelope's revision from get_account.")] int expectedRevision,
        [Description("New display name, 1-80 characters.")] string name,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<RenameAccountCommand, AccountDto>(
            new RenameAccountCommand(user.AuthSubject(), accountId, expectedRevision, name), ct));

    [McpServerTool(Name = "archive_account", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Archives an envelope: it disappears from get_budget_context and new-expense choices and takes no new limits; its expenses stay. Reversible with restore_account. Needs the revision from get_account.")]
    public static Task<AccountDto> ArchiveAccount(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Envelope id from get_budget_context.")] Guid accountId,
        [Description("The envelope's revision from get_account.")] int expectedRevision,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<ArchiveAccountCommand, AccountDto>(
            new ArchiveAccountCommand(user.AuthSubject(), accountId, expectedRevision), ct));

    [McpServerTool(Name = "restore_account", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Restores an archived envelope so it can take new expenses again. Needs the revision from get_account.")]
    public static Task<AccountDto> RestoreAccount(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Envelope id from get_account (archived envelopes are not in get_budget_context).")] Guid accountId,
        [Description("The envelope's revision from get_account.")] int expectedRevision,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<RestoreAccountCommand, AccountDto>(
            new RestoreAccountCommand(user.AuthSubject(), accountId, expectedRevision), ct));

    [McpServerTool(Name = "set_limit", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Sets or changes an envelope's spending limit for one month; \"0.00\" is allowed and differs from no limit. Setting for the first time sends no revision; changing an existing limit needs its revision from list_limits. Not allowed on archived envelopes.")]
    public static Task<MonthlyLimitDto> SetLimit(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Envelope id from get_budget_context.")] Guid accountId,
        [Description("Month, YYYY-MM.")] string month,
        [Description("Decimal string with at most two decimals, e.g. \"1500.00\".")] string amount,
        [Description("The existing limit's revision from list_limits; omit when setting for the first time.")] int? expectedRevision = null,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<SetMonthlyLimitCommand, MonthlyLimitDto>(
            new SetMonthlyLimitCommand(user.AuthSubject(), accountId, month, amount, expectedRevision), ct));

    [McpServerTool(Name = "clear_limit", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Removes an envelope's limit for one month, back to no limit. Needs the revision from list_limits. Clearing a limit that is already gone succeeds.")]
    public static Task<string> ClearLimit(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Envelope id from get_budget_context.")] Guid accountId,
        [Description("Month, YYYY-MM.")] string month,
        [Description("The limit's revision from list_limits.")] int expectedRevision,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new ClearMonthlyLimitCommand(user.AuthSubject(), accountId, month, expectedRevision), ct);
            return $"Cleared the {month} limit.";
        });
}
