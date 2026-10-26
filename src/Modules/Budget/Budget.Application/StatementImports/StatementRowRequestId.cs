namespace Budget.Application.StatementImports;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// The client request ID of one imported row's revision 1 (<c>bank-import.md</c> §5.2): UUIDv5 with the
/// import's request ID as namespace and the source key as name. Deterministic, so a replay finds the same
/// revisions; distinct per row, so one import does not collide with itself on the request-identity index.
/// </summary>
internal static class StatementRowRequestId
{
    /// <summary>RFC 9562 UUIDv5 of <paramref name="sourceKey"/> in the <paramref name="importRequestId"/> namespace.</summary>
    public static Guid For(Guid importRequestId, string sourceKey)
    {
        // SHA-1 is what UUIDv5 specifies; this is an identifier, not a security boundary.
#pragma warning disable CA5350, S4790
        var hash = SHA1.HashData([.. importRequestId.ToByteArray(bigEndian: true), .. Encoding.UTF8.GetBytes(sourceKey)]);
#pragma warning restore CA5350, S4790
        var bytes = hash.AsSpan(0, 16);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes, bigEndian: true);
    }
}
