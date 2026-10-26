namespace Budget.Application.StatementImports;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// The hashed key that makes a statement row importable once (<c>bank-import.md</c> §4.1). Only the
/// hash is stored; bank account numbers and references never reach the database in clear.
/// </summary>
internal static class StatementSourceKey
{
    /// <summary>
    /// One key per transaction, in order. With a bank reference: <c>profile | account | reference</c>.
    /// Otherwise the fingerprint <c>profile | account | booking date | amount | description | n</c>, where
    /// <c>n</c> is the 1-based occurrence of that exact tuple in the file, so two identical coffees on the
    /// same day stay two rows. A file that names no account (PKO BP) uses the importing person instead, so
    /// two adults' identical purchases do not collide in the household scope; a joint account exported by
    /// both then shows as <c>PossibleDuplicate</c> rather than being blocked.
    /// </summary>
    public static IReadOnlyList<string> ForAll(string profile, Guid importerPersonId, IReadOnlyList<StatementTransaction> transactions)
    {
        Dictionary<string, int> occurrences = [];
        List<string> keys = new(transactions.Count);

        foreach (var t in transactions)
        {
            var account = t.AccountId is null ? $"person:{importerPersonId}" : StatementText.NormalizeForMatching(t.AccountId);
            string material;
            if (t.BankReference is { } reference)
                material = $"{profile}|{account}|ref|{reference}";
            else
            {
                var booked = t.TransactionTime is { } time
                    ? $"{t.BookingDate:yyyy-MM-dd}T{time:HH:mm:ss}"
                    : t.BookingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var tuple = $"{profile}|{account}|{booked}|{t.Amount.ToString("F2", CultureInfo.InvariantCulture)}|{StatementText.NormalizeForMatching($"{t.Counterparty} {t.Title}")}";
                var n = occurrences[tuple] = occurrences.GetValueOrDefault(tuple) + 1;
                material = $"{tuple}|{n}";
            }

            keys.Add(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(material))));
        }

        return keys;
    }
}
