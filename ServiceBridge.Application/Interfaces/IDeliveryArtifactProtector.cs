namespace ServiceBridge.Application.Interfaces;

public interface IDeliveryArtifactProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
    string ComputeFingerprint(string plaintext);
}
