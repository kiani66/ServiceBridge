using System.Security.Cryptography;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Infrastructure.Services;

public class OrderNumberGenerator : IOrderNumberGenerator
{
    public string Generate(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Order number date must be UTC.", nameof(utcNow));
        }

        var suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
        return $"PB-{utcNow:yyyyMMdd}-{suffix}";
    }
}
