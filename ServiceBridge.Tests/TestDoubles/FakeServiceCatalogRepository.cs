using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeServiceCatalogRepository : IServiceCatalogRepository
{
    private readonly Dictionary<Guid, ServiceCategory> _categories = new();
    private readonly Dictionary<Guid, DigitalService> _services = new();
    private readonly Dictionary<Guid, ServicePlan> _plans = new();
    private readonly Dictionary<Guid, ServiceRequiredField> _fields = new();

    public void SeedCategory(ServiceCategory category)
    {
        _categories[category.Id] = category;
    }

    public void SeedService(DigitalService service)
    {
        _services[service.Id] = service;
        SeedCategory(service.Category);

        foreach (var field in service.RequiredFields)
        {
            _fields[field.Id] = field;
        }

        foreach (var plan in service.Plans)
        {
            _plans[plan.Id] = plan;
        }
    }

    public Task<IReadOnlyCollection<DigitalService>> GetActiveServicesAsync()
    {
        IReadOnlyCollection<DigitalService> result = _services.Values
            .Where(x => x.IsActive && x.Category.IsActive)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task<DigitalService?> GetActiveServiceByIdAsync(Guid id)
    {
        var service = _services.GetValueOrDefault(id);
        return Task.FromResult(
            service is { IsActive: true } && service.Category.IsActive ? service : null);
    }

    public Task<IReadOnlyCollection<ServiceCategory>> GetAllCategoriesAsync()
    {
        IReadOnlyCollection<ServiceCategory> result = _categories.Values.ToArray();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<DigitalService>> GetAllServicesAsync()
    {
        IReadOnlyCollection<DigitalService> result = _services.Values.ToArray();
        return Task.FromResult(result);
    }

    public Task<ServiceCategory?> GetCategoryByIdAsync(Guid id)
    {
        return Task.FromResult(_categories.GetValueOrDefault(id));
    }

    public Task<DigitalService?> GetServiceByIdAsync(Guid id)
    {
        return Task.FromResult(_services.GetValueOrDefault(id));
    }

    public Task<ServiceRequiredField?> GetRequiredFieldByIdAsync(Guid id)
    {
        return Task.FromResult(_fields.GetValueOrDefault(id));
    }

    public Task<ServicePlan?> GetPlanByIdAsync(Guid id)
    {
        return Task.FromResult(_plans.GetValueOrDefault(id));
    }

    public Task<bool> CategorySlugExistsAsync(string slug, Guid? excludingId = null)
    {
        return Task.FromResult(_categories.Values.Any(x =>
            x.Slug == slug &&
            (!excludingId.HasValue || x.Id != excludingId.Value)));
    }

    public Task<bool> ServiceSlugExistsAsync(string slug, Guid? excludingId = null)
    {
        return Task.FromResult(_services.Values.Any(x =>
            x.Slug == slug &&
            (!excludingId.HasValue || x.Id != excludingId.Value)));
    }

    public Task<bool> PlanSlugExistsAsync(
        Guid serviceId,
        string slug,
        Guid? excludingId = null)
    {
        return Task.FromResult(_plans.Values.Any(x =>
            x.ServiceId == serviceId &&
            x.Slug == slug &&
            (!excludingId.HasValue || x.Id != excludingId.Value)));
    }

    public Task<bool> FieldKeyExistsAsync(
        Guid serviceId,
        string fieldKey,
        OrderFulfillmentMode fulfillmentMode,
        CredentialActor actor,
        Guid? excludingId = null)
    {
        return Task.FromResult(_fields.Values.Any(x =>
            x.ServiceId == serviceId &&
            x.FulfillmentMode == fulfillmentMode &&
            x.Actor == actor &&
            x.FieldKey == fieldKey &&
            (!excludingId.HasValue || x.Id != excludingId.Value)));
    }

    public Task AddCategoryAsync(ServiceCategory category)
    {
        _categories[category.Id] = category;
        return Task.CompletedTask;
    }

    public Task AddServiceAsync(DigitalService service)
    {
        _services[service.Id] = service;
        return Task.CompletedTask;
    }

    public Task AddPlanAsync(ServicePlan plan)
    {
        _plans[plan.Id] = plan;
        plan.Service.Plans.Add(plan);
        return Task.CompletedTask;
    }

    public Task AddRequiredFieldAsync(ServiceRequiredField field)
    {
        _fields[field.Id] = field;
        field.Service.RequiredFields.Add(field);
        return Task.CompletedTask;
    }

    public Task UpdateCategoryAsync(ServiceCategory category)
    {
        category.UpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task UpdateServiceAsync(DigitalService service)
    {
        service.UpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task UpdatePlanAsync(ServicePlan plan)
    {
        plan.UpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task UpdateRequiredFieldAsync(ServiceRequiredField field)
    {
        field.UpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}
