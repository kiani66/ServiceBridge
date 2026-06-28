using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Infrastructure.Services;

internal static class ServiceCatalogMapper
{
    public static ServiceCategoryResponse MapCategory(ServiceCategory category)
    {
        return new ServiceCategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            IsActive = category.IsActive,
            DisplayOrder = category.DisplayOrder
        };
    }

    public static ServiceSummaryResponse MapSummary(DigitalService service)
    {
        return new ServiceSummaryResponse
        {
            Id = service.Id,
            CategoryId = service.CategoryId,
            CategoryName = service.Category.Name,
            Name = service.Name,
            Provider = service.Provider.Name,
            Slug = service.Slug,
            Description = service.Description,
            LogoUrl = service.LogoUrl,
            CurrencyCode = GetDisplayCurrencyCode(service),
            BasePrice = GetDisplayBasePrice(service),
            ProcessingTimeDescription = service.ProcessingTimeDescription
        };
    }

    public static AdminServiceSummaryResponse MapAdminSummary(DigitalService service)
    {
        return new AdminServiceSummaryResponse
        {
            Id = service.Id,
            CategoryId = service.CategoryId,
            CategoryName = service.Category.Name,
            Name = service.Name,
            Provider = service.Provider.Name,
            Slug = service.Slug,
            Description = service.Description,
            LogoUrl = service.LogoUrl,
            CurrencyCode = GetDisplayCurrencyCode(service),
            BasePrice = GetDisplayBasePrice(service),
            ProcessingTimeDescription = service.ProcessingTimeDescription,
            IsActive = service.IsActive
        };
    }

    public static ServiceDetailResponse MapDetail(DigitalService service)
    {
        return new ServiceDetailResponse
        {
            Id = service.Id,
            CategoryId = service.CategoryId,
            CategoryName = service.Category.Name,
            Name = service.Name,
            Provider = service.Provider.Name,
            Slug = service.Slug,
            Description = service.Description,
            LogoUrl = service.LogoUrl,
            CurrencyCode = GetDisplayCurrencyCode(service),
            BasePrice = GetDisplayBasePrice(service),
            ProcessingTimeDescription = service.ProcessingTimeDescription,
            SupportsUseExistingAccount = service.SupportsUseExistingAccount,
            SupportsCreateNewAccount = service.SupportsCreateNewAccount,
            SupportedFulfillmentModes = GetSupportedFulfillmentModes(service),
            Plans = service.Variants
                .OrderBy(x => x.DisplayOrder)
                .Select(MapVariant)
                .ToArray(),
            RequiredFields = service.RequiredFields
                .OrderBy(x => x.DisplayOrder)
                .Select(MapField)
                .ToArray()
        };
    }

    public static AdminServiceResponse MapAdminDetail(DigitalService service)
    {
        return new AdminServiceResponse
        {
            Id = service.Id,
            CategoryId = service.CategoryId,
            CategoryName = service.Category.Name,
            Name = service.Name,
            Provider = service.Provider.Name,
            Slug = service.Slug,
            Description = service.Description,
            LogoUrl = service.LogoUrl,
            CurrencyCode = GetDisplayCurrencyCode(service),
            BasePrice = GetDisplayBasePrice(service),
            ProcessingTimeDescription = service.ProcessingTimeDescription,
            IsActive = service.IsActive,
            SupportsUseExistingAccount = service.SupportsUseExistingAccount,
            SupportsCreateNewAccount = service.SupportsCreateNewAccount,
            SupportedFulfillmentModes = GetSupportedFulfillmentModes(service),
            Plans = service.Variants
                .OrderBy(x => x.DisplayOrder)
                .Select(MapVariant)
                .ToArray(),
            RequiredFields = service.RequiredFields
                .OrderBy(x => x.DisplayOrder)
                .Select(MapField)
                .ToArray()
        };
    }

    public static ServiceRequiredFieldResponse MapField(ServiceRequiredField field)
    {
        return new ServiceRequiredFieldResponse
        {
            Id = field.Id,
            FieldKey = field.FieldKey,
            Label = field.Label,
            FieldType = field.FieldType.ToString(),
            FulfillmentMode = field.FulfillmentMode,
            Actor = field.Actor,
            IsRequired = field.IsRequired,
            IsSensitive = field.IsSensitive,
            IsVisibleToCustomerAfterCompletion =
                field.IsVisibleToCustomerAfterCompletion,
            DisplayOrder = field.DisplayOrder,
            OptionsJson = field.OptionsJson
        };
    }

    public static ServiceVariantResponse MapVariant(ServiceVariant plan)
    {
        return new ServiceVariantResponse
        {
            Id = plan.Id,
            Name = plan.Name,
            Slug = plan.Slug,
            DurationDays = plan.DurationDays,
            RegionCode = plan.RegionCode,
            CurrencyCode = plan.CurrencyCode,
            BasePrice = plan.BasePrice,
            IsActive = plan.IsActive,
            DisplayOrder = plan.DisplayOrder,
            ProcessingTimeDescription = plan.ProcessingTimeDescription
        };
    }

    private static IReadOnlyCollection<OrderFulfillmentMode>
        GetSupportedFulfillmentModes(DigitalService service)
    {
        var modes = new List<OrderFulfillmentMode>();
        if (service.SupportsUseExistingAccount)
        {
            modes.Add(OrderFulfillmentMode.UseExistingAccount);
        }

        if (service.SupportsCreateNewAccount)
        {
            modes.Add(OrderFulfillmentMode.CreateNewAccount);
        }

        return modes;
    }

    private static decimal GetDisplayBasePrice(DigitalService service)
    {
        return service.Variants
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => (decimal?)x.BasePrice)
            .FirstOrDefault() ?? service.BasePrice;
    }

    private static string GetDisplayCurrencyCode(DigitalService service)
    {
        return service.Variants
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => x.CurrencyCode)
            .FirstOrDefault() ?? service.CurrencyCode;
    }
}
