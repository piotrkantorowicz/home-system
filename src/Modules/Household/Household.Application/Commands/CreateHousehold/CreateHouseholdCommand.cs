using Shared.Abstractions.Cqrs;

namespace Household.Application.Commands.CreateHousehold;

public sealed record CreateHouseholdCommand(string RequestingAuthSubject, string Name) : ICommand<Guid>;
