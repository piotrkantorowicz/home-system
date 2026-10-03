namespace Budget.Application.Commands.CreateExpense;

using Budget.Domain.Aggregates;
using Budget.Domain.ValueObjects;
using Shared.Abstractions.Cqrs;

internal sealed class CreateExpenseCommandValidator : ICommandValidator<CreateExpenseCommand>
{
    public IEnumerable<ValidationError> Validate(CreateExpenseCommand command)
    {
        if (command.ClientRequestId == Guid.Empty)
            yield return new ValidationError(nameof(command.ClientRequestId), "A client request ID is required.");

        if (command.AccountId == Guid.Empty)
            yield return new ValidationError(nameof(command.AccountId), "An envelope is required.");

        if (!Money.TryParsePositive(command.Amount, out _))
            yield return new ValidationError(nameof(command.Amount), "Amount must be a positive number with at most two decimals, up to 9999999999.99.");

        if (!TryParseCategory(command.Category, out _))
            yield return new ValidationError(nameof(command.Category), "Unknown category.");

        if (command.FundingSource is not null && !TryParseFunding(command.FundingSource, out _))
            yield return new ValidationError(nameof(command.FundingSource), "Funding source must be Individual or HouseholdFunds.");

        if (command.ParticipantIds is { } ids && ids.Distinct().Count() != ids.Count)
            yield return new ValidationError(nameof(command.ParticipantIds), "A participant can only be listed once.");

        if (!ValidDescription(command.Description))
            yield return new ValidationError(nameof(command.Description), $"A description can be at most {Expense.MaxDescriptionLength} characters.");
    }

    internal static bool ValidDescription(string? description)
        => Expense.TryNormalizeDescription(description, out _);

    internal static bool TryParseCategory(string? value, out ExpenseCategory category)
        => TryParseEnum(value, out category);

    internal static bool TryParseFunding(string? value, out FundingSource funding)
        => TryParseEnum(value, out funding);

    private static bool TryParseEnum<T>(string? value, out T result) where T : struct, Enum
        => Enum.TryParse(value?.Trim(), ignoreCase: true, out result)
           && Enum.IsDefined(result)
           && value!.Trim().All(char.IsLetter); // rejects numeric enum values such as "1"
}
