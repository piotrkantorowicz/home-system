namespace Shared.Infrastructure.Cqrs.Decorators;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Cqrs;

internal sealed partial class LoggingQueryDispatcherDecorator : IQueryDispatcher
{
    private readonly IQueryDispatcher _inner;
    private readonly ILogger<LoggingQueryDispatcherDecorator> _logger;

    public LoggingQueryDispatcherDecorator(
        IQueryDispatcher inner,
        ILogger<LoggingQueryDispatcherDecorator> logger)
        => (_inner, _logger) = (inner, logger);

    public async Task<TResult> SendAsync<TQuery, TResult>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery<TResult>
    {
        var name = typeof(TQuery).Name;
        LogExecuting(name);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _inner.SendAsync<TQuery, TResult>(query, ct);
            LogExecuted(name, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            LogFailed(ex, name, sw.ElapsedMilliseconds);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing query {QueryName}")]
    private partial void LogExecuting(string queryName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Query {QueryName} executed in {ElapsedMs}ms")]
    private partial void LogExecuted(string queryName, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Query {QueryName} failed after {ElapsedMs}ms")]
    private partial void LogFailed(Exception exception, string queryName, long elapsedMs);
}
