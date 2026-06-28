using Microsoft.Extensions.Configuration;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class AdminSeederTests
{
    [Fact]
    public async Task SeedAsync_WhenNoAdminExists_CreatesAdmin()
    {
        var repository = new FakeUserRepository();
        var seeder = CreateSeeder(repository, ValidConfiguration());

        await seeder.SeedAsync();

        var admin = Assert.Single(repository.Users);
        Assert.Equal(UserRole.Admin, admin.Role);
        Assert.Equal("alikianigpt@gmail.com", admin.Email);
        Assert.Equal("System Admin", admin.FullName);
        Assert.True(admin.IsActive);
        Assert.NotEqual(Guid.Empty, admin.Id);
        Assert.True(
            BCrypt.Net.BCrypt.Verify("ValidSeedPassword1!", admin.PasswordHash));
        Assert.Equal(1, repository.AddCallCount);
    }

    [Fact]
    public async Task SeedAsync_WhenAdminExists_DoesNotCreateDuplicate()
    {
        var repository = new FakeUserRepository();
        repository.Seed(new User
        {
            Email = "existing@paybridge.test",
            FullName = "Existing Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Existing123!"),
            Role = UserRole.Admin,
            IsActive = true
        });
        var seeder = CreateSeeder(repository, ValidConfiguration());

        await seeder.SeedAsync();

        Assert.Single(repository.Users);
        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task SeedAsync_WhenConfigurationIsMissing_ThrowsClearException()
    {
        var repository = new FakeUserRepository();
        var configuration = new ConfigurationBuilder().Build();
        var seeder = CreateSeeder(repository, configuration);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => seeder.SeedAsync());

        Assert.Equal(
            "AdminSeed configuration section is missing.",
            exception.Message);
        Assert.Equal(0, repository.AddCallCount);
    }

    private static AdminSeeder CreateSeeder(
        FakeUserRepository repository,
        IConfiguration configuration)
    {
        return new AdminSeeder(
            repository,
            new BcryptPasswordHasher(),
            configuration);
    }

    private static IConfiguration ValidConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminSeed:Email"] = "alikianigpt@gmail.com",
                ["AdminSeed:Password"] = "ValidSeedPassword1!",
                ["AdminSeed:FullName"] = "System Admin"
            })
            .Build();
    }
}
