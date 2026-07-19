using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Settings;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Services;

public sealed class DataProtectionDeliveryArtifactProtector :
    IDeliveryArtifactProtector
{
    public const int PlaintextValueMaxLength = 4096;

    private const string ProtectionPurpose =
        "ServiceBridge.DeliveryArtifacts.Value.v1";
    private const string FingerprintPurpose =
        "ServiceBridge.DeliveryArtifacts.Fingerprint.v1";
    private const int MinimumFingerprintKeyBytes = 32;

    private readonly IDataProtector _protector;
    private readonly DeliveryArtifactProtectionOptions _options;

    public DataProtectionDeliveryArtifactProtector(
        IDataProtectionProvider provider,
        IOptions<DeliveryArtifactProtectionOptions> options)
    {
        _protector = provider.CreateProtector(ProtectionPurpose);
        _options = options.Value;

        var fingerprintKey = GetFingerprintKey();
        CryptographicOperations.ZeroMemory(fingerprintKey);
    }

    public string Protect(string plaintext)
    {
        ValidatePlaintext(plaintext);

        var protectedValue = _protector.Protect(plaintext);
        if (protectedValue.Length > DeliveryArtifact.ProtectedValueMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(plaintext),
                "Protected delivery artifact exceeds the persistence limit.");
        }

        return protectedValue;
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
        {
            throw new ArgumentException(
                "Protected delivery artifact value is required.",
                nameof(protectedValue));
        }

        if (protectedValue.Length > DeliveryArtifact.ProtectedValueMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(protectedValue),
                $"Protected value cannot exceed " +
                $"{DeliveryArtifact.ProtectedValueMaxLength} characters.");
        }

        return _protector.Unprotect(protectedValue);
    }

    public string ComputeFingerprint(string plaintext)
    {
        ValidatePlaintext(plaintext);

        var key = GetFingerprintKey();
        try
        {
            using var hmac = new HMACSHA256(key);
            var normalizedValue = plaintext.Trim();
            var purposeSeparatedValue =
                $"{FingerprintPurpose}\0{normalizedValue}";
            var fingerprint = hmac.ComputeHash(
                Encoding.UTF8.GetBytes(purposeSeparatedValue));
            return Convert.ToHexString(fingerprint);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static void ValidatePlaintext(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            throw new ArgumentException(
                "Delivery artifact value is required.",
                nameof(plaintext));
        }

        if (plaintext.Length > PlaintextValueMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(plaintext),
                $"Delivery artifact value cannot exceed " +
                $"{PlaintextValueMaxLength} characters.");
        }
    }

    private byte[] GetFingerprintKey()
    {
        if (string.IsNullOrWhiteSpace(_options.FingerprintKey))
        {
            throw ConfigurationException();
        }

        try
        {
            var key = Convert.FromBase64String(_options.FingerprintKey);
            if (key.Length < MinimumFingerprintKeyBytes)
            {
                CryptographicOperations.ZeroMemory(key);
                throw ConfigurationException();
            }

            return key;
        }
        catch (FormatException exception)
        {
            throw ConfigurationException(exception);
        }
    }

    private static InvalidOperationException ConfigurationException(
        Exception? innerException = null)
    {
        return new InvalidOperationException(
            "Delivery artifact fingerprint key must be configured as a Base64 " +
            "value containing at least 32 bytes.",
            innerException);
    }
}
