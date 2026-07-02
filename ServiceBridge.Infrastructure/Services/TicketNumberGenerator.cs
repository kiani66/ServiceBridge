using System.Security.Cryptography;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Infrastructure.Services;

public class TicketNumberGenerator : ITicketNumberGenerator
{
    public string Generate(DateTime createdAtUtc)
    {
        return $"TKT-{createdAtUtc:yyyyMMdd}-{RandomNumberGenerator.GetInt32(0, 1_000_000):D6}";
    }
}
