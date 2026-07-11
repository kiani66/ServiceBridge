namespace ServiceBridge.Application.Interfaces;

public interface IInventoryCodeProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
    string ComputeFingerprint(string plaintext);
}
