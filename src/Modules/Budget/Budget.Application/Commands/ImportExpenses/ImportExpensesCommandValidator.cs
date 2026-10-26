namespace Budget.Application.Commands.ImportExpenses;

using Budget.Application.Commands.CreateExpense;
using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

/// <summary>Shape checks per row, named <c>Rows[i].Field</c>, so one bad row rejects the import with its position.</summary>
internal sealed class ImportExpensesCommandValidator : ICommandValidator<ImportExpensesCommand>
{
    public IEnumerable<ValidationError> Validate(ImportExpensesCommand command)
    {
        if (command.ClientRequestId == Guid.Empty)
            yield return new ValidationError(nameof(command.ClientRequestId), "A client request ID is required.");

        if (command.AccountId == Guid.Empty)
            yield return new ValidationError(nameof(command.AccountId), "An envelope is required.");

        if (command.Rows is not { Count: > 0 and <= ImportExpensesCommand.MaxRows })
        {
            yield return new ValidationError(nameof(command.Rows), $"Select between 1 and {ImportExpensesCommand.MaxRows} rows.");
            yield break;
        }

        HashSet<string> keys = [];
        for (var i = 0; i < command.Rows.Count; i++)
        {
            var row = command.Rows[i];
            var at = $"{nameof(command.Rows)}[{i}]";

            if (!IsSourceKey(row.SourceKey))
                yield return new ValidationError($"{at}.{nameof(row.SourceKey)}", "A source key is 64 lowercase hex characters.");
            else if (!keys.Add(row.SourceKey))
                yield return new ValidationError($"{at}.{nameof(row.SourceKey)}", "A statement row can only be imported once per request.");

            if (!Money.TryParsePositive(row.Amount, out _))
                yield return new ValidationError($"{at}.{nameof(row.Amount)}", "Amount must be a positive number with at most two decimals, up to 9999999999.99.");

            if (!CreateExpenseCommandValidator.TryParseCategory(row.Category, out _))
                yield return new ValidationError($"{at}.{nameof(row.Category)}", "Unknown category.");

            if (row.FundingSource is not null && !CreateExpenseCommandValidator.TryParseFunding(row.FundingSource, out _))
                yield return new ValidationError($"{at}.{nameof(row.FundingSource)}", "Funding source must be Individual or HouseholdFunds.");

            if (row.ParticipantIds is { } ids && ids.Distinct().Count() != ids.Count)
                yield return new ValidationError($"{at}.{nameof(row.ParticipantIds)}", "A participant can only be listed once.");

            if (!CreateExpenseCommandValidator.ValidDescription(row.Description))
                yield return new ValidationError($"{at}.{nameof(row.Description)}", "A description can be at most 80 characters.");

            if (row.MerchantKey is { } merchant && (string.IsNullOrWhiteSpace(merchant) || merchant.Length > CategoryRule.MaxMerchantKeyLength))
                yield return new ValidationError($"{at}.{nameof(row.MerchantKey)}", $"A merchant key is 1 to {CategoryRule.MaxMerchantKeyLength} characters.");
        }
    }

    private static bool IsSourceKey(string? key) => key is { Length: 64 } && key.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
