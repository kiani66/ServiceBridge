using System.Text;
using System.Text.Json;

namespace ServiceBridge.Infrastructure.Services;

public static class PaymentGatewayPayloadSanitizer
{
    private const int MaxPayloadLength = 16_384;
    private const string RedactedValue = "[REDACTED]";

    private static readonly string[] SensitiveKeyFragments =
    [
        "secret",
        "token",
        "apikey",
        "password",
        "credential",
        "signature",
        "authorization",
        "cvv",
        "pin",
        "pan"
    ];

    public static string Sanitize(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return "{}";
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(
                       stream,
                       new JsonWriterOptions { Indented = false }))
            {
                WriteSanitizedElement(writer, document.RootElement, propertyName: null);
            }

            return Truncate(Encoding.UTF8.GetString(stream.ToArray()));
        }
        catch (JsonException)
        {
            return Truncate(payload);
        }
    }

    private static void WriteSanitizedElement(
        Utf8JsonWriter writer,
        JsonElement element,
        string? propertyName)
    {
        if (IsSensitiveKey(propertyName))
        {
            writer.WriteStringValue(RedactedValue);
            return;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    WriteSanitizedElement(writer, property.Value, property.Name);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteSanitizedElement(writer, item, propertyName: null);
                }

                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static bool IsSensitiveKey(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return false;
        }

        var normalized = new string(
            propertyName
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());

        return SensitiveKeyFragments.Any(normalized.Contains);
    }

    private static string Truncate(string value)
    {
        return value.Length <= MaxPayloadLength
            ? value
            : value[..MaxPayloadLength];
    }
}
