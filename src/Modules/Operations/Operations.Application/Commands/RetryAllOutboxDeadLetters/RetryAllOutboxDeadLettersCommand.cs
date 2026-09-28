namespace Operations.Application.Commands.RetryAllOutboxDeadLetters;

using Shared.Abstractions.Cqrs;

/// <summary>
/// Admin action: retries every dead-lettered message of one module (up to <see cref="MaxBatch"/>
/// per call), each exactly like <c>RetryOutboxDeadLetterCommand</c>.
/// </summary>
/// <param name="Module">Publishing module name.</param>
public sealed record RetryAllOutboxDeadLettersCommand(string Module) : ICommand<OutboxRetryAllResult>
{
    /// <summary>Most messages retried by one call; a larger backlog needs another call.</summary>
    public const int MaxBatch = 500;
}
