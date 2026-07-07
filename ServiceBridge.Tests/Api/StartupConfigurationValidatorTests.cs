using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using ServiceBridge.Api.Configuration;
using Xunit;

namespace ServiceBridge.Tests.Api;

public class StartupConfigurationValidatorTests
{
    [Fact]
    public void Validate_WhenProductionConfigurationIsComplete_ReturnsStartupConfiguration()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=ServiceBridge;Trusted_Connection=True;",
            ["Jwt:Issuer"] = "ServiceBridge",
            ["Jwt:Audience"] = "ServiceBridge.Web",
            ["Jwt:SigningKey"] = "production-signing-key-at-least-32-chars",
            ["Jwt:ExpirationMinutes"] = "1440"
        });

        var result = StartupConfigurationValidator.Validate(
            configuration,
            new TestHostEnvironment(Environments.Production));

        Assert.Equal("ServiceBridge", result.JwtIssuer);
        Assert.Equal("ServiceBridge.Web", result.JwtAudience);
        Assert.Equal(1440, result.JwtExpirationMinutes);
    }

    [Fact]
    public void Validate_WhenProductionConfigurationIsInvalid_RejectsWithoutSecretValues()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "",
            ["Jwt:Issuer"] = "ServiceBridge",
            ["Jwt:Audience"] = "ServiceBridge.Web",
            ["Jwt:SigningKey"] = "short-secret-value",
            ["Jwt:ExpirationMinutes"] = "0"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            StartupConfigurationValidator.Validate(
                configuration,
                new TestHostEnvironment(Environments.Production)));

        Assert.Contains("ConnectionStrings:DefaultConnection", exception.Message);
        Assert.Contains("Jwt:ExpirationMinutes", exception.Message);
        Assert.Contains("Jwt:SigningKey", exception.Message);
        Assert.DoesNotContain("short-secret-value", exception.Message);
    }

    [Fact]
    public void Validate_WhenDevelopmentSigningKeyIsShort_AllowsLocalConfiguration()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=ServiceBridgeDev;Trusted_Connection=True;",
            ["Jwt:Issuer"] = "ServiceBridge",
            ["Jwt:Audience"] = "ServiceBridge.Web",
            ["Jwt:SigningKey"] = "local-dev-key",
            ["Jwt:ExpirationMinutes"] = "10080"
        });

        var result = StartupConfigurationValidator.Validate(
            configuration,
            new TestHostEnvironment(Environments.Development));

        Assert.Equal("local-dev-key", result.JwtSigningKey);
    }

    private static IConfiguration CreateConfiguration(
        IEnumerable<KeyValuePair<string, string?>> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "ServiceBridge.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
