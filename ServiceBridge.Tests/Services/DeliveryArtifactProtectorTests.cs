using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Settings;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Services;
using Xunit;

namespace ServiceBridge.Tests.Services;

public sealed class DeliveryArtifactProtectorTests
{
    [Fact]
    public void ProtectAndUnprotect_RoundTripWithoutPlaintextStorage()
    {
        var protector = CreateProtector();
        const string plaintext = "gift-code-123";

        var protectedValue = protector.Protect(plaintext);

        Assert.NotEqual(plaintext, protectedValue);
        Assert.DoesNotContain(plaintext, protectedValue, StringComparison.Ordinal);
        Assert.Equal(plaintext, protector.Unprotect(protectedValue));
    }

    [Fact]
    public void ComputeFingerprint_IsKeyedDeterministicAndValueSensitive()
    {
        var protector = CreateProtector();

        var first = protector.ComputeFingerprint("gift-code-123");
        var repeated = protector.ComputeFingerprint("gift-code-123");
        var different = protector.ComputeFingerprint("gift-code-124");

        Assert.Equal(first, repeated);
        Assert.NotEqual(first, different);
        Assert.Equal(
            "CD52BC62774A82E05AA09AC8CD27C6DADF8E68267FE67DF14A42936FE4C18281",
            first);
        Assert.Matches("^[A-F0-9]{64}$", first);
    }

    [Fact]
    public void PurposeIsolation_RejectsInventoryCiphertextAndSeparatesFingerprint()
    {
        var provider = new EphemeralDataProtectionProvider();
        var key = FingerprintKey();
        var artifactProtector = CreateProtector(provider, key);
        var inventoryProtector = new DataProtectionInventoryCodeProtector(
            provider,
            Options.Create(new InventoryCodeProtectionOptions
            {
                FingerprintKey = key
            }));
        const string plaintext = "shared-low-entropy-code";

        var inventoryCiphertext = inventoryProtector.Protect(plaintext);

        Assert.Throws<CryptographicException>(() =>
            artifactProtector.Unprotect(inventoryCiphertext));
        Assert.NotEqual(
            inventoryProtector.ComputeFingerprint(plaintext),
            artifactProtector.ComputeFingerprint(plaintext));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ProtectAndFingerprint_RejectEmptyValues(string plaintext)
    {
        var protector = CreateProtector();

        Assert.Throws<ArgumentException>(() => protector.Protect(plaintext));
        Assert.Throws<ArgumentException>(() =>
            protector.ComputeFingerprint(plaintext));
    }

    [Fact]
    public void ProtectAndFingerprint_RejectOversizedValues()
    {
        var protector = CreateProtector();
        var oversized = new string(
            'X',
            DataProtectionDeliveryArtifactProtector.PlaintextValueMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            protector.Protect(oversized));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            protector.ComputeFingerprint(oversized));

        var oversizedOutputProtector = CreateProtector(
            new OversizedDataProtectionProvider());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            oversizedOutputProtector.Protect("bounded-plaintext"));
    }

    [Fact]
    public void MissingKey_DoesNotBreakRegistrationButFailsOnExplicitResolution()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.Configure<DeliveryArtifactProtectionOptions>(_ => { });
        services.AddSingleton<
            IDeliveryArtifactProtector,
            DataProtectionDeliveryArtifactProtector>();

        using var serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
        var exception = Assert.Throws<InvalidOperationException>(() =>
            serviceProvider.GetRequiredService<IDeliveryArtifactProtector>());
        Assert.Contains(
            "fingerprint key",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        foreach (var invalidKey in new[] { "not-a-base64-secret", "AQID" })
        {
            var invalidException = Assert.Throws<InvalidOperationException>(() =>
                CreateProtector(fingerprintKey: invalidKey));

            Assert.Contains(
                "fingerprint key",
                invalidException.Message,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                invalidKey,
                invalidException.ToString(),
                StringComparison.Ordinal);
        }
    }

    private static DataProtectionDeliveryArtifactProtector CreateProtector(
        IDataProtectionProvider? provider = null,
        string? fingerprintKey = null)
    {
        return new DataProtectionDeliveryArtifactProtector(
            provider ?? new EphemeralDataProtectionProvider(),
            Options.Create(new DeliveryArtifactProtectionOptions
            {
                FingerprintKey = fingerprintKey ?? FingerprintKey()
            }));
    }

    private static string FingerprintKey()
    {
        return Convert.ToBase64String(
            Encoding.UTF8.GetBytes("ABCDEF0123456789ABCDEF0123456789"));
    }

    private sealed class OversizedDataProtectionProvider :
        IDataProtectionProvider
    {
        public IDataProtector CreateProtector(string purpose)
        {
            return new OversizedDataProtector();
        }
    }

    private sealed class OversizedDataProtector : IDataProtector
    {
        public IDataProtector CreateProtector(string purpose)
        {
            return this;
        }

        public byte[] Protect(byte[] plaintext)
        {
            return Encoding.UTF8.GetBytes(new string(
                'X',
                DeliveryArtifact.ProtectedValueMaxLength + 1));
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            return protectedData;
        }
    }
}
