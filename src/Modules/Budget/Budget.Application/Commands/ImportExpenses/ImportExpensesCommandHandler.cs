namespace Budget.Application.Commands.ImportExpenses;

using Budget.Application.Common;
using Budget.Application.StatementImports;
using Budget.Domain.Abstractions;
using Budget.Domain.Aggregates;
using Budget.Domain.Exceptions;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Creates every row's expense in one commit, or none. Replay needs no receipt table: each row's revision 1
/// carries a request ID derived from the import's (<see cref="StatementRowRequestId"/>), so a retry finds them.
/// </summary>
internal sealed class ImportExpensesCommandHandler(
    BudgetAccessService access,
    IBudgetRepository budgets,
    IBudgetAccountRepository accounts,
    IExpenseRepository expenses,
    IBudgetUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<ImportExpensesCommand, ImportExpensesResult>
{
    public async Task<ImportExpensesResult> HandleAsync(ImportExpensesCommand command, CancellationToken ct = default)
    {
        var caller = await access.RequireAccessAsync(command.AuthSubject, ct);
        var account = await accounts.LoadManageableAsync(budgets, caller, command.AccountId, ct);

        // Shape was checked by ImportExpensesCommandValidator before the handler runs.
        var rows = command.Rows.Select(row => Normalize(account, row, StatementRowRequestId.For(command.ClientRequestId, row.SourceKey))).ToList();

        // Authorised above; now recognise a retry before any current-roster, archive or duplicate validation.
        await expenses.LockRequestAsync(account.BudgetId, caller.PersonId, command.ClientRequestId, ct);
        var previous = (await expenses.FindRevisionsByRequestsAsync(account.BudgetId, caller.PersonId, [.. rows.Select(r => r.RequestId)], ct))
            .ToDictionary(r => r.ClientRequestId);
        if (previous.Count > 0)
        {
            return rows.All(r => previous.TryGetValue(r.RequestId, out var p) && p.Request.Matches(r.Request))
                ? new ImportExpensesResult([.. rows.Select(r => previous[r.RequestId].ExpenseId.Value)], Created: false)
                : throw new ConflictException("This request ID was already used for a different request.");
        }

        // An envelope problem, not a row one: checked here so the per-row catch below never labels it Rows[0].
        if (account.IsArchived)
            throw new BudgetDomainException(Expense.ArchivedEnvelopeMessage);

        var scope = Expense.ImportScopeOf(account.Visibility, account.OwnerPersonId);
        await expenses.LockImportScopeAsync(account.BudgetId, scope, ct);
        if (await expenses.AnyImportedAsync(account.BudgetId, scope, [.. rows.Select(r => r.Row.SourceKey)], ct))
            throw new ConflictException("Some of these rows were imported since the preview. Preview the statement again.");

        var now = clock.GetUtcNow().UtcDateTime;
        List<Guid> ids = new(rows.Count);
        for (var i = 0; i < rows.Count; i++)
        {
            var (row, amount, requestId, request) = rows[i];
            Expense expense;
            try
            {
                var (paidBy, participants) = ExpenseParties.Resolve(caller, account, request.FundingSource, row.PaidByPersonId, row.ParticipantIds);
                expense = Expense.Create(
                    ExpenseId.New(), account, amount,
                    request.Category, row.OccurredOn, request.FundingSource, paidBy, caller.Self, participants,
                    requestId, now, request.Description, row.SourceKey);
            }
            catch (BudgetDomainException e)
            {
                // Same 0-based Rows[i] the validator uses, so the UI highlights one row for either error.
                throw new BudgetDomainException($"Rows[{i}]: {e.Message}");
            }

            await expenses.AddAsync(expense, ct);
            ids.Add(expense.Id.Value);
        }

        await unitOfWork.CommitAsync(ct);
        return new ImportExpensesResult(ids, Created: true);
    }

    /// <summary>The row's normalised request, as stored on its revision and compared on replay.</summary>
    private static (ImportExpenseRow Row, Money Amount, Guid RequestId, ExpenseRequest Request) Normalize(BudgetAccount account, ImportExpenseRow row, Guid requestId)
    {
        if (!Money.TryParsePositive(row.Amount, out var amount))
            throw new InvalidOperationException("Unvalidated amount.");
        var funding = row.FundingSource is null
            ? FundingSource.Individual
            : Enum.Parse<FundingSource>(row.FundingSource.Trim(), ignoreCase: true);
        var payerId = account.Visibility == AccountVisibility.Personal ? account.OwnerPersonId : row.PaidByPersonId;

        return (row, amount, requestId, new ExpenseRequest(
            "Import", account.Id.Value, amount.ToString(), Enum.Parse<ExpenseCategory>(row.Category.Trim(), ignoreCase: true),
            row.OccurredOn, funding, payerId, [.. (row.ParticipantIds ?? []).Order()], Description: Expense.NormalizeDescription(row.Description)));
    }
}
