namespace DietPlanner.Application.Commands.CompleteMealEntry;

using Shared.Abstractions.Cqrs;

public sealed record CompleteMealEntryCommand(Guid Id, string UserId) : ICommand;
