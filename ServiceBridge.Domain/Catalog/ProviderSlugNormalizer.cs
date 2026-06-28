namespace ServiceBridge.Domain.Catalog;

public static class ProviderSlugNormalizer
{
    public const string UnknownProviderName = "Unknown Provider";
    public const string UnknownProviderSlug = "unknown-provider";

    public static string NormalizeName(string providerName)
    {
        return string.IsNullOrWhiteSpace(providerName)
            ? UnknownProviderName
            : providerName.Trim();
    }

    public static string NormalizeSlug(string providerName)
    {
        var slug = NormalizeName(providerName)
            .ToLowerInvariant()
            .Replace("&", " and ")
            .Replace(".", string.Empty)
            .Replace(",", string.Empty)
            .Replace("'", string.Empty)
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace(" ", "-");

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-");
        }

        return string.IsNullOrWhiteSpace(slug) ? UnknownProviderSlug : slug;
    }
}
