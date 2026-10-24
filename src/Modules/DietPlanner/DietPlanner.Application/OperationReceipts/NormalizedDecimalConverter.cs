namespace DietPlanner.Application.OperationReceipts;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Writes decimals without trailing zeros, so <c>300</c> and <c>300.0</c> hash the same.</summary>
internal sealed class NormalizedDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDecimal();

    // Dividing by 1 at the highest scale strips trailing zeros.
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value / 1.0000000000000000000000000000m);
}
