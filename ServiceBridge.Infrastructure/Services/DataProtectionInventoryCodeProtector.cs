using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Settings;

namespace ServiceBridge.Infrastructure.Services;

public sealed class DataProtectionInventoryCodeProtector : IInventoryCodeProtector
{
    private const string Purpose = "ServiceBridge.InventoryCodes.v1";
    private const int MinimumFingerprintKeyBytes = 32;
    private readonly IDataProtector _protector;
    private readonly InventoryCodeProtectionOptions _options;

    public DataProtectionInventoryCodeProtector(
        IDataProtectionProvider provider,
        IOptions<InventoryCodeProtectionOptions> options)
    {
        _protector = provider.CreateProtector(Purpose);
        _options = options.Value;
    }

    public string Protect(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        return _protector.Protect(plaintext);
    }

    public string Unprotect(string protectedValue)
    {
        ArgumentNullException.ThrowIfNull(protectedValue);
        return _protector.Unprotect(protectedValue);
    }

    public string ComputeFingerprint(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var key = GetFingerprintKey();
        var normalizedCode = plaintext.Trim();
        using var hmac = new HMACSHA256(key);
        var fingerprint = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalizedCode));
        return Convert.ToHexString(fingerprint);
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
            "Inventory code fingerprint key must be configured as a Base64 value containing at least 32 bytes.",
            innerException);
    }
}
