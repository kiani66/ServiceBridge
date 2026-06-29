using Microsoft.Extensions.Logging;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Catalog;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class AdminCatalogService : IAdminCatalogService
{
    private readonly IServiceCatalogRepository _repository;
    private readonly ILogger<AdminCatalogService> _logger;

    public AdminCatalogService(
        IServiceCatalogRepository repository,
        ILogger<AdminCatalogService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ServiceResult<ServiceCategoryResponse>> CreateCategoryAsync(
        CreateServiceCategoryRequest request)
    {
        var slug = NormalizeSlug(request.Slug);
        if (await _repository.CategorySlugExistsAsync(slug))
        {
            return ServiceResult<ServiceCategoryResponse>.Fail(
                AppMessages.DuplicateCategorySlug,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var category = new ServiceCategory
        {
            Name = request.Name.Trim(),
            Slug = slug,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive
        };

        await _repository.AddCategoryAsync(category);
        _logger.LogInformation("Service category {CategoryId} created.", category.Id);

        return ServiceResult<ServiceCategoryResponse>.Created(
            ServiceCatalogMapper.MapCategory(category),
            AppMessages.CategoryCreated);
    }

    public async Task<ServiceResult<ServiceCategoryResponse>> UpdateCategoryAsync(
        Guid id,
        UpdateServiceCategoryRequest request)
    {
        var category = await _repository.GetCategoryByIdAsync(id);
        if (category == null)
        {
            return ServiceResult<ServiceCategoryResponse>.Fail(
                AppMessages.CategoryNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var slug = NormalizeSlug(request.Slug);
        if (await _repository.CategorySlugExistsAsync(slug, id))
        {
            return ServiceResult<ServiceCategoryResponse>.Fail(
                AppMessages.DuplicateCategorySlug,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        category.Name = request.Name.Trim();
        category.Slug = slug;
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;

        await _repository.UpdateCategoryAsync(category);
        return ServiceResult<ServiceCategoryResponse>.Ok(
            ServiceCatalogMapper.MapCategory(category),
            AppMessages.CategoryUpdated);
    }

    public async Task<ServiceResult<ServiceDetailResponse>> CreateServiceAsync(
        CreateDigitalServiceRequest request)
    {
        var supportValidation = ValidateSupportedFulfillmentModes(request);
        if (supportValidation != null)
        {
            return supportValidation;
        }

        var category = await _repository.GetCategoryByIdAsync(request.CategoryId);
        if (category == null)
        {
            return ServiceResult<ServiceDetailResponse>.Fail(
                AppMessages.CategoryNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var slug = NormalizeSlug(request.Slug);
        if (await _repository.ServiceSlugExistsAsync(slug))
        {
            return ServiceResult<ServiceDetailResponse>.Fail(
                AppMessages.DuplicateServiceSlug,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var provider = await ResolveProviderAsync(request.Provider);
        var service = new DigitalService
        {
            CategoryId = category.Id,
            Category = category,
            ProviderId = provider.Id,
            Provider = provider,
            Name = request.Name.Trim(),
            Slug = slug,
            Description = NormalizeOptional(request.Description),
            LogoUrl = NormalizeOptional(request.LogoUrl),
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            BasePrice = request.BasePrice,
            IsActive = request.IsActive,
            SupportsUseExistingAccount = request.SupportsUseExistingAccount,
            SupportsCreateNewAccount = request.SupportsCreateNewAccount,
            ProcessingTimeDescription = NormalizeOptional(request.ProcessingTimeDescription)
        };

        await _repository.AddServiceAsync(service);
        _logger.LogInformation("Digital service {ServiceId} created.", service.Id);

        return ServiceResult<ServiceDetailResponse>.Created(
            ServiceCatalogMapper.MapDetail(service),
            AppMessages.ServiceCreated);
    }

    public async Task<ServiceResult<ServiceDetailResponse>> UpdateServiceAsync(
        Guid id,
        UpdateDigitalServiceRequest request)
    {
        var supportValidation = ValidateSupportedFulfillmentModes(request);
        if (supportValidation != null)
        {
            return supportValidation;
        }

        var service = await _repository.GetServiceByIdAsync(id);
        if (service == null)
        {
            return ServiceResult<ServiceDetailResponse>.Fail(
                AppMessages.ServiceNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var category = await _repository.GetCategoryByIdAsync(request.CategoryId);
        if (category == null)
        {
            return ServiceResult<ServiceDetailResponse>.Fail(
                AppMessages.CategoryNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var slug = NormalizeSlug(request.Slug);
        if (await _repository.ServiceSlugExistsAsync(slug, id))
        {
            return ServiceResult<ServiceDetailResponse>.Fail(
                AppMessages.DuplicateServiceSlug,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var provider = await ResolveProviderAsync(request.Provider);

        service.CategoryId = category.Id;
        service.Category = category;
        service.ProviderId = provider.Id;
        service.Provider = provider;
        service.Name = request.Name.Trim();
        service.Slug = slug;
        service.Description = NormalizeOptional(request.Description);
        service.LogoUrl = NormalizeOptional(request.LogoUrl);
        service.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        service.BasePrice = request.BasePrice;
        service.IsActive = request.IsActive;
        service.SupportsUseExistingAccount = request.SupportsUseExistingAccount;
        service.SupportsCreateNewAccount = request.SupportsCreateNewAccount;
        service.ProcessingTimeDescription = NormalizeOptional(request.ProcessingTimeDescription);

        await _repository.UpdateServiceAsync(service);
        return ServiceResult<ServiceDetailResponse>.Ok(
            ServiceCatalogMapper.MapDetail(service),
            AppMessages.ServiceUpdated);
    }

    public async Task<ServiceResult<ServiceVariantResponse>> CreateVariantAsync(
        Guid serviceId,
        CreateServiceVariantRequest request)
    {
        var service = await _repository.GetServiceByIdAsync(serviceId);
        if (service == null)
        {
            return ServiceResult<ServiceVariantResponse>.Fail(
                AppMessages.ServiceNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var slug = NormalizeSlug(request.Slug);
        if (await _repository.VariantSlugExistsAsync(serviceId, slug))
        {
            return ServiceResult<ServiceVariantResponse>.Fail(
                AppMessages.DuplicateVariantSlug,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var variant = new ServiceVariant
        {
            ServiceId = service.Id,
            Service = service,
            Name = request.Name.Trim(),
            Slug = slug,
            DurationDays = request.DurationDays,
            BillingCycle = request.BillingCycle,
            AccountDeliveryType = request.AccountDeliveryType,
            RegionCode = NormalizeOptional(request.RegionCode)?.ToUpperInvariant(),
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            BasePrice = request.BasePrice,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
            ProcessingTimeDescription = NormalizeOptional(
                request.ProcessingTimeDescription)
        };

        await _repository.AddVariantAsync(variant);
        _logger.LogInformation(
            "service variant {variantId} created for service {ServiceId}.",
            variant.Id,
            serviceId);

        return ServiceResult<ServiceVariantResponse>.Created(
            ServiceCatalogMapper.MapVariant(variant),
            AppMessages.ServiceVariantCreated);
    }

    public async Task<ServiceResult<ServiceVariantResponse>> UpdateVariantAsync(
        Guid id,
        UpdateServiceVariantRequest request)
    {
        var variant = await _repository.GetVariantByIdAsync(id);
        if (variant == null)
        {
            return ServiceResult<ServiceVariantResponse>.Fail(
                AppMessages.ServiceVariantNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var slug = NormalizeSlug(request.Slug);
        if (await _repository.VariantSlugExistsAsync(variant.ServiceId, slug, id))
        {
            return ServiceResult<ServiceVariantResponse>.Fail(
                AppMessages.DuplicateVariantSlug,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        variant.Name = request.Name.Trim();
        variant.Slug = slug;
        variant.DurationDays = request.DurationDays;
        variant.BillingCycle = request.BillingCycle;
        variant.AccountDeliveryType = request.AccountDeliveryType;
        variant.RegionCode = NormalizeOptional(request.RegionCode)?.ToUpperInvariant();
        variant.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        variant.BasePrice = request.BasePrice;
        variant.IsActive = request.IsActive;
        variant.DisplayOrder = request.DisplayOrder;
        variant.ProcessingTimeDescription = NormalizeOptional(
            request.ProcessingTimeDescription);

        await _repository.UpdateVariantAsync(variant);
        return ServiceResult<ServiceVariantResponse>.Ok(
            ServiceCatalogMapper.MapVariant(variant),
            AppMessages.ServiceVariantUpdated);
    }

    public async Task<ServiceResult<ServiceRequiredFieldResponse>> CreateRequiredFieldAsync(
        Guid serviceId,
        CreateServiceRequiredFieldRequest request)
    {
        var service = await _repository.GetServiceByIdAsync(serviceId);
        if (service == null)
        {
            return ServiceResult<ServiceRequiredFieldResponse>.Fail(
                AppMessages.ServiceNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var fieldKey = NormalizeFieldKey(request.FieldKey);
        if (await _repository.FieldKeyExistsAsync(
                serviceId,
                fieldKey,
                request.FulfillmentMode,
                request.Actor))
        {
            return ServiceResult<ServiceRequiredFieldResponse>.Fail(
                AppMessages.DuplicateFieldKey,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var field = new ServiceRequiredField
        {
            ServiceId = serviceId,
            Service = service,
            FieldKey = fieldKey,
            Label = request.Label.Trim(),
            FieldType = request.FieldType,
            FulfillmentMode = request.FulfillmentMode,
            Actor = request.Actor,
            IsRequired = request.IsRequired,
            IsSensitive = request.IsSensitive,
            IsVisibleToCustomerAfterCompletion =
                request.IsVisibleToCustomerAfterCompletion,
            DisplayOrder = request.DisplayOrder,
            OptionsJson = NormalizeOptional(request.OptionsJson)
        };

        await _repository.AddRequiredFieldAsync(field);
        return ServiceResult<ServiceRequiredFieldResponse>.Created(
            ServiceCatalogMapper.MapField(field),
            AppMessages.RequiredFieldCreated);
    }

    public async Task<ServiceResult<ServiceRequiredFieldResponse>> UpdateRequiredFieldAsync(
        Guid id,
        UpdateServiceRequiredFieldRequest request)
    {
        var field = await _repository.GetRequiredFieldByIdAsync(id);
        if (field == null)
        {
            return ServiceResult<ServiceRequiredFieldResponse>.Fail(
                AppMessages.RequiredFieldNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var fieldKey = NormalizeFieldKey(request.FieldKey);
        if (await _repository.FieldKeyExistsAsync(
                field.ServiceId,
                fieldKey,
                request.FulfillmentMode,
                request.Actor,
                id))
        {
            return ServiceResult<ServiceRequiredFieldResponse>.Fail(
                AppMessages.DuplicateFieldKey,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        field.FieldKey = fieldKey;
        field.Label = request.Label.Trim();
        field.FieldType = request.FieldType;
        field.FulfillmentMode = request.FulfillmentMode;
        field.Actor = request.Actor;
        field.IsRequired = request.IsRequired;
        field.IsSensitive = request.IsSensitive;
        field.IsVisibleToCustomerAfterCompletion =
            request.IsVisibleToCustomerAfterCompletion;
        field.DisplayOrder = request.DisplayOrder;
        field.OptionsJson = NormalizeOptional(request.OptionsJson);

        await _repository.UpdateRequiredFieldAsync(field);
        return ServiceResult<ServiceRequiredFieldResponse>.Ok(
            ServiceCatalogMapper.MapField(field),
            AppMessages.RequiredFieldUpdated);
    }

    private static string NormalizeSlug(string slug)
    {
        return slug.Trim().ToLowerInvariant();
    }

    private async Task<Provider> ResolveProviderAsync(string providerName)
    {
        var name = ProviderSlugNormalizer.NormalizeName(providerName);
        var slug = ProviderSlugNormalizer.NormalizeSlug(name);
        var provider = await _repository.GetProviderBySlugAsync(slug);
        if (provider != null)
        {
            return provider;
        }

        provider = new Provider
        {
            Name = name,
            Slug = slug,
            IsActive = true
        };
        await _repository.AddProviderAsync(provider);
        return provider;
    }

    private static ServiceResult<ServiceDetailResponse>? ValidateSupportedFulfillmentModes(
        CreateDigitalServiceRequest request)
    {
        if (request.SupportsUseExistingAccount || request.SupportsCreateNewAccount)
        {
            return null;
        }

        return new ServiceResult<ServiceDetailResponse>
        {
            Success = false,
            Message = AppMessages.ValidationFailed,
            ErrorCode = AppConstants.ErrorCodes.ValidationFailed,
            StatusCode = 400,
            ValidationErrors = new Dictionary<string, string[]>
            {
                [nameof(request.SupportsUseExistingAccount)] =
                    ["Select at least one supported fulfillment mode."]
            }
        };
    }

    private static string NormalizeFieldKey(string fieldKey)
    {
        return fieldKey.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
