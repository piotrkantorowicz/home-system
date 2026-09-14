namespace Shared.Infrastructure.Cqrs.Decorators;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Cqrs;

internal sealed partial class LoggingCommandDispatcherDecorator : ICommandDispatcher
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
        LogExecuting(name);
        var sw = Stopwatch.StartNew();
        try
        {
            await _inner.SendAsync(command, ct);
            LogExecuted(name, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            LogFailed(ex, name, sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand<TResult>
    {
        var name = typeof(TCommand).Name;
        LogExecuting(name);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _inner.SendAsync<TCommand, TResult>(command, ct);
            LogExecuted(name, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            LogFailed(ex, name, sw.ElapsedMilliseconds);
            throw;
        }
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Executing command {CommandName}")]
    private partial void LogExecuting(string commandName);

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Command {CommandName} executed in {ElapsedMs}ms")]
    private partial void LogExecuted(string commandName, long elapsedMs);

    [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "Command {CommandName} failed after {ElapsedMs}ms")]
    private partial void LogFailed(Exception exception, string commandName, long elapsedMs);
}
