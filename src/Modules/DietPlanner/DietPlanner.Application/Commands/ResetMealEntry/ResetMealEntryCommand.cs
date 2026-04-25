namespace DietPlanner.Application.Commands.ResetMealEntry;

using Shared.Abstractions.CQRS;

public sealed record ResetMealEntryCommand(Guid Id, string UserId) : ICommand;
