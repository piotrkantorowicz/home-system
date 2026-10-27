namespace Investments.Mcp;

using System.ComponentModel;
using Investments.Application.Commands.DeleteSnapshot;
using Investments.Application.Commands.RecordCheckIn;
using Investments.Application.Commands.RecordSnapshot;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;

/// <summary>
/// Balance writes through the same commands as REST. A balance is keyed by wallet and date, so a retried
/// call overwrites the same balance instead of adding another; no operation key is needed.
/// </summary>
[McpServerToolType]
[Authorize(Policy = InvestmentsMcp.WriteScope)]
internal sealed class SnapshotWriteTools
{
    [McpServerTool(Name = "record_snapshot", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Records a wallet's balance for a date, or corrects the one already recorded for that date. Repeating the call changes nothing. Created is false for a correction or repeat.")]
    public static Task<RecordSnapshotResult> RecordSnapshot(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Wallet id from list_wallets.")] Guid walletId,
        [Description("Date of the balance, yyyy-MM-dd; not in the future.")] DateOnly asOf,
        [Description("Balance in the wallet's currency as a decimal string, e.g. \"12450.10\": not negative, at most two decimals.")] string amount,
        [Description("Optional note, at most 200 characters.")] string? note = null,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<RecordSnapshotCommand, RecordSnapshotResult>(
            new RecordSnapshotCommand(user.AuthSubject(), walletId, asOf, amount, note), ct));

    [McpServerTool(Name = "record_check_in", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Records balances for several wallets on one date, all or nothing: one bad entry saves none and names the wallet. Wallets left out are untouched. Repeating the call changes nothing.")]
    public static Task<RecordCheckInResult> RecordCheckIn(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Date of every balance, yyyy-MM-dd; not in the future.")] DateOnly asOf,
        [Description("One entry per wallet, at most 100: walletId from list_wallets, amount as a decimal string such as \"12450.10\", optional note.")] IReadOnlyList<CheckInEntry> entries,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<RecordCheckInCommand, RecordCheckInResult>(
            new RecordCheckInCommand(user.AuthSubject(), asOf, entries), ct));

    [McpServerTool(Name = "delete_snapshot", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Deletes a wallet's balance for one date. Cannot be undone. Fails when nothing is recorded for that date, so a repeat reports it as already gone.")]
    public static Task<string> DeleteSnapshot(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Wallet id from list_wallets.")] Guid walletId,
        [Description("Date of the balance to delete, yyyy-MM-dd.")] DateOnly asOf,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(async () =>
        {
            await commands.SendAsync(new DeleteSnapshotCommand(user.AuthSubject(), walletId, asOf), ct);
            return $"Deleted the {asOf:yyyy-MM-dd} balance.";
        });
}
