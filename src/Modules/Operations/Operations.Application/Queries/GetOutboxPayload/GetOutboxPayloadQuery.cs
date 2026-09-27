namespace Operations.Application.Queries.GetOutboxPayload;

using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Messaging.Outbox;

/// <summary>Admin view: one outbox message's serialised event. May contain personal data.</summary>
/// <param name="Module">Publishing module name.</param>
/// <param name="MessageId">The outbox row.</param>
public sealed record GetOutboxPayloadQuery(string Module, Guid MessageId) : IQuery<OutboxPayload>;
