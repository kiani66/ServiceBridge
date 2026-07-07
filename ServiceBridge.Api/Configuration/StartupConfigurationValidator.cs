namespace ServiceBridge.Api.Configuration;

public static class StartupConfigurationValidator
{
    public static StartupConfiguration Validate(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var failures = new List<string>();

        var defaultConnectionString = RequireConnectionString(
            configuration,
            "DefaultConnection",
            failures);
        var jwtIssuer = RequireValue(configuration, "Jwt:Issuer", failures);
        var jwtAudience = RequireValue(configuration, "Jwt:Audience", failures);
        var jwtSigningKey = RequireValue(configuration, "Jwt:SigningKey", failures);
        var jwtExpirationMinutes = RequirePositiveInt(
            configuration,
            "Jwt:ExpirationMinutes",
            failures);

        if (environment.IsProduction() &&
            !string.IsNullOrWhiteSpace(jwtSigningKey) &&
            jwtSigningKey.Length < 32)
        {
            failures.Add("Jwt:SigningKey must be at least 32 characters in production.");
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Startup configuration is invalid: " + string.Join(" ", failures));
        }

        return new StartupConfiguration(
            defaultConnectionString,
            jwtIssuer,
            jwtAudience,
            jwtSigningKey,
            jwtExpirationMinutes);
    }

    private static string RequireConnectionString(
        IConfiguration configuration,
        string name,
        ICollection<string> failures)
    {
        var value = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"ConnectionStrings:{name} must be configured.");
            return string.Empty;
        }

        return value;
    }

    private static string RequireValue(
        IConfiguration configuration,
        string key,
        ICollection<string> failures)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{key} must be configured.");
            return string.Empty;
        }

        return value;
    }

    private static int RequirePositiveInt(
        IConfiguration configuration,
        string key,
        ICollection<string> failures)
    {
        var value = configuration[key];
        if (!int.TryParse(value, out var parsedValue) || parsedValue <= 0)
        {
            failures.Add($"{key} must be a positive integer.");
            return 0;
        }

        return parsedValue;
    }
}
