using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminUserCommandRepository
{
    Task<bool> EmailExistsAsync(string email);
    Task<User?> GetUserForUpdateAsync(Guid userId);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
