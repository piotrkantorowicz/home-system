namespace DietPlanner.Application.Commands.CompleteMealEntry;

using Shared.Abstractions.CQRS;

public sealed record CompleteMealEntryCommand(Guid Id, string UserId) : ICommand;
