using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class ServiceCatalogRepository : IServiceCatalogRepository
{
    private readonly ServiceBridgeDbContext _context;

    public ServiceCatalogRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<DigitalService>> GetActiveServicesAsync()
    {
        return await _context.DigitalServices
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Provider)
            .Include(x => x.Variants.Where(plan => plan.IsActive).OrderBy(plan => plan.DisplayOrder))
            .Where(x => x.IsActive && x.Category.IsActive && x.Provider.IsActive)
            .OrderBy(x => x.Category.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<DigitalService?> GetActiveServiceByIdAsync(Guid id)
    {
        return await _context.DigitalServices
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Provider)
            .Include(x => x.Variants.Where(plan => plan.IsActive).OrderBy(plan => plan.DisplayOrder))
            .Include(x => x.RequiredFields.OrderBy(field => field.DisplayOrder))
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.IsActive &&
                x.Category.IsActive &&
                x.Provider.IsActive);
    }

    public async Task<DigitalService?> GetActiveServiceBySlugAsync(string slug)
    {
        return await _context.DigitalServices
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Provider)
            .Include(x => x.Variants.Where(plan => plan.IsActive).OrderBy(plan => plan.DisplayOrder))
            .Include(x => x.RequiredFields.OrderBy(field => field.DisplayOrder))
            .FirstOrDefaultAsync(x =>
                x.Slug == slug &&
                x.IsActive &&
                x.Category.IsActive &&
                x.Provider.IsActive);
    }

    public async Task<IReadOnlyCollection<ServiceCategory>> GetAllCategoriesAsync()
    {
        return await _context.ServiceCategories
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<DigitalService>> GetAllServicesAsync()
    {
        return await _context.DigitalServices
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Provider)
            .Include(x => x.Variants.OrderBy(plan => plan.DisplayOrder))
            .OrderBy(x => x.Category.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<ServiceCategory?> GetCategoryByIdAsync(Guid id)
    {
        return await _context.ServiceCategories
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Provider?> GetProviderBySlugAsync(string slug)
    {
        return await _context.Providers
            .FirstOrDefaultAsync(x => x.Slug == slug);
    }

    public async Task<DigitalService?> GetServiceByIdAsync(Guid id)
    {
        return await _context.DigitalServices
            .Include(x => x.Category)
            .Include(x => x.Provider)
            .Include(x => x.Variants.OrderBy(plan => plan.DisplayOrder))
            .Include(x => x.RequiredFields.OrderBy(field => field.DisplayOrder))
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<ServiceRequiredField?> GetRequiredFieldByIdAsync(Guid id)
    {
        return await _context.ServiceRequiredFields
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<ServiceVariant?> GetVariantByIdAsync(Guid id)
    {
        return await _context.ServiceVariants
            .Include(x => x.Service)
            .ThenInclude(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> CategorySlugExistsAsync(string slug, Guid? excludingId = null)
    {
        return await _context.ServiceCategories.AnyAsync(x =>
            x.Slug == slug &&
            (!excludingId.HasValue || x.Id != excludingId.Value));
    }

    public async Task<bool> ServiceSlugExistsAsync(string slug, Guid? excludingId = null)
    {
        return await _context.DigitalServices.AnyAsync(x =>
            x.Slug == slug &&
            (!excludingId.HasValue || x.Id != excludingId.Value));
    }

    public async Task<bool> VariantSlugExistsAsync(
        Guid serviceId,
        string slug,
        Guid? excludingId = null)
    {
        return await _context.ServiceVariants.AnyAsync(x =>
            x.ServiceId == serviceId &&
            x.Slug == slug &&
            (!excludingId.HasValue || x.Id != excludingId.Value));
    }

    public async Task<bool> FieldKeyExistsAsync(
        Guid serviceId,
        string fieldKey,
        OrderFulfillmentMode fulfillmentMode,
        CredentialActor actor,
        Guid? excludingId = null)
    {
        return await _context.ServiceRequiredFields.AnyAsync(x =>
            x.ServiceId == serviceId &&
            x.FulfillmentMode == fulfillmentMode &&
            x.Actor == actor &&
            x.FieldKey == fieldKey &&
            (!excludingId.HasValue || x.Id != excludingId.Value));
    }

    public async Task AddCategoryAsync(ServiceCategory category)
    {
        await _context.ServiceCategories.AddAsync(category);
        await _context.SaveChangesAsync();
    }

    public async Task AddProviderAsync(Provider provider)
    {
        await _context.Providers.AddAsync(provider);
        await _context.SaveChangesAsync();
    }

    public async Task AddServiceAsync(DigitalService service)
    {
        await _context.DigitalServices.AddAsync(service);
        await _context.SaveChangesAsync();
    }

    public async Task AddVariantAsync(ServiceVariant plan)
    {
        await _context.ServiceVariants.AddAsync(plan);
        await _context.SaveChangesAsync();
    }

    public async Task AddRequiredFieldAsync(ServiceRequiredField field)
    {
        await _context.ServiceRequiredFields.AddAsync(field);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateCategoryAsync(ServiceCategory category)
    {
        category.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateServiceAsync(DigitalService service)
    {
        service.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateVariantAsync(ServiceVariant plan)
    {
        plan.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRequiredFieldAsync(ServiceRequiredField field)
    {
        field.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
