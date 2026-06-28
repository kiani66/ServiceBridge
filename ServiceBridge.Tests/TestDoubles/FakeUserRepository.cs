using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _users = new();

    public int GetByEmailCallCount { get; private set; }
    public int AddCallCount { get; private set; }
    public int UpdateCallCount { get; private set; }
    public int UpdatePasswordCallCount { get; private set; }
    public IReadOnlyCollection<User> Users => _users.Values;

    public void Seed(User user)
    {
        _users[user.Id] = user;
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        GetByEmailCallCount++;
        var user = _users.Values.FirstOrDefault(
            x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<User?> GetByIdAsync(Guid id)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task AddAsync(User user)
    {
        _users[user.Id] = user;
        AddCallCount++;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _users[user.Id] = user;
        UpdateCallCount++;
        return Task.CompletedTask;
    }

    public Task UpdatePasswordAsync(User user, string passwordHash)
    {
        user.PasswordHash = passwordHash;
        user.UpdatedAt = DateTime.UtcNow;
        _users[user.Id] = user;
        UpdatePasswordCallCount++;
        return Task.CompletedTask;
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        var exists = _users.Values.Any(
            x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task<bool> AdminExistsAsync()
    {
        return Task.FromResult(
            _users.Values.Any(x => x.Role == UserRole.Admin));
    }
}
