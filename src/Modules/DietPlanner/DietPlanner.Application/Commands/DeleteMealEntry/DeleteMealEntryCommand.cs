namespace DietPlanner.Application.Commands.DeleteMealEntry;

using Shared.Abstractions.Cqrs;

public sealed record DeleteMealEntryCommand(Guid Id, string UserId) : ICommand;
