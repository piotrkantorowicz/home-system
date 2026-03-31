namespace Shared.Infrastructure.CQRS.Decorators;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.CQRS;

internal sealed class LoggingCommandDispatcherDecorator : ICommandDispatcher
{
    private readonly ICommandDispatcher _inner;
    private readonly ILogger<LoggingCommandDispatcherDecorator> _logger;

    public LoggingCommandDispatcherDecorator(
        ICommandDispatcher inner,
        ILogger<LoggingCommandDispatcherDecorator> logger)
        => (_inner, _logger) = (inner, logger);

    public async Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand
    {
        var name = typeof(TCommand).Name;
        _logger.LogInformation("Executing command {CommandName}", name);
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.SendAsync(command, ct);
            _logger.LogInformation("Command {CommandName} executed in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command {CommandName} failed after {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>
    {
        var name = typeof(TCommand).Name;
        _logger.LogInformation("Executing command {CommandName}", name);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _inner.SendAsync<TCommand, TResult>(command, ct);
            _logger.LogInformation("Command {CommandName} executed in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command {CommandName} failed after {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
