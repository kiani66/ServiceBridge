using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeOrderFieldProtector : IOrderFieldProtector
{
    public int ProtectCallCount { get; private set; }

    public string Protect(string value)
    {
        ProtectCallCount++;
        return $"protected::{value}";
    }

    public string Unprotect(string protectedValue)
    {
        return protectedValue["protected::".Length..];
    }
}
