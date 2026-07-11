using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakeInventoryCodeProtector : IInventoryCodeProtector
{
    public bool ThrowOnFingerprint { get; set; }
    public int ProtectCallCount { get; private set; }
    public List<string> FingerprintInputs { get; } = new();

    public string Protect(string plaintext)
    {
        ProtectCallCount++;
        return $"protected:{plaintext}";
    }

    public string Unprotect(string protectedValue) => protectedValue["protected:".Length..];

    public string ComputeFingerprint(string plaintext)
    {
        if (ThrowOnFingerprint)
        {
            throw new InvalidOperationException("Fingerprint key unavailable.");
        }

        FingerprintInputs.Add(plaintext);
        return $"fingerprint:{plaintext}";
    }
}
