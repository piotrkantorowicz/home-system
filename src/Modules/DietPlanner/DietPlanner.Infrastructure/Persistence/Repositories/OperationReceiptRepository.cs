namespace DietPlanner.Infrastructure.Persistence.Repositories;

using DietPlanner.Domain.Ledgers;
using DietPlanner.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

internal sealed class OperationReceiptRepository(DietPlannerDbContext dbContext) : IOperationReceiptRepository
{
    public async Task<bool> TryClaimAsync(OperationReceipt receipt, CancellationToken ct = default)
        => await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO operation_receipts (actor_subject, operation, key, request_hash, result_id, result, created_at)
            VALUES ({receipt.ActorSubject}, {receipt.Operation}, {receipt.Key}, {receipt.RequestHash}, {receipt.ResultId}, CAST({receipt.Result} AS jsonb), {receipt.CreatedAt})
            ON CONFLICT DO NOTHING
            """, ct) == 1;

    public async Task<OperationReceipt?> FindAsync(string actorSubject, string operation, string key, CancellationToken ct = default)
        => await dbContext.Set<OperationReceipt>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ActorSubject == actorSubject && x.Operation == operation && x.Key == key, ct);
}
