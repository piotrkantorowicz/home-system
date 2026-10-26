namespace Budget.Application.StatementImports;

using System.Text.RegularExpressions;

/// <summary>
/// The supported bank exports (<c>bank-import.md</c> §2.1). <c>mbank</c> is checked against a real
/// export; the others follow each bank's documented layout until a real fixture replaces theirs.
/// </summary>
internal static partial class StatementProfiles
{
    private const string IsoDate = "yyyy-MM-dd";

    /// <summary>Every profile the parser tries, in detection order.</summary>
    public static IReadOnlyList<StatementProfile> All { get; } =
    [
        new("mbank", ';', false, ["#Data księgowania", "#Data operacji", "#Opis operacji", "#Tytuł"], MapMbank, MbankPeriod),
        new("pko-bp", ',', false, ["Data operacji", "Data waluty", "Typ transakcji", "Kwota", "Waluta"], MapPko),
        new("ing", ';', false, ["Data transakcji", "Data księgowania", "Dane kontrahenta", "Tytuł"], MapIng),
        new("revolut", ',', true, ["Type", "Product", "Started Date", "Completed Date", "Description", "Amount", "Fee", "Currency", "State"], MapRevolut),
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
            row.Preamble("Wybrane rachunki:", 2), // "Konto Direct";"(PLN)";"<account number>"
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

    /// <summary>Revolut: one file holds every pocket and currency; the fee is charged on top of the amount.</summary>
    private static StatementTransaction MapRevolut(StatementRow row, string budgetCurrency)
    {
        const string format = "yyyy-MM-dd HH:mm:ss";
        var started = row.DateTime("Started Date", format);
        var currency = row.Text("Currency");
        var isBooked = row.Text("State") == "COMPLETED";

        return new StatementTransaction(
            row.LineNumber,
            $"{row.Text("Product")}:{currency}",
            DateOnly.FromDateTime(isBooked ? row.DateTime("Completed Date", format) : started),
            DateOnly.FromDateTime(started),
            TimeOnly.FromDateTime(started),
            row.Amount("Amount", '.') - row.Amount("Fee", '.'),
            currency,
            currency != budgetCurrency,
            row.Text("Description"),
            "",
            null,
            row.Text("Type"),
            isBooked);
    }

    private static string? Detail(IReadOnlyList<string> details, string prefix)
        => details.FirstOrDefault(d => d.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..];

    [GeneratedRegex(@"DATA TRANSAKCJI:\s*(\d{4}-\d{2}-\d{2})")]
    private static partial Regex MbankPurchaseDate();
}
