namespace Household.Domain.Abstractions;

using Shared.Abstractions.Core.Domain;

/// <summary>
/// Module-scoped unit of work. A second Style-1 module must not rebind the global
/// <see cref="IUnitOfWork"/> (the first EF module owns that); handlers in this module
/// depend on this abstraction instead. Bound to <c>HouseholdDbContext</c> in infrastructure DI.
/// </summary>
public interface IHouseholdUnitOfWork : IUnitOfWork
{
}
