namespace ServiceBridge.Domain.ValueObjects;

public sealed record FulfillmentFailureCode
{
    public const int MaxLength = 100;

    private FulfillmentFailureCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static FulfillmentFailureCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Fulfillment failure code is required.",
                nameof(value));
        }

        var normalized = value.Trim();
        if (normalized.Length > MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                $"Fulfillment failure code cannot exceed {MaxLength} characters.");
        }

        if (normalized.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not '.' and not '-' and not '_'))
        {
            throw new ArgumentException(
                "Fulfillment failure code may contain only ASCII letters, digits, dots, hyphens, and underscores.",
                nameof(value));
        }

        return new FulfillmentFailureCode(normalized);
    }

    public override string ToString()
    {
        return Value;
    }
}
