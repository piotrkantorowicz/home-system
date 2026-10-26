namespace Budget.Application.Queries.PreviewStatementImport;

using Budget.Application.StatementImports;
using Shared.Abstractions.Cqrs;

/// <summary>
/// Parses a bank statement and classifies each row for import into one envelope. Writes nothing; the
/// file is held in memory for this request only (<c>bank-import.md</c> §6–§7).
/// </summary>
/// <param name="AuthSubject">Auth subject of the caller.</param>
/// <param name="BudgetAccountId">The envelope the rows would be imported into.</param>
/// <param name="Content">The statement file's raw bytes.</param>
public sealed record PreviewStatementImportQuery(string AuthSubject, Guid BudgetAccountId, byte[] Content) : IQuery<StatementPreviewDto>
{
    /// <summary>Largest accepted statement file.</summary>
    public const int MaxFileBytes = StatementParser.MaxFileBytes;
}
