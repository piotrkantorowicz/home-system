namespace Budget.Application.Queries.GetBudgetContext;

/// <summary>A household member who may pay for or share a household expense.</summary>
/// <param name="PersonId">Use as <c>paidByPersonId</c> or in <c>participantIds</c>.</param>
/// <param name="DisplayName">Current roster name.</param>
public sealed record BudgetParticipantDto(Guid PersonId, string DisplayName);
