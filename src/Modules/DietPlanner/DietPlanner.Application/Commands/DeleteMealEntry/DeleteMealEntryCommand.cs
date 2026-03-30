namespace DietPlanner.Application.Commands.DeleteMealEntry;

using Shared.Abstractions.CQRS;

public sealed record DeleteMealEntryCommand(Guid Id, string UserId) : ICommand;
