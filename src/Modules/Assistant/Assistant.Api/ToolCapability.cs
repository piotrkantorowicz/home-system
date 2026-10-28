namespace Assistant.Api;

/// <summary>What the assistant may do with a tool, derived from its scopes and annotations (design §5).</summary>
public enum ToolCapability
{
    /// <summary>Never offered to the model; a call to it is refused.</summary>
    NotOffered,

    /// <summary>Read-only: runs as soon as the model calls it.</summary>
    RunNow,

    /// <summary>Changes state: held for the user's confirmation (#648). Not offered yet.</summary>
    Hold,
}
