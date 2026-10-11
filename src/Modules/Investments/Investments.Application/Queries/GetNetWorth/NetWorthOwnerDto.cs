namespace Investments.Application.Queries.GetNetWorth;

/// <summary>PLN total of one owner's wallets.</summary>
/// <param name="PersonId">The owner.</param>
/// <param name="Name">Their current name, or the stored snapshot.</param>
/// <param name="Total">PLN total.</param>
public sealed record NetWorthOwnerDto(Guid PersonId, string Name, string Total);
