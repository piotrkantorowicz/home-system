namespace Shared.Web.IntegrationTests.Infrastructure;

using Shared.Abstractions.Cqrs;

/// <summary>Does nothing; only the dispatch span matters.</summary>
internal sealed class PingCommandHandler : ICommandHandler<PingCommand>
{
    public Task HandleAsync(PingCommand command, CancellationToken ct = default) => Task.CompletedTask;
}
