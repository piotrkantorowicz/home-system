namespace Assistant.Api;

using System.Collections.Concurrent;
using System.Security.Claims;

/// <summary>
/// Per-user limits: requests per minute (fixed window) and tokens per day, both on the server's clock.
/// Design: <c>docs/design/assistant/README.md</c> §8.
/// </summary>
/// <param name="options">The configured limits.</param>
/// <param name="clock">Server clock; the day and the minute window come from it.</param>
// ponytail: in memory, so a restart resets the counts (worst case one extra daily budget per restart) and
// it only holds for a single instance. Move to a small table if the host ever scales out.
internal sealed class AssistantUsage(AssistantOptions options, TimeProvider clock)
{
    private readonly ConcurrentDictionary<string, UsageCounter> _users = new(StringComparer.Ordinal);

    /// <summary>The key the limits are counted under: the caller's auth subject.</summary>
    /// <param name="user">The authenticated caller.</param>
    /// <returns>The subject.</returns>
    public static string SubjectOf(ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub")
           ?? throw new InvalidOperationException("The caller has no subject claim.");

    /// <summary>Tokens per user per day.</summary>
    public long DailyBudget => options.DailyTokenBudget;

    /// <summary>Counts one request, unless the caller is over a limit.</summary>
    /// <param name="subject">Auth subject.</param>
    /// <returns>Why the request is refused, or null when it may go ahead.</returns>
    public string? TryStartRequest(string subject)
    {
        UsageCounter counter = _users.GetOrAdd(subject, _ => new UsageCounter());
        lock (counter)
        {
            Roll(counter);
            if (counter.Tokens >= options.DailyTokenBudget)
                return "You have used today's assistant allowance. It resets tomorrow.";
            if (counter.Requests >= options.RequestsPerMinute)
                return "Too many assistant requests. Wait a minute and try again.";

            counter.Requests++;
            return null;
        }
    }

    /// <summary>True when the caller has used today's budget; checked before every model call.</summary>
    /// <param name="subject">Auth subject.</param>
    /// <returns>Whether the next model call must not happen.</returns>
    public bool IsOverBudget(string subject) => Today(subject) >= options.DailyTokenBudget;

    /// <summary>Adds the tokens one model call used.</summary>
    /// <param name="subject">Auth subject.</param>
    /// <param name="tokens">Input plus output tokens.</param>
    public void Add(string subject, long tokens)
    {
        UsageCounter counter = _users.GetOrAdd(subject, _ => new UsageCounter());
        lock (counter)
        {
            Roll(counter);
            counter.Tokens += tokens;
        }
    }

    /// <summary>Tokens the caller used today.</summary>
    /// <param name="subject">Auth subject.</param>
    /// <returns>Today's total.</returns>
    public long Today(string subject)
    {
        if (!_users.TryGetValue(subject, out UsageCounter? counter))
            return 0;

        lock (counter)
        {
            Roll(counter);
            return counter.Tokens;
        }
    }

    // A new server day clears the tokens; a new minute window clears the requests.
    private void Roll(UsageCounter counter)
    {
        DateTimeOffset now = clock.GetUtcNow();
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, clock.LocalTimeZone).DateTime);
        if (counter.Day != day)
        {
            counter.Day = day;
            counter.Tokens = 0;
        }

        if (now - counter.WindowStart >= TimeSpan.FromMinutes(1))
        {
            counter.WindowStart = now;
            counter.Requests = 0;
        }
    }
}
