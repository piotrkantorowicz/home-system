namespace DietPlanner.Application.Commands.DeleteWeightEntry;

using Shared.Abstractions.Cqrs;

public sealed record DeleteWeightEntryCommand(string UserId, Guid EntryId) : ICommand;
