namespace Budget.Application.Queries.ListRepayments;

/// <summary>A recorded repayment: a household fact that someone paid someone. No money moved through the app.</summary>
/// <param name="Id">Repayment identifier.</param>
/// <param name="FromPersonId">Who paid.</param>
/// <param name="FromDisplayName">Current roster name for a member, else the stored snapshot.</param>
/// <param name="ToPersonId">Who received.</param>
/// <param name="ToDisplayName">Current roster name for a member, else the stored snapshot.</param>
/// <param name="Amount">Decimal string with two fractional digits.</param>
/// <param name="PaidOn">The date the payment was made.</param>
/// <param name="Note">Optional note.</param>
/// <param name="AddedByPersonId">Who recorded it.</param>
/// <param name="AddedByDisplayName">Current roster name for a member, else the stored snapshot.</param>
/// <param name="Revision">1, or 2 once voided; send back when voiding.</param>
/// <param name="IsVoided">Voided repayments no longer count.</param>
/// <param name="VoidedAt">When it was voided, UTC.</param>
/// <param name="VoidReason">Why it was voided.</param>
/// <param name="CreatedAt">When it was recorded, UTC.</param>
public sealed record RepaymentDto(
    Guid Id,
    Guid FromPersonId,
    string FromDisplayName,
    Guid ToPersonId,
    string ToDisplayName,
    string Amount,
    DateOnly PaidOn,
    string? Note,
    Guid AddedByPersonId,
    string AddedByDisplayName,
    int Revision,
    bool IsVoided,
    DateTime? VoidedAt,
    string? VoidReason,
    DateTime CreatedAt);
