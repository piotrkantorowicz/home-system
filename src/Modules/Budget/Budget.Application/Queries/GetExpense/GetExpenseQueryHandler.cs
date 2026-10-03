namespace Budget.Application.Queries.GetExpense;

using Budget.Application.Common;
using Budget.Application.Persistence;
using Budget.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class GetExpenseQueryHandler(BudgetAccessService access, IBudgetReadDbContext db)
    : IQueryHandler<GetExpenseQuery, ExpenseDto>
{
    public async Task<ExpenseDto> HandleAsync(GetExpenseQuery query, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        var id = ExpenseId.From(query.Id);

        var row = await db.VisibleTo(caller).Where(e => e.Id == id).Select(ExpenseReadExtensions.ToRow).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Expense", query.Id);

        var revisions = await db.ExpenseRevisions.AsNoTracking()
            .Where(r => r.ExpenseId == id)
            .OrderBy(r => r.RevisionNumber)
            .ToListAsync(ct);

        return row.ToDto(caller) with { History = [.. revisions.Select(ToDto)] };
    }

    private static ExpenseRevisionDto ToDto(Budget.Domain.Entities.ExpenseRevision r)
    {
        var s = r.Snapshot;
        return new ExpenseRevisionDto(
            r.RevisionNumber, r.Operation, r.ActorPersonId, r.ActorDisplayName, r.Reason, r.CreatedAt,
            new ExpenseSnapshotDto(
                s.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture), s.Category.ToString(), s.OccurredOn, s.Description,
                s.FundingSource.ToString(), s.PaidByPersonId, s.PaidByDisplayName, s.AddedByPersonId, s.AddedByDisplayName, s.IsVoided,
                [.. s.Shares.Select(x => new ExpenseShareDto(
                    x.PersonId, x.PersonDisplayName, x.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)))]));
    }
}
