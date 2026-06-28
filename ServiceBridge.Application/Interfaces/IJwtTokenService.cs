using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    DateTime GetExpirationUtc();
}
