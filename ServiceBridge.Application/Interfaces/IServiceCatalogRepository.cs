using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface IServiceCatalogRepository
{
    Task<IReadOnlyCollection<DigitalService>> GetActiveServicesAsync();
    Task<DigitalService?> GetActiveServiceByIdAsync(Guid id);
    Task<DigitalService?> GetActiveServiceBySlugAsync(string slug);
    Task<IReadOnlyCollection<ServiceCategory>> GetAllCategoriesAsync();
    Task<IReadOnlyCollection<DigitalService>> GetAllServicesAsync();
    Task<Provider?> GetProviderBySlugAsync(string slug);
    Task<ServiceCategory?> GetCategoryByIdAsync(Guid id);
    Task<DigitalService?> GetServiceByIdAsync(Guid id);
    Task<ServiceVariant?> GetVariantByIdAsync(Guid id);
    Task<ServiceRequiredField?> GetRequiredFieldByIdAsync(Guid id);
    Task<bool> CategorySlugExistsAsync(string slug, Guid? excludingId = null);
    Task<bool> ServiceSlugExistsAsync(string slug, Guid? excludingId = null);
    Task<bool> VariantSlugExistsAsync(Guid serviceId, string slug, Guid? excludingId = null);
    Task<bool> FieldKeyExistsAsync(
        Guid serviceId,
        string fieldKey,
        OrderFulfillmentMode fulfillmentMode,
        CredentialActor actor,
        Guid? excludingId = null);
    Task AddCategoryAsync(ServiceCategory category);
    Task AddProviderAsync(Provider provider);
    Task AddServiceAsync(DigitalService service);
    Task AddVariantAsync(ServiceVariant plan);
    Task AddRequiredFieldAsync(ServiceRequiredField field);
    Task UpdateCategoryAsync(ServiceCategory category);
    Task UpdateServiceAsync(DigitalService service);
    Task UpdateVariantAsync(ServiceVariant plan);
    Task UpdateRequiredFieldAsync(ServiceRequiredField field);
}
