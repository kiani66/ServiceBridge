using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IServiceCatalogQueryService
{
    Task<ServiceResult<IReadOnlyCollection<ServiceSummaryResponse>>> GetActiveServicesAsync();
    Task<ServiceResult<ServiceDetailResponse>> GetActiveServiceByIdAsync(Guid id);
    Task<ServiceResult<ServiceDetailResponse>> GetActiveServiceBySlugAsync(string slug);
    Task<ServiceResult<IReadOnlyCollection<ServiceCategoryResponse>>> GetAllCategoriesAsync();
    Task<ServiceResult<IReadOnlyCollection<AdminServiceSummaryResponse>>> GetAllServicesAsync();
    Task<ServiceResult<AdminServiceResponse>> GetServiceByIdAsync(Guid id);
}
