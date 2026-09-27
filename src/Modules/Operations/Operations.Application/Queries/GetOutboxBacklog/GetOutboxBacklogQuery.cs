namespace Operations.Application.Queries.GetOutboxBacklog;

using Shared.Abstractions.Cqrs;

/// <summary>Admin view: dead-lettered and retrying outbox messages of every publishing module.</summary>
public sealed record GetOutboxBacklogQuery : IQuery<IReadOnlyList<OutboxModuleBacklog>>;
