namespace ServiceBridge.Domain.ValueObjects;

public sealed record RegionCode
{
    private RegionCode(string code, string? displayName = null)
    {
        Code = code;
        DisplayName = displayName;
    }

    public string Code { get; }

    public string? DisplayName { get; }

    public static RegionCode Global { get; } = new("GLOBAL", "Global");
    public static RegionCode US { get; } = new("US", "United States");
    public static RegionCode UK { get; } = new("UK", "United Kingdom");
    public static RegionCode TR { get; } = new("TR", "Turkey");
    public static RegionCode AE { get; } = new("AE", "United Arab Emirates");
    public static RegionCode EU { get; } = new("EU", "European Union");
    public static RegionCode DE { get; } = new("DE", "Germany");
    public static RegionCode FR { get; } = new("FR", "France");

    public static IReadOnlyCollection<RegionCode> KnownRegions { get; } =
    [
        Global,
        US,
        UK,
        TR,
        AE,
        EU,
        DE,
        FR
    ];

    public static RegionCode FromCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Region code is required.", nameof(code));
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        return KnownRegions.FirstOrDefault(region => region.Code == normalizedCode)
            ?? new RegionCode(normalizedCode);
    }

    public override string ToString()
    {
        return Code;
    }
}
