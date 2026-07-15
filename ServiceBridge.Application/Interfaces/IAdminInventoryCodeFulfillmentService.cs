using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminInventoryCodeFulfillmentService
{
    Task<ServiceResult<RetryInventoryCodeFulfillmentResultDto>> RetryAsync(
        Guid adminId,
        Guid orderId,
        CancellationToken cancellationToken = default);
}
