namespace Household.Application.Queries.GetCurrentPerson;

using Shared.Abstractions.Cqrs;

public sealed record GetCurrentPersonQuery(string AuthSubject) : IQuery<CurrentPersonDto?>;
