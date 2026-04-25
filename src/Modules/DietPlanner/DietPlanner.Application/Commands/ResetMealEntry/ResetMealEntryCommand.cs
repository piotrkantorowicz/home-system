namespace DietPlanner.Application.Commands.ResetMealEntry;

using Shared.Abstractions.Cqrs;

public sealed record ResetMealEntryCommand(Guid Id, string UserId) : ICommand;
