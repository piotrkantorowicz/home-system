namespace Budget.UnitTests.Application.StatementImports;

using System.Text;
using global::Budget.Application.StatementImports;
using global::Budget.Domain.Exceptions;
using global::Budget.Domain.ValueObjects;

/// <summary>#670 — bank statement exports parse to normalised transactions; bad rows become issues.</summary>
public sealed class StatementParserTests
{
    private const string MbankAccount = "00 1140 0000 0000 0000 0000 0001";

    private static StatementParseResult ParseFixture(string name)
        => StatementParser.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Application", "StatementImports", "Fixtures", name)), BudgetCurrency.PLN);

    private static StatementParseResult ParseText(string text)
        => StatementParser.Parse(Encoding.UTF8.GetBytes(text), BudgetCurrency.PLN);

    private static string MbankFile(params string[] rows)
        => string.Join("\n", ["#Waluta;", "PLN;", "#Data księgowania;#Data operacji;#Opis operacji;#Tytuł;#Nadawca/Odbiorca;#Numer konta;#Kwota;#Saldo po operacji;", .. rows]);

    /// <summary>Windows-1250, CRLF, a ~37-line preamble and a footer: card dates come from the title, thousands separators are spaces.</summary>
    [Fact]
    public void Parse_MbankFixture_ReturnsBookedTransactions()
    {
        var result = ParseFixture("mbank.csv");

        result.Profile.ShouldBe("mbank");
        result.PeriodFrom.ShouldBe(new DateOnly(2026, 10, 19));
        result.PeriodTo.ShouldBe(new DateOnly(2026, 10, 25));
        result.Issues.ShouldBeEmpty();
        result.Transactions.ShouldBe(
        [
            new(39, MbankAccount, new(2026, 10, 19), new(2026, 10, 18), null, -438.02m, "PLN", false, "", "APTEKA TESTOWA /BIALYSTOK", null, "ZAKUP PRZY UŻYCIU KARTY", true),
            new(40, MbankAccount, new(2026, 10, 21), new(2026, 10, 20), null, -29.38m, "PLN", false, "", "SKLEP SPOŻYWCZY 12 /GLIWICE", null, "ZAKUP PRZY UŻYCIU KARTY", true),
            new(41, MbankAccount, new(2026, 10, 22), new(2026, 10, 22), null, -5636.78m, "PLN", false, "", "KARTA WORLD MASTERCARD INTENSI", null, "RĘCZNA SPŁATA KARTY KREDYT.", true),
            new(42, MbankAccount, new(2026, 10, 23), new(2026, 10, 23), null, -45.99m, "PLN", false, "", "MUZYKA ONLINE P000000000 /Stockholm", null, "ZAKUP PRZY UŻYCIU KARTY", true),
        ]);
    }

    /// <summary>The card merchant comes from the location column; a transfer keeps payee and title; EUR is flagged.</summary>
    [Fact]
    public void Parse_PkoFixture_ReadsUnnamedDescriptionColumns()
    {
        var result = ParseFixture("pko.csv");

        result.Profile.ShouldBe("pko-bp");
        result.Issues.ShouldBeEmpty();
        result.Transactions.ShouldBe(
        [
            new(2, null, new(2026, 10, 21), new(2026, 10, 20), null, -25.50m, "PLN", false, "SKLEP TESTOWY", "", null, "Płatność kartą", true),
            new(3, null, new(2026, 10, 22), new(2026, 10, 22), null, -1200.00m, "PLN", false, "JAN TESTOWY", "CZYNSZ PAŹDZIERNIK", null, "Przelew z rachunku", true),
            new(4, null, new(2026, 10, 24), new(2026, 10, 23), null, -10.00m, "EUR", true, "CAFE TEST", "", null, "Płatność kartą", true),
        ]);
    }

    /// <summary>ING carries its own transaction number and the account in the preamble; a pending card payment has only a blocked amount.</summary>
    [Fact]
    public void Parse_IngFixture_KeepsReferenceAndMarksBlockedRows()
    {
        var result = ParseFixture("ing.csv");

        result.Profile.ShouldBe("ing");
        result.PeriodFrom.ShouldBe(new DateOnly(2026, 10, 19));
        result.PeriodTo.ShouldBe(new DateOnly(2026, 10, 25));
        result.Issues.ShouldBeEmpty();
        result.Transactions.ShouldBe(
        [
            new(14, "00 1050 0000 0000 0000 0000 0003", new(2026, 10, 21), new(2026, 10, 20), null, -15.40m, "PLN", false, "PIEKARNIA TESTOWA WARSZAWA PL", "Płatność kartą 20.10.2026 Nr karty 4246xx******1234", "202629400000001", "TR.KART", true),
            new(15, "00 1050 0000 0000 0000 0000 0003", new(2026, 10, 22), new(2026, 10, 22), null, -1250.00m, "PLN", false, "JAN TESTOWY", "PRĄD", "202629500000002", "PRZELEW", true),
            new(16, "00 1050 0000 0000 0000 0000 0003", new(2026, 10, 24), new(2026, 10, 24), null, -99.99m, "PLN", false, "SKLEP INTERNETOWY", "Płatność kartą 24.10.2026", null, "", false),
        ]);
    }

