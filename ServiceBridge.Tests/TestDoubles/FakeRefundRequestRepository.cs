using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakeRefundRequestRepository : IRefundRequestRepository
{
    public Order? Order { get; set; }
    public RefundRequest? RefundRequest { get; set; }
    public bool ActiveRefundExists { get; set; }
    public bool SaveSucceeds { get; set; } = true;
    public int SaveCount { get; private set; }
    public int AddCount { get; private set; }

    public Task<Order?> GetOrderWithCompletedPaymentAsync(Guid userId, Guid orderId)
    {
        var ownedOrder = Order?.UserId == userId && Order.Id == orderId
            ? Order
            : null;

        return Task.FromResult(ownedOrder);
    }

    public Task<bool> HasActiveRefundRequestAsync(Guid orderId, Guid paymentId)
    {
        return Task.FromResult(ActiveRefundExists);
    }

    public Task AddAsync(RefundRequest refundRequest)
    {
        RefundRequest = refundRequest;
        AddCount++;
        return Task.CompletedTask;
    }

    public Task<RefundRequest?> GetForReviewAsync(Guid refundRequestId)
    {
        var request = RefundRequest?.Id == refundRequestId ? RefundRequest : null;
        return Task.FromResult(request);
    }

    public Task<PagedResultDto<RefundRequestDto>> GetUserRefundsAsync(
        Guid userId,
        int page,
        int pageSize)
    {
        return Task.FromResult(new PagedResultDto<RefundRequestDto>
        {
            Page = page,
            PageSize = pageSize
        });
    }

    public Task<PagedResultDto<AdminRefundRequestDto>> GetAdminRefundsAsync(
        int page,
        int pageSize,
        RefundStatus? status)
    {
        return Task.FromResult(new PagedResultDto<AdminRefundRequestDto>
        {
            Page = page,
            PageSize = pageSize
        });
    }

    public Task<bool> SaveChangesAsync()
    {
        SaveCount++;
        return Task.FromResult(SaveSucceeds);
    }
}
