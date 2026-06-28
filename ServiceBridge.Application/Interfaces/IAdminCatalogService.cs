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
    Task<ServiceResult<ServicePlanResponse>> CreatePlanAsync(
        Guid serviceId,
        CreateServicePlanRequest request);
    Task<ServiceResult<ServicePlanResponse>> UpdatePlanAsync(
        Guid id,
        UpdateServicePlanRequest request);
    Task<ServiceResult<ServiceRequiredFieldResponse>> CreateRequiredFieldAsync(
        Guid serviceId,
        CreateServiceRequiredFieldRequest request);
    Task<ServiceResult<ServiceRequiredFieldResponse>> UpdateRequiredFieldAsync(
        Guid id,
        UpdateServiceRequiredFieldRequest request);
}
