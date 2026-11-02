namespace Investments.Mcp;

using System.ComponentModel;
using Investments.Application.Commands.DeleteContribution;
using Investments.Application.Commands.RecordContribution;
using Investments.Application.Queries.ListContributions;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;

/// <summary>
/// Deposits and withdrawals through the same commands as REST. Several per date are allowed, so a
/// recorded contribution is never merged with another; correct one by deleting it and recording again.
/// </summary>
[McpServerToolType]
[Authorize(Policy = InvestmentsMcp.WriteScope)]
internal sealed class ContributionWriteTools
{
    // ponytail: no operation key, so a blind retry adds a second entry (delete it). Add durable replay like
    // DietPlanner's OperationReplay if agents start duplicating.
    [McpServerTool(Name = "record_contribution", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Records money put into (Deposit) or taken out of (Withdrawal) a wallet; their net sum is the wallet's invested capital in get_net_worth. Not idempotent: after a lost response, check list_contributions before calling again.")]
    public static Task<ContributionDto> RecordContribution(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Wallet id from list_wallets; not archived.")] Guid walletId,
        [Description("When the money moved, yyyy-MM-dd; not in the future.")] DateOnly date,
        [Description("Deposit or Withdrawal.")] string direction,
        [Description("Amount in the wallet's currency as a decimal string, e.g. \"500.00\": greater than zero, at most two decimals.")] string amount,
        [Description("Optional note, at most 200 characters.")] string? note = null,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<RecordContributionCommand, ContributionDto>(
            new RecordContributionCommand(user.AuthSubject(), walletId, date, direction, amount, note), ct));

    [McpServerTool(Name = "delete_contribution", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Deletes one deposit or withdrawal. Cannot be undone. Fails when it no longer exists, so a repeat reports it as already gone.")]
    public static Task<string> DeleteContribution(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Wallet id from list_wallets.")] Guid walletId,
        [Description("Contribution id from list_contributions.")] Guid contributionId,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new DeleteContributionCommand(user.AuthSubject(), walletId, contributionId), ct);
            return "Deleted the contribution.";
        });
}
