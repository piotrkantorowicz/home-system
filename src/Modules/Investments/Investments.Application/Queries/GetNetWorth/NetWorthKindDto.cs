namespace Investments.Application.Queries.GetNetWorth;

/// <summary>PLN total of one wallet kind.</summary>
/// <param name="Kind">The kind, e.g. <c>IKE</c>.</param>
/// <param name="Total">PLN total.</param>
public sealed record NetWorthKindDto(string Kind, string Total);
