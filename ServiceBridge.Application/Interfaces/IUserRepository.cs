using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task<bool> AdminExistsAsync();
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task UpdatePasswordAsync(User user, string passwordHash);
    Task<bool> EmailExistsAsync(string email);
}
