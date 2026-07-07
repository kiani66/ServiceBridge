namespace ServiceBridge.Api.Configuration;

public sealed record StartupConfiguration(
    string DefaultConnectionString,
    string JwtIssuer,
    string JwtAudience,
    string JwtSigningKey,
    int JwtExpirationMinutes);
