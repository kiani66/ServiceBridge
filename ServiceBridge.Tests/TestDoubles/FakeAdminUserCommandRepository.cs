using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeAdminUserCommandRepository : IAdminUserCommandRepository
{
    private readonly Dictionary<Guid, User> _users = new();

    public int SaveCount { get; private set; }
    public IReadOnlyCollection<User> Users => _users.Values;

    public void Seed(User user)
    {
        _users[user.Id] = user;
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        return Task.FromResult(
            _users.Values.Any(x =>
                string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<User?> GetUserForUpdateAsync(Guid userId)
    {
        _users.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    public Task AddAsync(User user)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
