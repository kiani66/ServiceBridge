using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ServiceBridge.Infrastructure.Services;

public static class SensitiveDataRedactor
{
    public const string RedactedValue = "[REDACTED]";

    private static readonly string[] SensitiveKeyParts =
    [
        "password",
        "credential",
        "token",
        "secret",
        "key",
        "authority",
        "gateway",
        "hash",
        "recovery",
        "otp",
        "code"
    ];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    public static string? ToRedactedJson(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var node = ToJsonNode(value);
        if (node is null)
        {
            return null;
        }

        RedactNode(node);
        return node.ToJsonString(SerializerOptions);
    }

    private static JsonNode? ToJsonNode(object value)
    {
        if (value is JsonNode node)
        {
            return node.DeepClone();
        }

        if (value is JsonElement element)
        {
            return JsonNode.Parse(element.GetRawText());
        }

        if (value is string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return JsonValue.Create(text);
            }

            try
            {
                return JsonNode.Parse(text);
            }
            catch (JsonException)
            {
                return JsonValue.Create(text);
            }
        }

        return JsonSerializer.SerializeToNode(value, SerializerOptions);
    }

    public static string? RedactDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        return SensitiveKeyParts.Any(part =>
            description.Contains(part, StringComparison.OrdinalIgnoreCase))
            ? RedactedValue
            : description;
    }

    private static void RedactNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                RedactObject(jsonObject);
                break;
            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    RedactNode(item);
                }

                break;
        }
    }

    private static void RedactObject(JsonObject jsonObject)
    {
        foreach (var property in jsonObject.ToList())
        {
            if (IsSensitiveKey(property.Key))
            {
                jsonObject[property.Key] = RedactedValue;
                continue;
            }

            RedactNode(property.Value);
        }
    }

    private static bool IsSensitiveKey(string key)
    {
        return SensitiveKeyParts.Any(part =>
            key.Contains(part, StringComparison.OrdinalIgnoreCase));
    }
}
