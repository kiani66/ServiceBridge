using Microsoft.Extensions.Configuration;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Services;
using ServiceBridge.Application.Settings;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Infrastructure.Services;

public class AdminSeeder : IAdminSeeder
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public AdminSeeder(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task SeedAsync()
    {
        var settings = GetRequiredSettings();

        if (await _userRepository.AdminExistsAsync())
        {
            return;
        }

        await _userRepository.AddAsync(new User
        {
            Id = Guid.NewGuid(),
            Email = settings.Email.Trim(),
            FullName = settings.FullName.Trim(),
            PasswordHash = _passwordHasher.Hash(settings.Password),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
    }

    private AdminSeedSettings GetRequiredSettings()
    {
        var section = _configuration.GetSection(AdminSeedSettings.SectionName);
        if (!section.Exists())
        {
            throw new InvalidOperationException(
                "AdminSeed configuration section is missing.");
        }

        var settings = section.Get<AdminSeedSettings>();
        if (settings == null ||
            string.IsNullOrWhiteSpace(settings.Email) ||
            string.IsNullOrWhiteSpace(settings.FullName) ||
            !PasswordPolicy.IsValid(settings.Password))
        {
            throw new InvalidOperationException(
                "AdminSeed configuration must contain a valid Email, FullName, and Password.");
        }

        return settings;
    }
}
