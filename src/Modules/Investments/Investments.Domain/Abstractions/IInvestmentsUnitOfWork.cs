namespace Investments.Domain.Abstractions;

using Shared.Abstractions.Core.Domain;

/// <summary>
/// Module-scoped unit of work, bound to <c>InvestmentsDbContext</c>. A second Style-1 module must not
/// rebind the global <see cref="IUnitOfWork"/> (the first EF module owns that).
/// </summary>
public interface IInvestmentsUnitOfWork : IUnitOfWork
{
}
