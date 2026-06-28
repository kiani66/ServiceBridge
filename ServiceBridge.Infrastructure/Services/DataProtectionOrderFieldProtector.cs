using Microsoft.AspNetCore.DataProtection;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Infrastructure.Services;

public class DataProtectionOrderFieldProtector : IOrderFieldProtector
{
    private const string Purpose = "ServiceBridge.OrderFieldValues.v1";
    private readonly IDataProtector _protector;

    public DataProtectionOrderFieldProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string value)
    {
        return _protector.Protect(value);
    }

    public string Unprotect(string protectedValue)
    {
        return _protector.Unprotect(protectedValue);
    }
}
