namespace Shared.Infrastructure.Web.Admin;

/// <summary>Outcome of retrying all of a module's dead-lettered outbox messages.</summary>
/// <param name="Retried">How many messages were put back in the worker's queue.</param>
public sealed record OutboxRetryAllResult(int Retried);
