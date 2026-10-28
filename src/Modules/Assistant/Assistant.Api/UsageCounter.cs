namespace Assistant.Api;

/// <summary>One user's counts in <see cref="AssistantUsage"/>; read and written under a lock on itself.</summary>
internal sealed class UsageCounter
{
    /// <summary>Server day <see cref="Tokens"/> belongs to.</summary>
    public DateOnly Day { get; set; }

    /// <summary>Input plus output tokens used on <see cref="Day"/>.</summary>
    public long Tokens { get; set; }

    /// <summary>Start of the current one-minute request window.</summary>
    public DateTimeOffset WindowStart { get; set; }

    /// <summary>Requests started in the current window.</summary>
    public int Requests { get; set; }
}
