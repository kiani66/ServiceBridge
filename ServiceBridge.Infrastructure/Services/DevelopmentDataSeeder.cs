using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Settings;
using ServiceBridge.Domain.Catalog;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Services;

public class DevelopmentDataSeeder : IDevelopmentDataSeeder
{
    private readonly ServiceBridgeDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public DevelopmentDataSeeder(
        ServiceBridgeDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task SeedAsync()
    {
        var settings = _configuration
            .GetSection(DevelopmentSeedSettings.SectionName)
            .Get<DevelopmentSeedSettings>();

        if (settings?.Enabled != true)
        {
            return;
        }

        ValidateSettings(settings);

        await SeedUsersAsync(settings);
        await SeedCatalogAsync();
        await SeedPricingAsync(settings);
        await _context.SaveChangesAsync();
    }

    private async Task SeedUsersAsync(DevelopmentSeedSettings settings)
    {
        await AddUserIfMissingAsync(
            "Test Customer",
            settings.TestUserEmail,
            UserRole.User);
        await AddUserIfMissingAsync(
            "Test Agent",
            settings.TestAgentEmail,
            UserRole.Agent);
    }

    private async Task AddUserIfMissingAsync(
        string fullName,
        string email,
        UserRole role)
    {
        if (await _context.Users.AnyAsync(x => x.Email == email))
        {
            return;
        }

        await _context.Users.AddAsync(new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email.Trim(),
            PasswordHash = _passwordHasher.Hash(CreateUnrecoverablePassword()),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task SeedCatalogAsync()
    {
        var aiCategory = await EnsureCategoryAsync(
            "AI Subscriptions",
            "ai-subscriptions",
            1);
        var developerCategory = await EnsureCategoryAsync(
            "Developer Tools",
            "developer-tools",
            2);
        var productivityCategory = await EnsureCategoryAsync(
            "Productivity Subscriptions",
            "productivity-subscriptions",
            3);

        await AddServiceIfMissingAsync(
            aiCategory,
            "ChatGPT Plus",
            "OpenAI",
            "chatgpt-plus",
            "ChatGPT Plus subscription purchase or renewal.",
            20m);

        await AddServiceIfMissingAsync(
            aiCategory,
            "Claude Pro",
            "Anthropic",
            "claude-pro",
            "Claude Pro subscription purchase or renewal.",
            20m);

        await AddServiceIfMissingAsync(
            aiCategory,
            "Gemini Advanced",
            "Google",
            "gemini-advanced",
            "Gemini Advanced subscription purchase or renewal.",
            20m);

        await AddServiceIfMissingAsync(
            aiCategory,
            "Perplexity Pro",
            "Perplexity",
            "perplexity-pro",
            "Perplexity Pro subscription purchase or renewal.",
            20m);

        await AddServiceIfMissingAsync(
            aiCategory,
            "Poe Premium",
            "Quora",
            "poe-premium",
            "Poe Premium subscription purchase or renewal.",
            20m);

        await AddServiceIfMissingAsync(
            aiCategory,
            "Midjourney Basic",
            "Midjourney",
            "midjourney-basic",
            "Midjourney Basic subscription purchase or renewal.",
            10m);

        await AddServiceIfMissingAsync(
            aiCategory,
            "ElevenLabs Starter",
            "ElevenLabs",
            "elevenlabs-starter",
            "ElevenLabs Starter subscription purchase or renewal.",
            5m);

        await AddServiceIfMissingAsync(
            aiCategory,
            "Runway Standard",
            "Runway",
            "runway-standard",
            "Runway Standard subscription purchase or renewal.",
            15m);

        await AddServiceIfMissingAsync(
            developerCategory,
            "Cursor Pro",
            "Cursor",
            "cursor-pro",
            "Cursor Pro subscription purchase or renewal.",
            20m);

        await AddServiceIfMissingAsync(
            developerCategory,
            "GitHub Copilot Individual",
            "GitHub",
            "github-copilot-individual",
            "GitHub Copilot Individual subscription purchase or renewal.",
            10m);

        await AddServiceIfMissingAsync(
            developerCategory,
            "JetBrains AI Pro",
            "JetBrains",
            "jetbrains-ai-pro",
            "JetBrains AI Pro subscription purchase or renewal.",
            10m);

        await AddServiceIfMissingAsync(
            productivityCategory,
            "Canva Pro",
            "Canva",
            "canva-pro",
            "Canva Pro subscription purchase or renewal.",
            15m);

        await AddServiceIfMissingAsync(
            productivityCategory,
            "Notion AI",
            "Notion",
            "notion-ai",
            "Notion AI subscription purchase or renewal.",
            10m);

        await AddServiceIfMissingAsync(
            productivityCategory,
            "Grammarly Pro",
            "Grammarly",
            "grammarly-pro",
            "Grammarly Pro subscription purchase or renewal.",
            12m);

        await AddServiceIfMissingAsync(
            productivityCategory,
            "Figma Professional",
            "Figma",
            "figma-professional",
            "Figma Professional subscription purchase or renewal.",
            12m);
    }

    private async Task<ServiceCategory> EnsureCategoryAsync(
        string name,
        string slug,
        int displayOrder)
    {
        var category = await _context.ServiceCategories
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (category != null)
        {
            category.Name = name;
            category.DisplayOrder = displayOrder;
            category.IsActive = true;
            category.UpdatedAt = DateTime.UtcNow;
            return category;
        }

        category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            DisplayOrder = displayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.ServiceCategories.AddAsync(category);
        return category;
    }

    private async Task AddServiceIfMissingAsync(
        ServiceCategory category,
        string name,
        string provider,
        string slug,
        string description,
        decimal basePrice)
    {
        var service = await _context.DigitalServices
            .FirstOrDefaultAsync(x => x.Slug == slug);
        var serviceProvider = await EnsureProviderAsync(provider);

        if (service == null)
        {
            service = new DigitalService
            {
                Id = Guid.NewGuid(),
                CategoryId = category.Id,
                ProviderId = serviceProvider.Id,
                Name = name,
                Provider = serviceProvider,
                Slug = slug,
                Description = description,
                CurrencyCode = "USD",
                BasePrice = basePrice,
                IsActive = true,
                SupportsUseExistingAccount = true,
                SupportsCreateNewAccount = true,
                ProcessingTimeDescription = "Usually completed within 1 to 6 hours.",
                CreatedAt = DateTime.UtcNow
            };
            await _context.DigitalServices.AddAsync(service);
        }
        else
        {
            service.CategoryId = category.Id;
            service.ProviderId = serviceProvider.Id;
            service.Provider = serviceProvider;
            service.Name = name;
            service.Description = description;
            service.CurrencyCode = "USD";
            service.BasePrice = basePrice;
            service.IsActive = true;
            service.SupportsUseExistingAccount = true;
            service.SupportsCreateNewAccount = true;
            service.ProcessingTimeDescription ??= "Usually completed within 1 to 6 hours.";
            service.UpdatedAt = DateTime.UtcNow;
        }

        await AddServiceFieldsIfMissingAsync(service, ExistingAccountCustomerFields());
        await AddServiceFieldsIfMissingAsync(service, NewAccountCustomerFields());
        await AddServiceFieldsIfMissingAsync(service, NewAccountAgentDeliveryFields());
        foreach (var plan in DefaultServiceVariants(basePrice))
        {
            await AddServiceVariantIfMissingAsync(service, plan);
        }
    }

    private async Task<Provider> EnsureProviderAsync(string name)
    {
        var providerName = ProviderSlugNormalizer.NormalizeName(name);
        var slug = ProviderSlugNormalizer.NormalizeSlug(providerName);
        var provider = await _context.Providers
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (provider != null)
        {
            provider.Name = providerName;
            provider.IsActive = true;
            provider.UpdatedAt = DateTime.UtcNow;
            return provider;
        }

        provider = new Provider
        {
            Id = Guid.NewGuid(),
            Name = providerName,
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Providers.AddAsync(provider);
        return provider;
    }

    private async Task AddServiceVariantIfMissingAsync(
        DigitalService service,
        SeedServiceVariant seedPlan)
    {
        var plan = await _context.ServiceVariants
            .FirstOrDefaultAsync(x =>
                x.ServiceId == service.Id &&
                x.Slug == seedPlan.Slug);

        if (plan == null)
        {
            await _context.ServiceVariants.AddAsync(new ServiceVariant
            {
                Id = Guid.NewGuid(),
                ServiceId = service.Id,
                Name = seedPlan.Name,
                Slug = seedPlan.Slug,
                DurationDays = seedPlan.DurationDays,
                RegionCode = seedPlan.RegionCode,
                CurrencyCode = seedPlan.CurrencyCode,
                FulfillmentMode = FulfillmentMode.ManualAgent,
                BasePrice = seedPlan.BasePrice,
                IsActive = true,
                DisplayOrder = seedPlan.DisplayOrder,
                ProcessingTimeDescription = service.ProcessingTimeDescription,
                CreatedAt = DateTime.UtcNow
            });
            return;
        }

        plan.Name = seedPlan.Name;
        plan.DurationDays = seedPlan.DurationDays;
        plan.RegionCode = seedPlan.RegionCode;
        plan.CurrencyCode = seedPlan.CurrencyCode;
        plan.FulfillmentMode = FulfillmentMode.ManualAgent;
        plan.BasePrice = seedPlan.BasePrice;
        plan.IsActive = true;
        plan.DisplayOrder = seedPlan.DisplayOrder;
        plan.ProcessingTimeDescription = service.ProcessingTimeDescription;
        plan.UpdatedAt = DateTime.UtcNow;
    }

    private static IReadOnlyCollection<SeedServiceVariant> DefaultServiceVariants(decimal basePrice)
    {
        return
        [
            new(
                "Monthly / Global",
                "monthly-global",
                30,
                "GLOBAL",
                "USD",
                basePrice,
                1),
            new(
                "Quarterly / Global",
                "quarterly-global",
                90,
                "GLOBAL",
                "USD",
                basePrice * 3,
                2),
            new(
                "Yearly / Global",
                "yearly-global",
                365,
                "GLOBAL",
                "USD",
                basePrice * 12,
                3)
        ];
    }

    private async Task AddServiceFieldsIfMissingAsync(
        DigitalService service,
        IReadOnlyCollection<SeedServiceField> fields)
    {
        var existingKeys = await _context.ServiceRequiredFields
            .Where(x =>
                x.ServiceId == service.Id &&
                fields.Select(field => field.FulfillmentMode).Contains(x.FulfillmentMode) &&
                fields.Select(field => field.Actor).Contains(x.Actor))
            .Select(x => new
            {
                x.FieldKey,
                x.FulfillmentMode,
                x.Actor
            })
            .ToListAsync();

        foreach (var field in fields)
        {
            var exists = existingKeys.Any(x =>
                x.FieldKey == field.Key &&
                x.FulfillmentMode == field.FulfillmentMode &&
                x.Actor == field.Actor);
            if (!exists)
            {
                await _context.ServiceRequiredFields.AddAsync(
                    new ServiceRequiredField
                    {
                        Id = Guid.NewGuid(),
                        ServiceId = service.Id,
                        FieldKey = field.Key,
                        Label = field.Label,
                        FieldType = field.Type,
                        FulfillmentMode = field.FulfillmentMode,
                        Actor = field.Actor,
                        IsRequired = field.Required,
                        IsSensitive = field.Sensitive,
                        IsVisibleToCustomerAfterCompletion =
                            field.VisibleToCustomerAfterCompletion,
                        DisplayOrder = field.DisplayOrder,
                        CreatedAt = DateTime.UtcNow
                    });
            }
        }
    }

    private static IReadOnlyCollection<SeedServiceField>
        ExistingAccountCustomerFields()
    {
        return
        [
            new(
                "account_email",
                "Account email",
                ServiceFieldType.Email,
                OrderFulfillmentMode.UseExistingAccount,
                CredentialActor.Customer,
                true,
                false,
                false,
                1),
            new(
                "account_password",
                "Account password",
                ServiceFieldType.Password,
                OrderFulfillmentMode.UseExistingAccount,
                CredentialActor.Customer,
                true,
                true,
                false,
                2),
            new(
                "login_note",
                "Login note or verification instructions",
                ServiceFieldType.Textarea,
                OrderFulfillmentMode.UseExistingAccount,
                CredentialActor.Customer,
                false,
                false,
                false,
                3)
        ];
    }

    private static IReadOnlyCollection<SeedServiceField>
        NewAccountCustomerFields()
    {
        return
        [
            new(
                "full_name",
                "Full name",
                ServiceFieldType.Text,
                OrderFulfillmentMode.CreateNewAccount,
                CredentialActor.Customer,
                true,
                false,
                false,
                1),
            new(
                "preferred_email",
                "Preferred email",
                ServiceFieldType.Email,
                OrderFulfillmentMode.CreateNewAccount,
                CredentialActor.Customer,
                false,
                false,
                false,
                2),
            new(
                "recovery_email",
                "Recovery email",
                ServiceFieldType.Email,
                OrderFulfillmentMode.CreateNewAccount,
                CredentialActor.Customer,
                false,
                false,
                false,
                3),
            new(
                "account_note",
                "Account creation note",
                ServiceFieldType.Textarea,
                OrderFulfillmentMode.CreateNewAccount,
                CredentialActor.Customer,
                false,
                false,
                false,
                4)
        ];
    }

    private static IReadOnlyCollection<SeedServiceField>
        NewAccountAgentDeliveryFields()
    {
        return
        [
            new(
                "created_email",
                "Created account email",
                ServiceFieldType.Email,
                OrderFulfillmentMode.CreateNewAccount,
                CredentialActor.Agent,
                true,
                false,
                true,
                1),
            new(
                "created_password",
                "Created account password",
                ServiceFieldType.Password,
                OrderFulfillmentMode.CreateNewAccount,
                CredentialActor.Agent,
                true,
                true,
                true,
                2),
            new(
                "recovery_info",
                "Recovery information",
                ServiceFieldType.Textarea,
                OrderFulfillmentMode.CreateNewAccount,
                CredentialActor.Agent,
                false,
                true,
                true,
                3),
            new(
                "delivery_note",
                "Delivery note",
                ServiceFieldType.Textarea,
                OrderFulfillmentMode.CreateNewAccount,
                CredentialActor.Agent,
                false,
                false,
                true,
                4)
        ];
    }

    private async Task SeedPricingAsync(DevelopmentSeedSettings settings)
    {
        if (!await _context.ExchangeRates.AnyAsync(x =>
                x.CurrencyCode == "USD" && x.IsActive))
        {
            await _context.ExchangeRates.AddAsync(new ExchangeRate
            {
                Id = Guid.NewGuid(),
                CurrencyCode = "USD",
                RateToIrr = settings.UsdRateToIrr,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await _context.ProfitMargins.AnyAsync(x =>
                x.CategoryId == null &&
                x.ServiceId == null &&
                x.IsActive))
        {
            await _context.ProfitMargins.AddAsync(new ProfitMargin
            {
                Id = Guid.NewGuid(),
                Percentage = settings.GlobalMarginPercentage,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private static void ValidateSettings(DevelopmentSeedSettings settings)
    {
        if (settings.UsdRateToIrr <= 0 ||
            settings.GlobalMarginPercentage is < 0 or > 100 ||
            string.IsNullOrWhiteSpace(settings.TestUserEmail) ||
            string.IsNullOrWhiteSpace(settings.TestAgentEmail))
        {
            throw new InvalidOperationException(
                "DevelopmentSeed configuration is invalid.");
        }
    }

    private static string CreateUnrecoverablePassword()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    }

    private sealed record SeedServiceField(
        string Key,
        string Label,
        ServiceFieldType Type,
        OrderFulfillmentMode FulfillmentMode,
        CredentialActor Actor,
        bool Required,
        bool Sensitive,
        bool VisibleToCustomerAfterCompletion,
        int DisplayOrder);

    private sealed record SeedServiceVariant(
        string Name,
        string Slug,
        int? DurationDays,
        string RegionCode,
        string CurrencyCode,
        decimal BasePrice,
        int DisplayOrder);
}
