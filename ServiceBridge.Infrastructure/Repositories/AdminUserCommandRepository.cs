using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class AdminUserCommandRepository : IAdminUserCommandRepository
{
    private readonly ServiceBridgeDbContext _context;

    public AdminUserCommandRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        return _context.Users.AnyAsync(x => x.Email == email);
    }

    public Task<User?> GetUserForUpdateAsync(Guid userId)
    {
        return _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
