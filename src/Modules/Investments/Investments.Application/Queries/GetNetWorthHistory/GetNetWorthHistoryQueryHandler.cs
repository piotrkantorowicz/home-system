namespace Investments.Application.Queries.GetNetWorthHistory;

using System.Globalization;
using Investments.Application.Common;
using Investments.Application.Persistence;
using Investments.Domain.NetWorth;
using Shared.Abstractions.Cqrs;

internal sealed class GetNetWorthHistoryQueryHandler(InvestmentsAccessService access, IInvestmentsReadDbContext db, TimeProvider clock)
    : IQueryHandler<GetNetWorthHistoryQuery, NetWorthHistoryDto>
{
    private const int MaxYears = 10;

    public async Task<NetWorthHistoryDto> HandleAsync(GetNetWorthHistoryQuery query, CancellationToken ct = default)
    {
        var scope = NetWorthScope.Parse(query.Scope, nameof(GetNetWorthHistoryQuery));
        var today = HouseholdCalendar.Today(clock);
        var to = query.To ?? today;
        var from = query.From ?? to.AddMonths(-12);
        Validate(from, to, today);

        var caller = await access.RequireAccessAsync(query.AuthSubject, ct);
        var (calculator, _) = await db.LoadNetWorthAsync(caller, scope, from, to, ct);

        return new NetWorthHistoryDto(
            scope,
            [.. NetWorthCalculator.MonthEnds(from, to)
                .Select(calculator.ValueAt)
                .Select(p => new NetWorthHistoryPointDto(p.Date, p.Total.ToString("F2", CultureInfo.InvariantCulture), p.Incomplete))]);
    }

    private static void Validate(DateOnly from, DateOnly to, DateOnly today)
    {
        List<ValidationError> errors = [];
        if (to > today)
            errors.Add(new ValidationError("To", "The range can't end in the future."));
        if (from > to)
            errors.Add(new ValidationError("From", "From must not be after To."));
        if (from < to.AddYears(-MaxYears))
            errors.Add(new ValidationError("From", $"The range is limited to {MaxYears} years."));
        if (errors.Count > 0)
            throw new CommandValidationException(nameof(GetNetWorthHistoryQuery), errors);
    }
}