    /// <summary>Revolut: fee added to the spend, purchase time kept, pocket+currency as the account, other currencies flagged.</summary>
    [Fact]
    public void Parse_RevolutFixture_AddsFeesAndSeparatesCurrencies()
    {
        var result = ParseFixture("revolut.csv");

        result.Profile.ShouldBe("revolut");
        result.Issues.ShouldBeEmpty();
        result.Transactions.ShouldBe(
        [
            new(2, "Current:PLN", new(2026, 10, 20), new(2026, 10, 19), new(12, 3, 44), -12.50m, "PLN", false, "Kawiarnia Żółta", "", null, "CARD_PAYMENT", true),
            new(3, "Current:PLN", new(2026, 10, 21), new(2026, 10, 21), new(18, 0, 0), -30.00m, "PLN", false, "Pending Shop", "", null, "CARD_PAYMENT", false),
            new(4, "Current:PLN", new(2026, 10, 22), new(2026, 10, 22), new(9, 0, 0), -101.00m, "PLN", false, "Exchanged to EUR", "", null, "EXCHANGE", true),
            new(5, "Current:EUR", new(2026, 10, 23), new(2026, 10, 23), new(10, 0, 0), -4.20m, "EUR", true, "Cafe Paris", "", null, "CARD_PAYMENT", true),
        ]);
    }

    /// <summary>The Polish-language export: localised headers and states read like the English ones.</summary>
    [Fact]
    public void Parse_RevolutPolishFixture_ReadsLocalisedColumns()
    {
        var result = ParseFixture("revolut-pl.csv");

        result.Profile.ShouldBe("revolut");
        result.Issues.ShouldBeEmpty();
        result.Transactions.ShouldBe(
        [
            new(2, "Bieżące:PLN", new(2026, 10, 20), new(2026, 10, 19), new(12, 3, 44), -12.50m, "PLN", false, "Kawiarnia Żółta", "", null, "Płatność kartą", true),
            new(3, "Bieżące:PLN", new(2026, 10, 21), new(2026, 10, 21), new(18, 0, 0), -30.00m, "PLN", false, "Sklep W Toku", "", null, "Płatność kartą", false),
            new(4, "Bieżące:PLN", new(2026, 10, 22), new(2026, 10, 22), new(9, 0, 0), -101.00m, "PLN", false, "Wymieniono na EUR", "", null, "Wymiana", true),
        ]);
    }

    /// <summary>Alior: period from the criteria line, the other party by direction, the amount in the account's currency.</summary>
    [Fact]
    public void Parse_AliorFixture_ReadsPeriodAndAccountCurrencyAmounts()
    {
        var result = ParseFixture("alior.csv");

        result.Profile.ShouldBe("alior");
        result.PeriodFrom.ShouldBe(new DateOnly(2026, 10, 19));
        result.PeriodTo.ShouldBe(new DateOnly(2026, 10, 25));
        result.Issues.ShouldBeEmpty();
        result.Transactions.ShouldBe(
        [
            new(3, null, new(2026, 10, 21), new(2026, 10, 20), null, -123.45m, "PLN", false, "", "ALLEGRO.PL POZNAN POL", null, "", true),
            new(4, null, new(2026, 10, 22), new(2026, 10, 22), null, -1500.00m, "PLN", false, "ANNA TESTOWA", "Czynsz za październik", null, "", true),
            new(5, null, new(2026, 10, 23), new(2026, 10, 23), null, 45.00m, "PLN", false, "ANNA TESTOWA", "Zwrot za zakupy", null, "", true),
            new(6, null, new(2026, 10, 24), new(2026, 10, 24), null, -18.30m, "PLN", false, "", "CAFE TEST PARIS FRA", null, "", true),
        ]);
    }

    /// <summary>Each bad row is reported by line number and code; the good rows around it still parse.</summary>
    [Fact]
    public void Parse_MalformedRows_ReturnsIssuesWithLineNumbers()
    {
        var result = ParseText(MbankFile(
            "2026-10-19;2026-10-19;ZAKUP;A;;'';-1,00;0,00;",
            "19.10.2026;2026-10-19;ZAKUP;B;;'';-1,00;0,00;",
            "2026-10-19;2026-10-19;ZAKUP;C;;'';abc;0,00;",
            "2026-10-19;2026-10-19;ZAKUP;D;;'';-1,005;0,00;",
            "2026-10-19;2026-10-19;ZAKUP",
            "2026-10-19;2026-10-19;ZAKUP;E;;'';;0,00;",
            "2026-10-20;2026-10-20;ZAKUP;F;;'';-2,00;0,00;"));

        result.Transactions.Select(t => (t.LineNumber, t.Title)).ShouldBe([(4, "A"), (10, "F")]);
        result.Issues.ShouldBe(
        [
            new(5, StatementIssueCode.InvalidDate),
            new(6, StatementIssueCode.InvalidAmount),
            new(7, StatementIssueCode.ExcessPrecision),
            new(8, StatementIssueCode.MissingColumn),
            new(9, StatementIssueCode.InvalidAmount),
        ]);
    }

