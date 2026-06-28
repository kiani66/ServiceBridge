using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeOrderFieldProtector : IOrderFieldProtector
{
    public string Protect(string value)
    {
        return $"protected::{value}";
    }

    public string Unprotect(string protectedValue)
    {
        return protectedValue["protected::".Length..];
    }
}
