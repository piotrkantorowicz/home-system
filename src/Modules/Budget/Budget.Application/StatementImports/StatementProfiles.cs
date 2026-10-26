namespace Budget.Application.StatementImports;

using System.Text.RegularExpressions;

/// <summary>
/// The supported bank exports (<c>bank-import.md</c> §2.1), each checked against a real export; the
/// fixtures reproduce those layouts with synthetic data.
/// </summary>
internal static partial class StatementProfiles
{
    private const string IsoDate = "yyyy-MM-dd";

    // Revolut localises its headers and states; both languages map the same columns, in this order.
    private static readonly string[] RevolutEnglish = ["Type", "Product", "Started Date", "Completed Date", "Description", "Amount", "Fee", "Currency", "State"];
    private static readonly string[] RevolutPolish = ["Rodzaj", "Produkt", "Data rozpoczęcia", "Data zrealizowania", "Opis", "Kwota", "Opłata", "Waluta", "State"];

    /// <summary>Every profile the parser tries, in detection order.</summary>
    public static IReadOnlyList<StatementProfile> All { get; } =
    [
        new("mbank", ';', false, ["#Data księgowania", "#Data operacji", "#Opis operacji", "#Tytuł"], MapMbank, MbankPeriod),
        new("pko-bp", ';', false, ["Data operacji", "Data waluty", "Typ transakcji", "Kwota", "Waluta"], MapPko),
        new("ing", ';', false, ["Data transakcji", "Data księgowania", "Dane kontrahenta", "Tytuł"], MapIng, IngPeriod),
        new("revolut", ',', true, RevolutEnglish, Revolut(RevolutEnglish, "COMPLETED")),
        new("revolut", ',', true, RevolutPolish, Revolut(RevolutPolish, "ZAKOŃCZONO")),
        new("alior", ';', false, ["Data transakcji", "Data księgowania", "Nazwa nadawcy", "Nazwa odbiorcy", "Szczegóły transakcji"], MapAlior, AliorPeriod),
    ];

    /// <summary>mBank "Elektroniczne zestawienie operacji": booked rows only, currency and account in the preamble.</summary>
    private static StatementTransaction MapMbank(StatementRow row, string budgetCurrency)
    {
        var title = row.Text("#Tytuł");
        var purchase = MbankPurchaseDate().Match(title);
        var currency = row.Preamble("#Waluta") ?? throw new StatementRowException(StatementIssueCode.MissingColumn);

        return new StatementTransaction(
            row.LineNumber,
            row.Preamble("#Numer rachunku"),
            row.Date("#Data księgowania", IsoDate),
            (purchase.Success ? StatementRow.ParseDate(purchase.Groups[1].Value, IsoDate) : null) ?? row.Date("#Data operacji", IsoDate),
            null,
            row.Amount("#Kwota", ','),
            currency,
            currency != budgetCurrency,
            row.Text("#Nadawca/Odbiorca"),
            StatementText.Collapse(purchase.Success ? title.Remove(purchase.Index, purchase.Length) : title),
            null,
            row.Text("#Opis operacji"),
            true);
    }

    private static (DateOnly? From, DateOnly? To) MbankPeriod(IReadOnlyDictionary<string, string[]> preamble)
        => preamble.TryGetValue("#Za okres:", out var period) && period.Length >= 2
            ? (StatementRow.ParseDate(period[0], "dd.MM.yyyy"), StatementRow.ParseDate(period[1], "dd.MM.yyyy"))
            : (null, null);

    /// <summary>
    /// PKO BP iPKO history: the description is spread over unnamed <c>Key: value</c> columns. A card
    /// payment names the merchant in its location, and its <c>Tytuł</c> is only a card reference number.
    /// </summary>
    private static StatementTransaction MapPko(StatementRow row, string budgetCurrency)
    {
        var details = row.TextsFrom("Opis transakcji");
        var location = Detail(details, "Lokalizacja: Adres: ");
        var currency = row.Text("Waluta");

        return new StatementTransaction(
            row.LineNumber,
            null,
            row.Date("Data waluty", IsoDate),
            row.Date("Data operacji", IsoDate),
            null,
            row.Amount("Kwota", '.'),
            currency,
            currency != budgetCurrency,
            Detail(details, "Nazwa odbiorcy: ") ?? Detail(details, "Nazwa nadawcy: ") ?? location?.Split(" Miasto: ")[0] ?? "",
            location is null ? Detail(details, "Tytuł: ") ?? "" : "",
            null,
            row.Text("Typ transakcji"),
            true);
    }

