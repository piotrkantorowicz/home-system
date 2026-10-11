namespace Investments.Infrastructure.Fx;

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Investments.Application.Fx;
using Investments.Domain.ValueObjects;

/// <summary>
/// NBP table A over a typed <see cref="HttpClient"/> (base address and 10 s timeout set at
/// registration). No retry policy: the worker's next run is the retry.
/// </summary>
internal sealed class NbpClient(HttpClient http) : INbpClient
{
    public async Task<IReadOnlyList<NbpRate>> GetRatesAsync(WalletCurrency currency, DateOnly first, DateOnly last, CancellationToken ct)
    {
        var path = string.Create(
            CultureInfo.InvariantCulture,
            $"exchangerates/rates/a/{currency.ToString().ToLowerInvariant()}/{first:yyyy-MM-dd}/{last:yyyy-MM-dd}/?format=json");

        try
        {
            using var response = await http.GetAsync(path, ct);
            // NBP answers 404 when it published nothing in the range (weekends, holidays).
            if (response.StatusCode == HttpStatusCode.NotFound)
                return [];
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<NbpSeries>(ct)
                ?? throw new NbpUnavailableException("NBP returned an empty body.");
            return [.. body.Rates.Select(r => new NbpRate(r.EffectiveDate, r.Mid, r.No))];
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException
            || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            throw new NbpUnavailableException($"NBP rates for {currency} {first:yyyy-MM-dd}..{last:yyyy-MM-dd} are unavailable.", ex);
        }
    }

    private sealed record NbpSeries(IReadOnlyList<NbpSeriesRate> Rates);

    private sealed record NbpSeriesRate(string No, DateOnly EffectiveDate, decimal Mid);
}
