using Microsoft.AspNetCore.DataProtection;
using ServiceBridge.Infrastructure.Services;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class OrderSecurityServicesTests
{
    [Fact]
    public void DataProtectionOrderFieldProtector_RoundTripsWithoutPlaintextStorage()
    {
        var protector = new DataProtectionOrderFieldProtector(
            new EphemeralDataProtectionProvider());

        var protectedValue = protector.Protect("secret-value");

        Assert.NotEqual("secret-value", protectedValue);
        Assert.Equal("secret-value", protector.Unprotect(protectedValue));
    }

    [Fact]
    public void OrderNumberGenerator_GeneratesExpectedFormat()
    {
        var generator = new OrderNumberGenerator();
        var utcNow = new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc);

        var orderNumber = generator.Generate(utcNow);

        Assert.Matches("^PB-20260621-[A-F0-9]{8}$", orderNumber);
    }
}
