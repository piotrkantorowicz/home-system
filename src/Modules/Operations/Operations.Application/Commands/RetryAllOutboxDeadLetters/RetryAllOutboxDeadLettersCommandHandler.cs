namespace Operations.Application.Commands.RetryAllOutboxDeadLetters;

using Microsoft.Extensions.Options;
using Operations.Application.Outbox;
using Shared.Abstractions.Cqrs;
using Shared.Infrastructure.Messaging.Outbox;

internal sealed class RetryAllOutboxDeadLettersCommandHandler(
    OutboxModules modules,
    IOptions<OutboxWorkerOptions> options,
    TimeProvider clock)
    : ICommandHandler<RetryAllOutboxDeadLettersCommand, OutboxRetryAllResult>
{
    public async Task<OutboxRetryAllResult> HandleAsync(
        RetryAllOutboxDeadLettersCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var retried = await modules.Store(command.Module).RetryAllAsync(
            options.Value.MaxAttempts, clock.GetUtcNow().UtcDateTime, RetryAllOutboxDeadLettersCommand.MaxBatch, ct);
        return new OutboxRetryAllResult(retried);
    }
}
