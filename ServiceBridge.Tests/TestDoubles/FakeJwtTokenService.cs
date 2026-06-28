using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeJwtTokenService : IJwtTokenService
{
    public const string Token = "test-access-token";
    public static readonly DateTime ExpirationUtc = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public int GenerateTokenCallCount { get; private set; }

    public string GenerateToken(User user)
    {
        GenerateTokenCallCount++;
        return Token;
    }

    public DateTime GetExpirationUtc()
    {
        return ExpirationUtc;
    }
}
