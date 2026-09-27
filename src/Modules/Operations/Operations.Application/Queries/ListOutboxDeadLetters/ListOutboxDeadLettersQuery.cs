namespace Operations.Application.Queries.ListOutboxDeadLetters;

using Shared.Abstractions.Core.Pagination;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Messaging.Outbox;

/// <summary>Admin view: pages through one module's dead-lettered outbox messages, oldest first.</summary>
/// <param name="Module">Publishing module name.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page (capped at 100).</param>
public sealed record ListOutboxDeadLettersQuery(string Module, int Page = 1, int PageSize = 20)
    : IQuery<PagedList<OutboxDeadLetter>>;
