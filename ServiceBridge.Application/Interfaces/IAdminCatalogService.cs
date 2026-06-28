using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminCatalogService
{
    Task<ServiceResult<ServiceCategoryResponse>> CreateCategoryAsync(
        CreateServiceCategoryRequest request);
    Task<ServiceResult<ServiceCategoryResponse>> UpdateCategoryAsync(
        Guid id,
        UpdateServiceCategoryRequest request);
    Task<ServiceResult<ServiceDetailResponse>> CreateServiceAsync(
        CreateDigitalServiceRequest request);
    Task<ServiceResult<ServiceDetailResponse>> UpdateServiceAsync(
        Guid id,
        UpdateDigitalServiceRequest request);
    Task<ServiceResult<ServiceVariantResponse>> CreateVariantAsync(
        Guid serviceId,
        CreateServiceVariantRequest request);
    Task<ServiceResult<ServiceVariantResponse>> UpdateVariantAsync(
        Guid id,
        UpdateServiceVariantRequest request);
    Task<ServiceResult<ServiceRequiredFieldResponse>> CreateRequiredFieldAsync(
        Guid serviceId,
        CreateServiceRequiredFieldRequest request);
    Task<ServiceResult<ServiceRequiredFieldResponse>> UpdateRequiredFieldAsync(
        Guid id,
        UpdateServiceRequiredFieldRequest request);
}
