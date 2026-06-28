using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class ServiceCatalogQueryService : IServiceCatalogQueryService
{
    private readonly IServiceCatalogRepository _repository;

    public ServiceCatalogQueryService(IServiceCatalogRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<IReadOnlyCollection<ServiceSummaryResponse>>> GetActiveServicesAsync()
    {
        var services = await _repository.GetActiveServicesAsync();
        var response = services.Select(ServiceCatalogMapper.MapSummary).ToArray();

        return ServiceResult<IReadOnlyCollection<ServiceSummaryResponse>>.Ok(response);
    }

    public async Task<ServiceResult<ServiceDetailResponse>> GetActiveServiceByIdAsync(Guid id)
    {
        var service = await _repository.GetActiveServiceByIdAsync(id);
        if (service == null)
        {
            return ServiceResult<ServiceDetailResponse>.Fail(
                AppMessages.ServiceNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        return ServiceResult<ServiceDetailResponse>.Ok(
            ServiceCatalogMapper.MapDetail(service));
    }

    public async Task<ServiceResult<IReadOnlyCollection<ServiceCategoryResponse>>> GetAllCategoriesAsync()
    {
        var categories = await _repository.GetAllCategoriesAsync();
        var response = categories.Select(ServiceCatalogMapper.MapCategory).ToArray();

        return ServiceResult<IReadOnlyCollection<ServiceCategoryResponse>>.Ok(response);
    }

    public async Task<ServiceResult<IReadOnlyCollection<AdminServiceSummaryResponse>>> GetAllServicesAsync()
    {
        var services = await _repository.GetAllServicesAsync();
        var response = services.Select(ServiceCatalogMapper.MapAdminSummary).ToArray();

        return ServiceResult<IReadOnlyCollection<AdminServiceSummaryResponse>>.Ok(response);
    }

    public async Task<ServiceResult<AdminServiceResponse>> GetServiceByIdAsync(Guid id)
    {
        var service = await _repository.GetServiceByIdAsync(id);
        if (service == null)
        {
            return ServiceResult<AdminServiceResponse>.Fail(
                AppMessages.ServiceNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        return ServiceResult<AdminServiceResponse>.Ok(
            ServiceCatalogMapper.MapAdminDetail(service));
    }
}
