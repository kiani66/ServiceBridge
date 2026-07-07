using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface IRefundRequestRepository
{
    Task<Order?> GetOrderWithCompletedPaymentAsync(Guid userId, Guid orderId);
    Task<bool> HasActiveRefundRequestAsync(Guid orderId, Guid paymentId);
    Task AddAsync(RefundRequest refundRequest);
    Task<RefundRequest?> GetForReviewAsync(Guid refundRequestId);
    Task<PagedResultDto<RefundRequestDto>> GetUserRefundsAsync(
        Guid userId,
        int page,
        int pageSize);
    Task<PagedResultDto<AdminRefundRequestDto>> GetAdminRefundsAsync(
        int page,
        int pageSize,
        RefundStatus? status);
    Task<bool> SaveChangesAsync();
}
