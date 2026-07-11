using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Settings;
using ServiceBridge.Infrastructure.Services;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class InventoryCodeProtectorTests
{
    [Fact]
    public void ProtectAndUnprotect_RoundTripsWithoutPlaintextStorage()
    {
        var protector = CreateProtector();

        var protectedValue = protector.Protect("Code-AbC-123");

        Assert.NotEqual("Code-AbC-123", protectedValue);
        Assert.Equal("Code-AbC-123", protector.Unprotect(protectedValue));
    }

    [Fact]
    public void ProtectAndUnprotect_PreserveSurroundingWhitespaceExactly()
    {
        var protector = CreateProtector();
        const string plaintext = "  Code-AbC-123  ";

        var protectedValue = protector.Protect(plaintext);

        Assert.Equal(plaintext, protector.Unprotect(protectedValue));
    }

    [Fact]
    public void Protect_RepeatedCallsNeedNotProduceIdenticalCiphertext()
    {
        var protector = CreateProtector();

        var first = protector.Protect("Code-AbC-123");
        var second = protector.Protect("Code-AbC-123");

        Assert.Equal("Code-AbC-123", protector.Unprotect(first));
        Assert.Equal("Code-AbC-123", protector.Unprotect(second));
    }

    [Fact]
    public void ComputeFingerprint_IsDeterministicAndDoesNotExposePlaintext()
    {
        var protector = CreateProtector();

        var first = protector.ComputeFingerprint("Code-AbC-123");
        var second = protector.ComputeFingerprint("Code-AbC-123");

        Assert.Equal(first, second);
        Assert.DoesNotContain("Code-AbC-123", first, StringComparison.Ordinal);
        Assert.Matches("^[A-F0-9]{64}$", first);
    }

    [Fact]
    public void ComputeFingerprint_DifferentPlaintextProducesDifferentFingerprint()
    {
        var protector = CreateProtector();

        Assert.NotEqual(
            protector.ComputeFingerprint("Code-1"),
            protector.ComputeFingerprint("Code-2"));
    }

    [Fact]
    public void ComputeFingerprint_PreservesCase()
    {
        var protector = CreateProtector();

        Assert.NotEqual(
            protector.ComputeFingerprint("Code-AbC"),
            protector.ComputeFingerprint("code-abc"));
    }

    [Fact]
    public void ComputeFingerprint_NormalizesSurroundingWhitespaceOnly()
    {
        var protector = CreateProtector();

        Assert.Equal(
            protector.ComputeFingerprint("Code AbC"),
            protector.ComputeFingerprint("  Code AbC\r\n"));
        Assert.NotEqual(
            protector.ComputeFingerprint("Code AbC"),
            protector.ComputeFingerprint("Code  AbC"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-base64")]
    public void ComputeFingerprint_WithoutValidKey_ThrowsClearConfigurationException(
        string? fingerprintKey)
    {
        var protector = CreateProtectorWithKey(fingerprintKey);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            protector.ComputeFingerprint("Code-AbC-123"));

        Assert.Contains("fingerprint key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComputeFingerprint_WithBase64KeyShorterThan32Bytes_ThrowsClearConfigurationException()
    {
        var protector = CreateProtectorWithKey(
            Convert.ToBase64String(new byte[31]));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            protector.ComputeFingerprint("Code-AbC-123"));

        Assert.Contains("fingerprint key", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingFingerprintKey_DefersFailureUntilFingerprinting()
    {
        using var provider = CreateServiceProviderWithoutFingerprintKey();
        var protector = provider.GetRequiredService<IInventoryCodeProtector>();

        var protectedValue = protector.Protect("Code-AbC-123");

        Assert.Equal("Code-AbC-123", protector.Unprotect(protectedValue));
        Assert.Throws<InvalidOperationException>(() =>
            protector.ComputeFingerprint("Code-AbC-123"));
    }

    [Fact]
    public void ComputeFingerprint_IndependentInstancesWithSameKeyMatch()
    {
        var first = CreateProtector();
        var second = CreateProtector();

        Assert.Equal(
            first.ComputeFingerprint("Code-AbC-123"),
            second.ComputeFingerprint("Code-AbC-123"));
    }

    private static DataProtectionInventoryCodeProtector CreateProtector()
    {
        var fingerprintKey = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF"));

        return CreateProtectorWithKey(fingerprintKey);
    }

    private static DataProtectionInventoryCodeProtector CreateProtectorWithKey(
        string? fingerprintKey)
    {
        return new DataProtectionInventoryCodeProtector(
            new EphemeralDataProtectionProvider(),
            Options.Create(new InventoryCodeProtectionOptions
            {
                FingerprintKey = fingerprintKey
            }));
    }

    private static ServiceProvider CreateServiceProviderWithoutFingerprintKey()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.Configure<InventoryCodeProtectionOptions>(_ => { });
        services.AddSingleton<IInventoryCodeProtector, DataProtectionInventoryCodeProtector>();
        return services.BuildServiceProvider();
    }
}
