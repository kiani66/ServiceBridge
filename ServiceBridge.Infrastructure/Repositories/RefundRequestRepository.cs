using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class RefundRequestRepository : IRefundRequestRepository
{
    private static readonly RefundStatus[] ActiveStatuses =
    [
        RefundStatus.Requested,
        RefundStatus.Approved
    ];

    private readonly ServiceBridgeDbContext _context;

    public RefundRequestRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public Task<Order?> GetOrderWithCompletedPaymentAsync(Guid userId, Guid orderId)
    {
        return _context.Orders
            .Include(x => x.PaymentRecords)
            .FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == userId);
    }

    public Task<bool> HasActiveRefundRequestAsync(Guid orderId, Guid paymentId)
    {
        return _context.RefundRequests.AnyAsync(x =>
            x.OrderId == orderId &&
            x.PaymentId == paymentId &&
            ActiveStatuses.Contains(x.Status));
    }

    public async Task AddAsync(RefundRequest refundRequest)
    {
        await _context.RefundRequests.AddAsync(refundRequest);
    }

    public Task<RefundRequest?> GetForReviewAsync(Guid refundRequestId)
    {
        return _context.RefundRequests
            .Include(x => x.Order)
            .Include(x => x.StatusHistory)
            .FirstOrDefaultAsync(x => x.Id == refundRequestId);
    }

    public async Task<PagedResultDto<RefundRequestDto>> GetUserRefundsAsync(
        Guid userId,
        int page,
        int pageSize)
    {
        var query = _context.RefundRequests
            .AsNoTracking()
            .Where(x => x.RequestedByUserId == userId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RefundRequestDto
            {
                RefundRequestId = x.Id,
                OrderId = x.OrderId,
                OrderNumber = x.Order.OrderNumber,
                PaymentId = x.PaymentId,
                Amount = x.Amount,
                Reason = x.Reason,
                Status = x.Status.ToString(),
                CreatedAtUtc = x.CreatedAt,
                ReviewedAtUtc = x.ReviewedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                AdminNote = x.AdminNote
            })
            .ToListAsync();

        return new PagedResultDto<RefundRequestDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResultDto<AdminRefundRequestDto>> GetAdminRefundsAsync(
        int page,
        int pageSize,
        RefundStatus? status)
    {
        var query = _context.RefundRequests.AsNoTracking();
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminRefundRequestDto
            {
                RefundRequestId = x.Id,
                OrderId = x.OrderId,
                OrderNumber = x.Order.OrderNumber,
                PaymentId = x.PaymentId,
                Amount = x.Amount,
                Reason = x.Reason,
                Status = x.Status.ToString(),
                CreatedAtUtc = x.CreatedAt,
                ReviewedAtUtc = x.ReviewedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                AdminNote = x.AdminNote,
                CustomerName = x.RequestedByUser.FullName,
                CustomerEmail = x.RequestedByUser.Email,
                OrderStatus = x.Order.Status.ToString(),
                PaymentStatus = x.Payment.Status.ToString(),
                ReviewedByAdminName = x.ReviewedByAdmin == null
                    ? null
                    : x.ReviewedByAdmin.FullName
            })
            .ToListAsync();

        return new PagedResultDto<AdminRefundRequestDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<bool> SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return false;
        }
    }
}
