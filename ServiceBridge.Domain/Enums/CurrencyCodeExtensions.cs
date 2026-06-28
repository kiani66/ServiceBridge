namespace ServiceBridge.Domain.Enums;

public static class CurrencyCodeExtensions
{
    public static string ToIsoCode(this CurrencyCode currencyCode)
    {
        return currencyCode switch
        {
            CurrencyCode.USD => "USD",
            CurrencyCode.EUR => "EUR",
            CurrencyCode.GBP => "GBP",
            CurrencyCode.AED => "AED",
            CurrencyCode.TRY => "TRY",
            _ => "UNKNOWN"
        };
    }

    public static string ToIsoString(this CurrencyCode currencyCode)
    {
        return currencyCode.ToIsoCode();
    }

    public static CurrencyCode FromIsoCode(string? isoCode)
    {
        return TryFromIsoCode(isoCode, out var currencyCode)
            ? currencyCode
            : throw new ArgumentException("Unsupported currency code.", nameof(isoCode));
    }

    public static CurrencyCode FromIsoString(string? isoCode)
    {
        return FromIsoCode(isoCode);
    }

    public static bool TryFromIsoCode(string? isoCode, out CurrencyCode currencyCode)
    {
        currencyCode = NormalizeIsoCode(isoCode) switch
        {
            "USD" => CurrencyCode.USD,
            "EUR" => CurrencyCode.EUR,
            "GBP" => CurrencyCode.GBP,
            "AED" => CurrencyCode.AED,
            "TRY" => CurrencyCode.TRY,
            _ => CurrencyCode.Unknown
        };

        return currencyCode != CurrencyCode.Unknown;
    }

    public static bool TryFromIsoString(string? isoCode, out CurrencyCode currencyCode)
    {
        return TryFromIsoCode(isoCode, out currencyCode);
    }

    private static string NormalizeIsoCode(string? isoCode)
    {
        return string.IsNullOrWhiteSpace(isoCode)
            ? string.Empty
            : isoCode.Trim().ToUpperInvariant();
    }
}
