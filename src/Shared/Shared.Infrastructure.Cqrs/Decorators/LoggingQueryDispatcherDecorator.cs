namespace Shared.Infrastructure.Cqrs.Decorators;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared.Abstractions.Cqrs;

internal sealed class LoggingQueryDispatcherDecorator : IQueryDispatcher
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
        _logger.LogInformation("Executing query {QueryName}", name);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await _inner.SendAsync<TQuery, TResult>(query, ct);
            _logger.LogInformation("Query {QueryName} executed in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query {QueryName} failed after {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
