namespace Operations.Application.Commands.RetryOutboxDeadLetter;

using Operations.Application.Outbox;
using Shared.Abstractions.Core.Domain;
using Shared.Abstractions.Cqrs;

internal sealed class RetryOutboxDeadLetterCommandHandler(OutboxModules modules, TimeProvider clock)
    : ICommandHandler<RetryOutboxDeadLetterCommand>
{
    public async Task HandleAsync(RetryOutboxDeadLetterCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!await modules.Store(command.Module).RetryAsync(command.MessageId, clock.GetUtcNow().UtcDateTime, ct))
            throw new NotFoundException("OutboxMessage", command.MessageId);
    }
}
