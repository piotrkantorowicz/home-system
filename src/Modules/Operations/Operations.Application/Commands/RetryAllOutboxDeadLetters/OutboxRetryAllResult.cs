namespace Operations.Application.Commands.RetryAllOutboxDeadLetters;

/// <summary>Outcome of <see cref="RetryAllOutboxDeadLettersCommand"/>.</summary>
/// <param name="Retried">How many messages were put back in the worker's queue.</param>
public sealed record OutboxRetryAllResult(int Retried);
