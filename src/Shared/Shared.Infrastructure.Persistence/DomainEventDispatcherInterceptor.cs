namespace Shared.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstractions.Cqrs;
using Shared.Abstractions.Core.Domain;

internal sealed class DomainEventDispatcherInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    // Dispatch BEFORE the flush so handlers can enlist new entities (e.g. outbox rows)
    // in the same SaveChanges. SavedChangesAsync would run after the flush and any new
    // ChangeTracker additions would be silently dropped.
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is not null)
            await DispatchDomainEventsAsync(eventData.Context, ct);

        return await base.SavingChangesAsync(eventData, result, ct);
    }

    private async Task DispatchDomainEventsAsync(Microsoft.EntityFrameworkCore.DbContext dbContext, CancellationToken ct)
    {
        var aggregates = dbContext.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                await (Task)handlerType
                    .GetMethod("HandleAsync")!
                    .Invoke(handler, [domainEvent, ct])!;
            }
        }
    }
}
