namespace Household.Application.Commands.CreateHousehold;

using Shared.Abstractions.Cqrs;

public sealed record CreateHouseholdCommand(string RequestingAuthSubject, string Name) : ICommand<Guid>;
