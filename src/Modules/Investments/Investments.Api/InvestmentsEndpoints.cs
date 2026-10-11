namespace Investments.Api;

using System.Security.Claims;
using System.Text.Json.Serialization;
using Investments.Application.Commands.ArchiveWallet;
using Investments.Application.Commands.CreateWallet;
using Investments.Application.Commands.DeleteSnapshot;
using Investments.Application.Commands.RecordSnapshot;
using Investments.Application.Commands.RestoreWallet;
using Investments.Application.Commands.UpdateWallet;
using Investments.Application.Queries.GetWallet;
using Investments.Application.Queries.ListSnapshots;
using Investments.Application.Queries.ListWallets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;

internal static class InvestmentsEndpoints
{
    internal static IEndpointRouteBuilder MapInvestmentsEndpointsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/investments")
            .RequireAuthorization()
            .WithTags("Investments");

        group.MapGet("/wallets", ListWallets)
            .WithName("ListInvestmentWallets")
            .WithSummary("Page the wallets the caller may see; archived ones only with includeArchived=true");
        group.MapPost("/wallets", CreateWallet)
            .WithName("CreateInvestmentWallet")
            .WithSummary("Create a wallet for the caller or a managed member");
        group.MapGet("/wallets/{id:guid}", GetWallet)
            .WithName("GetInvestmentWallet")
            .WithSummary("Get one visible wallet");
        group.MapPut("/wallets/{id:guid}", UpdateWallet)
            .WithName("UpdateInvestmentWallet")
            .WithSummary("Rename and/or change visibility with the expected version; owner, kind and currency cannot change");
        group.MapPost("/wallets/{id:guid}/archive", ArchiveWallet)
            .WithName("ArchiveInvestmentWallet")
            .WithSummary("Hide a wallet from active lists and totals; history stays");
        group.MapPost("/wallets/{id:guid}/restore", RestoreWallet)
            .WithName("RestoreInvestmentWallet")
            .WithSummary("Make an archived wallet active again");

        group.MapGet("/wallets/{id:guid}/snapshots", ListSnapshots)
            .WithName("ListInvestmentSnapshots")
            .WithSummary("Page a visible wallet's balances, newest first, each with the previous balance");
        group.MapPut("/wallets/{id:guid}/snapshots/{asOf}", RecordSnapshot)
            .WithName("RecordInvestmentSnapshot")
            .WithSummary("Record a wallet's balance for a date (YYYY-MM-DD); the same date replaces amount and note");
        group.MapDelete("/wallets/{id:guid}/snapshots/{asOf}", DeleteSnapshot)
            .WithName("DeleteInvestmentSnapshot")
            .WithSummary("Remove a wallet's balance for a date");

