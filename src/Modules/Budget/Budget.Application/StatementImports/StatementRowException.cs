namespace Budget.Application.StatementImports;

/// <summary>A data row failed to parse; the parser turns it into a <see cref="StatementIssue"/> and moves on.</summary>
internal sealed class StatementRowException(StatementIssueCode code) : Exception(code.ToString())
{
    /// <summary>Why the row failed.</summary>
    public StatementIssueCode Code { get; } = code;
}
