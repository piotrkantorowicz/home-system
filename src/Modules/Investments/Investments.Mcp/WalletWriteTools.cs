namespace Investments.Mcp;

using System.ComponentModel;
using Investments.Application.Commands.ArchiveWallet;
using Investments.Application.Commands.CreateWallet;
using Investments.Application.Commands.RestoreWallet;
using Investments.Application.Commands.UpdateWallet;
using Investments.Application.Queries.GetWallet;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Web;

/// <summary>
/// Wallet management through the same commands as REST, returning the wallet as <c>get_wallet</c> shows it.
/// </summary>
[McpServerToolType]
[Authorize(Policy = InvestmentsMcp.WriteScope)]
internal sealed class WalletWriteTools
{
    // ponytail: no operation key, so a blind retry can add a second empty wallet (no balance, no effect on
    // totals; archive it). Add durable replay like DietPlanner's OperationReplay if agents start duplicating.
    [McpServerTool(Name = "create_wallet", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Creates an investment wallet in the caller's household, for the caller or a member the caller manages. Not idempotent: after a lost response, check list_wallets before calling again. An invalid institution, kind or currency lists the allowed values.")]
    public static Task<WalletDto> CreateWallet(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Display name, 1-80 characters.")] string name,
        [Description("Institution, e.g. XTB or PKOBP.")] string institution,
        [Description("Account kind, e.g. Brokerage, IKE, IKZE or Savings.")] string kind,
        [Description("PLN, EUR or USD; cannot change later.")] string currency,
        [Description("Shared (the household sees it) or Private (owner and managing adults only).")] string visibility,
        [Description("Owner person id, for a member the caller manages; the caller when omitted.")] Guid? ownerPersonId = null,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<CreateWalletCommand, WalletDto>(
            new CreateWalletCommand(user.AuthSubject(), name, institution, kind, currency, visibility, ownerPersonId), ct));

    [McpServerTool(Name = "update_wallet", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Renames a wallet and/or changes its visibility; owner, kind and currency cannot change. Send both fields and the version from get_wallet. A version conflict means it changed since: call get_wallet and decide again.")]
    public static Task<WalletDto> UpdateWallet(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Wallet id from list_wallets.")] Guid walletId,
        [Description("Display name, 1-80 characters; send the current one to keep it.")] string name,
        [Description("Shared or Private; send the current one to keep it. Changing it needs the owner or an adult managing them.")] string visibility,
        [Description("The wallet's version from get_wallet or list_wallets.")] uint version,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<UpdateWalletCommand, WalletDto>(
            new UpdateWalletCommand(user.AuthSubject(), walletId, name, visibility, version), ct));

    [McpServerTool(Name = "archive_wallet", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Archives a wallet: it leaves active lists and net worth totals; its balance history stays and restore_wallet brings it back. Repeating the call changes nothing.")]
    public static Task<WalletDto> ArchiveWallet(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Wallet id from list_wallets.")] Guid walletId,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<ArchiveWalletCommand, WalletDto>(
            new ArchiveWalletCommand(user.AuthSubject(), walletId), ct));

    [McpServerTool(Name = "restore_wallet", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Makes an archived wallet active again. Repeating the call changes nothing.")]
    public static Task<WalletDto> RestoreWallet(
        McpToolUser user,
        ICommandDispatcher commands,
        [Description("Wallet id from list_wallets with includeArchived.")] Guid walletId,
        CancellationToken ct = default)
        => McpErrors.AsToolErrorsAsync(() => commands.SendAsync<RestoreWalletCommand, WalletDto>(
            new RestoreWalletCommand(user.AuthSubject(), walletId), ct));
}
