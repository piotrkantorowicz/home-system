namespace HomeSystem.REST.Mcp;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Shared.Infrastructure.Web;

/// <summary>
/// Logs every tool call with its name, outcome, duration and the HTTP trace identifier, and records the duration in
/// <see cref="McpToolMetrics"/>. Arguments, results, notes and tokens are never logged or tagged.
/// </summary>
internal static partial class McpCallLogging
{
    public static McpRequestHandler<CallToolRequestParams, CallToolResult> Filter(McpRequestHandler<CallToolRequestParams, CallToolResult> next)
        => async (context, ct) =>
        {
            var logger = context.Services!.GetRequiredService<ILoggerFactory>().CreateLogger("HomeSystem.Mcp");
            var metrics = context.Services!.GetRequiredService<McpToolMetrics>();
            var tool = context.Params?.Name ?? "?";
            var traceId = Activity.Current?.TraceId.ToString() ?? "-";
            var started = Stopwatch.GetTimestamp();
            string outcome = "ok";
            string metricOutcome = McpToolMetrics.Success;
            try
            {
                var result = await next(context, ct);
                if (result.IsError == true)
                {
                    outcome = "error";
                    metricOutcome = McpToolMetrics.Error;
                }

                return result;
            }
            catch (Exception ex)
            {
                outcome = ex.GetType().Name;
                metricOutcome = ex is OperationCanceledException && ct.IsCancellationRequested
                    ? McpToolMetrics.Cancelled
                    : McpToolMetrics.Exception;
                throw;
            }
            finally
            {
                var elapsed = Stopwatch.GetElapsedTime(started);
                LogCall(logger, tool, outcome, elapsed.TotalMilliseconds, traceId);
                metrics.RecordCall(context.Params?.Name, metricOutcome, elapsed);
            }
        };

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "MCP tool {Tool} finished with {Outcome} in {ElapsedMs:0} ms (trace {TraceId})")]
    private static partial void LogCall(ILogger logger, string tool, string outcome, double elapsedMs, string traceId);
}
