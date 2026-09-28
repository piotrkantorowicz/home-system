namespace Operations.Application.Outbox;

using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Core.Domain;
using Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// Resolves a publishing module's dead-letter store by its route name. Every module that calls
/// <c>AddOutbox&lt;TDbContext&gt;()</c> registers an <see cref="OutboxModule"/> and a keyed
/// <see cref="IOutboxDeadLetterStore"/>; Operations reads them without referencing the module.
/// </summary>
/// <param name="modules">The registered publishing modules.</param>
/// <param name="services">Resolves the keyed stores.</param>
internal sealed class OutboxModules(IEnumerable<OutboxModule> modules, IServiceProvider services)
{
    /// <summary>All publishing modules, ordered by name.</summary>
    public IEnumerable<OutboxModule> All => modules.OrderBy(m => m.Name, StringComparer.Ordinal);

    /// <summary>The store of the module with that name (case-insensitive).</summary>
    /// <param name="name">Module name from the route.</param>
    /// <exception cref="NotFoundException">No publishing module has that name.</exception>
    public IOutboxDeadLetterStore Store(string name)
    {
        var module = modules.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotFoundException("OutboxModule", name);
        return Store(module);
    }

    /// <summary>The store of a registered module.</summary>
    /// <param name="module">One of <see cref="All"/>.</param>
    public IOutboxDeadLetterStore Store(OutboxModule module)
        => services.GetRequiredKeyedService<IOutboxDeadLetterStore>(module.Key);
}
