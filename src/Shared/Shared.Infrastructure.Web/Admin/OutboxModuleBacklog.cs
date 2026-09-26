namespace Shared.Infrastructure.Web.Admin;

/// <summary>Undelivered outbox messages of one publishing module.</summary>
/// <param name="Module">Module name, used in the per-module admin routes.</param>
/// <param name="DeadLettered">Messages that used up their attempts; only a retry moves them.</param>
/// <param name="Retrying">Messages that failed at least once and are still being retried.</param>
public sealed record OutboxModuleBacklog(string Module, int DeadLettered, int Retrying);