        // Catch-all so the whole prefix answers 401 to anonymous callers and 404 to signed-in ones
        // for routes that do not exist; real endpoints always win over a fallback.
        group.MapFallback(() => TypedResults.NotFound())
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<Ok<PagedList<WalletDto>>> ListWallets(
        ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] bool includeArchived = false)
        => TypedResults.Ok(await dispatcher.SendAsync<ListWalletsQuery, PagedList<WalletDto>>(
            new ListWalletsQuery(Sub(user), page, pageSize, includeArchived), ct));

    private static async Task<Ok<WalletDto>> GetWallet(
        Guid id, ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<GetWalletQuery, WalletDto>(new GetWalletQuery(Sub(user), id), ct));

    private static async Task<Created<WalletDto>> CreateWallet(
        CreateWalletRequest request, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        var wallet = await dispatcher.SendAsync<CreateWalletCommand, WalletDto>(
            new CreateWalletCommand(
                Sub(user), request.Name, request.Institution, request.Kind, request.Currency, request.Visibility, request.OwnerPersonId), ct);
        return TypedResults.Created($"/api/investments/wallets/{wallet.Id}", wallet);
    }

    private static async Task<Ok<WalletDto>> UpdateWallet(
        Guid id, UpdateWalletRequest request, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<UpdateWalletCommand, WalletDto>(
            new UpdateWalletCommand(Sub(user), id, request.Name, request.Visibility, request.Version), ct));

    private static async Task<Ok<WalletDto>> ArchiveWallet(
        Guid id, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<ArchiveWalletCommand, WalletDto>(new ArchiveWalletCommand(Sub(user), id), ct));

    private static async Task<Ok<WalletDto>> RestoreWallet(
        Guid id, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
        => TypedResults.Ok(await dispatcher.SendAsync<RestoreWalletCommand, WalletDto>(new RestoreWalletCommand(Sub(user), id), ct));

    private static async Task<Ok<PagedList<SnapshotDto>>> ListSnapshots(
        Guid id, ClaimsPrincipal user, IQueryDispatcher dispatcher, CancellationToken ct,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => TypedResults.Ok(await dispatcher.SendAsync<ListSnapshotsQuery, PagedList<SnapshotDto>>(
            new ListSnapshotsQuery(Sub(user), id, page, pageSize), ct));

    private static async Task<Results<Created<RecordSnapshotResult>, Ok<RecordSnapshotResult>>> RecordSnapshot(
        Guid id, DateOnly asOf, RecordSnapshotRequest request, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        var result = await dispatcher.SendAsync<RecordSnapshotCommand, RecordSnapshotResult>(
            new RecordSnapshotCommand(Sub(user), id, asOf, request.Amount, request.Note), ct);

        return result.Created
            ? TypedResults.Created($"/api/investments/wallets/{id}/snapshots/{asOf:yyyy-MM-dd}", result)
            : TypedResults.Ok(result);
    }

    private static async Task<NoContent> DeleteSnapshot(
        Guid id, DateOnly asOf, ClaimsPrincipal user, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        await dispatcher.SendAsync(new DeleteSnapshotCommand(Sub(user), id, asOf), ct);
        return TypedResults.NoContent();
    }

    private static string Sub(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")
           ?? throw new UnauthorizedAccessException("Missing subject claim.");
}

/// <summary>
/// Body of <c>POST /api/investments/wallets</c>. Unknown members are rejected with 400: the server
/// decides who is acting.
/// </summary>
/// <param name="Name">Display name, 1–80 characters.</param>
/// <param name="Institution"><c>XTB</c>, <c>Saxo</c>, <c>Bossa</c>, <c>MBank</c>, <c>PKOBP</c>, <c>Allianz</c> or <c>Other</c>.</param>
/// <param name="Kind"><c>Brokerage</c>, <c>IKE</c>, <c>IKZE</c>, <c>PPK</c>, <c>TreasuryBonds</c>, <c>Savings</c> or <c>Cash</c>.</param>
/// <param name="Currency"><c>PLN</c>, <c>EUR</c> or <c>USD</c>.</param>
/// <param name="Visibility"><c>Shared</c> or <c>Private</c>.</param>
/// <param name="OwnerPersonId">The caller (default) or a managed member.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record CreateWalletRequest(
    string Name, string Institution, string Kind, string Currency, string Visibility, Guid? OwnerPersonId);

/// <summary>Body of <c>PUT /api/investments/wallets/{id}</c>; owner, kind and currency cannot be sent.</summary>
/// <param name="Name">New display name.</param>
/// <param name="Visibility"><c>Shared</c> or <c>Private</c>.</param>
/// <param name="Version">The version the client last saw.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record UpdateWalletRequest(string Name, string Visibility, uint Version);

/// <summary>Body of <c>PUT /api/investments/wallets/{id}/snapshots/{asOf}</c>.</summary>
/// <param name="Amount">Decimal string, not negative, at most two decimals, e.g. <c>"12450.10"</c>.</param>
/// <param name="Note">Optional note, at most 200 characters.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record RecordSnapshotRequest(string Amount, string? Note);
