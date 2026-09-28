namespace Operations.Application.Queries.GetOutboxPayload;

using Operations.Application.Outbox;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Messaging.Outbox;

internal sealed class GetOutboxPayloadQueryHandler(OutboxModules modules)
    : IQueryHandler<GetOutboxPayloadQuery, OutboxPayload>
{
    public async Task<OutboxPayload> HandleAsync(GetOutboxPayloadQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await modules.Store(query.Module).GetPayloadAsync(query.MessageId, ct)
            ?? throw new NotFoundException("OutboxMessage", query.MessageId);
    }
}