    /// <summary>ING Bank Śląski: a pending card payment has only a blocked amount and no booking date.</summary>
    private static StatementTransaction MapIng(StatementRow row, string budgetCurrency)
    {
        const string bookedAmount = "Kwota transakcji (waluta rachunku)";
        var isBooked = row.Text(bookedAmount).Length > 0;
        var amountColumn = isBooked ? bookedAmount : "Kwota blokady/zwolnienie blokady";
        var transactionDate = row.Date("Data transakcji", IsoDate);
        var currency = row.TextAfter(amountColumn);

        return new StatementTransaction(
            row.LineNumber,
            row.Preamble("Wybrane rachunki:", ^1), // "<account name> (PLN)";"<account number>"
            isBooked ? row.Date("Data księgowania", IsoDate) : transactionDate,
            transactionDate,
            null,
            row.Amount(amountColumn, ','),
            currency,
            currency != budgetCurrency,
            row.Text("Dane kontrahenta"),
            row.Text("Tytuł"),
            row.Text("Nr transakcji") is { Length: > 0 } reference ? reference : null,
            row.Text("Szczegóły"),
            isBooked);
    }

    /// <summary>ING's <c>Zakres dat:</c>;<c>2026-10-19 - 2026-10-25</c>.</summary>
    private static (DateOnly? From, DateOnly? To) IngPeriod(IReadOnlyDictionary<string, string[]> preamble)
        => preamble.TryGetValue("Zakres dat:", out var range) && range[0].Split(" - ") is [var from, var to]
            ? (StatementRow.ParseDate(from, IsoDate), StatementRow.ParseDate(to, IsoDate))
            : (null, null);

    /// <summary>
    /// Revolut: one file holds every pocket and currency; the fee is charged on top of the amount.
    /// <paramref name="columns"/> names Type, Product, Started, Completed, Description, Amount, Fee, Currency and State.
    /// </summary>
    private static Func<StatementRow, string, StatementTransaction> Revolut(string[] columns, string completedState)
        => (row, budgetCurrency) =>
        {
            const string format = "yyyy-MM-dd HH:mm:ss";
            var started = row.DateTime(columns[2], format);
            var currency = row.Text(columns[7]);
            var isBooked = row.Text(columns[8]) == completedState;

            return new StatementTransaction(
                row.LineNumber,
                $"{row.Text(columns[1])}:{currency}",
                DateOnly.FromDateTime(isBooked ? row.DateTime(columns[3], format) : started),
                DateOnly.FromDateTime(started),
                TimeOnly.FromDateTime(started),
                row.Amount(columns[5], '.') - row.Amount(columns[6], '.'),
                currency,
                currency != budgetCurrency,
                row.Text(columns[4]),
                "",
                null,
                row.Text(columns[0]),
                isBooked);
        };

    /// <summary>
    /// Alior Bank: no account number anywhere in the file, so rows are keyed by the importer (§4.1). A card
    /// payment names the merchant only in its details; a transfer names the other party by direction.
    /// </summary>
    private static StatementTransaction MapAlior(StatementRow row, string budgetCurrency)
    {
        const string date = "dd-MM-yyyy";
        var amount = row.Amount("Kwota w walucie rachunku", ',');
        var currency = row.Text("Waluta rachunku");

        return new StatementTransaction(
            row.LineNumber,
            null,
            row.Date("Data księgowania", date),
            row.Date("Data transakcji", date),
            null,
            amount,
            currency,
            currency != budgetCurrency,
            row.Text(amount < 0 ? "Nazwa odbiorcy" : "Nazwa nadawcy"),
            row.Text("Szczegóły transakcji"),
            null,
            "",
            true);
    }

    /// <summary>Alior's first line: <c>Kryteria transakcji: Okres: 01-10-2026 - 25-10-2026</c>.</summary>
    private static (DateOnly? From, DateOnly? To) AliorPeriod(IReadOnlyDictionary<string, string[]> preamble)
        => preamble.TryGetValue("Kryteria transakcji:", out var criteria) && AliorPeriodText().Match(criteria[0]) is { Success: true } match
            ? (StatementRow.ParseDate(match.Groups[1].Value, "dd-MM-yyyy"), StatementRow.ParseDate(match.Groups[2].Value, "dd-MM-yyyy"))
            : (null, null);

    private static string? Detail(IReadOnlyList<string> details, string prefix)
        => details.FirstOrDefault(d => d.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..];

    [GeneratedRegex(@"DATA TRANSAKCJI:\s*(\d{4}-\d{2}-\d{2})")]
    private static partial Regex MbankPurchaseDate();

    [GeneratedRegex(@"Okres:\s*(\d{2}-\d{2}-\d{4})\s*-\s*(\d{2}-\d{2}-\d{4})")]
    private static partial Regex AliorPeriodText();
}
