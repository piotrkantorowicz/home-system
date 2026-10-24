namespace DietPlanner.Application.OperationReceipts;

using System.Security.Cryptography;
using System.Text.Json;
using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Shared.Abstractions.Core.Domain;

/// <summary>
/// Makes a keyed write retry-safe. Call it inside the command, before writing: the receipt is claimed
/// in the command's transaction, so it commits or rolls back with the data. Same key and same request
/// returns the original result; same key with a different request is a conflict.
/// </summary>
internal sealed class OperationReplay(IOperationReceiptRepository receipts)
{
    /// <summary>Claims <paramref name="key"/> for this request, or finds the result of its earlier run.</summary>
    /// <param name="actorSubject">Auth subject of the caller.</param>
    /// <param name="operation">Operation name, e.g. <c>create_product</c>.</param>
    /// <param name="key">Caller-chosen idempotency key.</param>
    /// <param name="canonicalRequest">The request fields that define "the same request"; serialized to JSON and hashed.</param>
    /// <param name="resultId">Id this run will produce if it proceeds.</param>
    /// <param name="now">Current time, UTC.</param>
    /// <param name="ct">Propagates cancellation.</param>
    /// <returns><see langword="null"/> when this run claimed the key and must write; otherwise the original result id.</returns>
    /// <exception cref="ConflictException">The key was used for a different request.</exception>
    public async Task<Guid?> ClaimOrReplayAsync(
        string actorSubject, string operation, string key, object canonicalRequest, Guid resultId, DateTime now, CancellationToken ct)
        => (await ClaimOrReplayReceiptAsync(actorSubject, operation, key, canonicalRequest, resultId, result: null, now, ct))?.ResultId;

    /// <summary>Like <see cref="ClaimOrReplayAsync"/>, for operations that store a JSON result known before they write.</summary>
    /// <param name="actorSubject">Auth subject of the caller.</param>
    /// <param name="operation">Operation name.</param>
    /// <param name="key">Caller-chosen idempotency key.</param>
    /// <param name="canonicalRequest">The request fields that define "the same request".</param>
    /// <param name="resultId">Id this run will produce if it proceeds.</param>
    /// <param name="result">JSON result to return on replay.</param>
    /// <param name="now">Current time, UTC.</param>
    /// <param name="ct">Propagates cancellation.</param>
    /// <returns><see langword="null"/> when this run claimed the key and must write; otherwise the original receipt.</returns>
    /// <exception cref="ConflictException">The key was used for a different request.</exception>
    public async Task<OperationReceipt?> ClaimOrReplayReceiptAsync(
        string actorSubject, string operation, string key, object canonicalRequest, Guid resultId, string? result, DateTime now, CancellationToken ct)
    {
        var hash = Hash(canonicalRequest);
        if (await receipts.TryClaimAsync(OperationReceipt.Create(actorSubject, operation, key, hash, resultId, now, result), ct))
            return null;

        var existing = await receipts.FindAsync(actorSubject, operation, key, ct)
            ?? throw new ConflictException($"Operation key '{key}' is in use by a concurrent request; retry it.");

        return existing.RequestHash == hash
            ? existing
            : throw new ConflictException($"Operation key '{key}' was already used for a different {operation} request. Use a new key for a new request.");
    }

    internal static string Hash(object canonicalRequest)
        => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(canonicalRequest)));
}