    /// <summary>A stray quote inside an unquoted cell is text; it does not swallow the following delimiters.</summary>
    [Fact]
    public void Parse_MidFieldQuote_KeepsColumns()
    {
        var result = ParseText(MbankFile("2026-10-19;2026-10-19;ZAKUP;SKLEP 5\" EKRAN;;'';-1,00;0,00;"));

        var transaction = result.Transactions.ShouldHaveSingleItem();
        transaction.Title.ShouldBe("SKLEP 5\" EKRAN");
        transaction.Amount.ShouldBe(-1.00m);
    }

    /// <summary>A foreign row keeps its own currency and is flagged against the Budget's, not converted.</summary>
    [Fact]
    public void Parse_BudgetInEur_FlagsPlnRowsAsForeign()
    {
        var result = StatementParser.Parse(Encoding.UTF8.GetBytes(MbankFile("2026-10-19;2026-10-19;ZAKUP;A;;'';-1,00;0,00;")), BudgetCurrency.EUR);

        var transaction = result.Transactions.ShouldHaveSingleItem();
        transaction.Currency.ShouldBe("PLN");
        transaction.IsForeignCurrency.ShouldBeTrue();
    }

    /// <summary>A UTF-8 byte-order mark before the header does not hide it (§2.1 allows one).</summary>
    [Fact]
    public void Parse_Utf8WithBom_DetectsHeader()
    {
        var file = "Type,Product,Started Date,Completed Date,Description,Amount,Fee,Currency,State,Balance\nCARD_PAYMENT,Current,2026-10-19 12:00:00,2026-10-19 12:00:00,Żabka,-1.00,0.00,PLN,COMPLETED,1.00";

        var result = StatementParser.Parse([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(file)], BudgetCurrency.PLN);

        result.Profile.ShouldBe("revolut");
        result.Transactions.ShouldHaveSingleItem().Counterparty.ShouldBe("Żabka");
    }

    /// <summary>A file no profile recognises is rejected, never guessed at.</summary>
    [Fact]
    public void Parse_UnknownFormat_Throws()
        => Should.Throw<BudgetDomainException>(() => ParseText("Date,Amount\n2026-10-19,-1.00"))
            .Message.ShouldContain("Unrecognised statement format");

    /// <summary>Revolut exports UTF-8 only; a Windows-1250 re-save would already have damaged its text.</summary>
    [Fact]
    public void Parse_RevolutNotUtf8_Throws()
    {
        var file = "Type,Product,Started Date,Completed Date,Description,Amount,Fee,Currency,State,Balance\nCARD_PAYMENT,Current,2026-10-19 12:00:00,2026-10-19 12:00:00,Żabka,-1.00,0.00,PLN,COMPLETED,1.00";

        Should.Throw<BudgetDomainException>(() => StatementParser.Parse(CodePagesEncodingProvider.Instance.GetEncoding(1250)!.GetBytes(file), BudgetCurrency.PLN))
            .Message.ShouldContain("UTF-8");
    }

    /// <summary>The row limit admits exactly <see cref="StatementParser.MaxDataRows"/> rows.</summary>
    [Fact]
    public void Parse_MoreThanMaxRows_Throws()
    {
        var rows = Enumerable.Repeat("2026-10-19;2026-10-19;ZAKUP;A;;'';-1,00;0,00;", StatementParser.MaxDataRows);

        ParseText(MbankFile([.. rows])).Transactions.Count.ShouldBe(StatementParser.MaxDataRows);
        Should.Throw<BudgetDomainException>(() => ParseText(MbankFile([.. rows, .. rows.Take(1)])));
    }

    /// <summary>An oversized file is rejected before it is decoded.</summary>
    [Fact]
    public void Parse_LargerThanMaxBytes_Throws()
        => Should.Throw<BudgetDomainException>(() => StatementParser.Parse(new byte[StatementParser.MaxFileBytes + 1], BudgetCurrency.PLN));

    /// <summary>The fingerprint form: collapsed, upper-case, card masks removed.</summary>
    [Theory]
    [InlineData("  Płatność kartą  20.10.2026 Nr karty 4246xx******1234 ", "PŁATNOŚĆ KARTĄ 20.10.2026")]
    [InlineData("Sklep Testowy   /Gliwice", "SKLEP TESTOWY /GLIWICE")]
    [InlineData("Numer karty: 425125******1234 CAFE", "NUMER KARTY: CAFE")]
    public void NormalizeForMatching_CollapsesUppercasesAndDropsCardMasks(string input, string expected)
        => StatementText.NormalizeForMatching(input).ShouldBe(expected);
}
