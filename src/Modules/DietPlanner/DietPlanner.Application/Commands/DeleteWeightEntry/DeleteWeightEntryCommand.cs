namespace DietPlanner.Application.Commands.DeleteWeightEntry;

using Shared.Abstractions.CQRS;

public sealed record DeleteWeightEntryCommand(string UserId, Guid EntryId) : ICommand;
