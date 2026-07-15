using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ServiceBridgeDbContext _context;

    public PaymentRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public Task<Order?> GetOrderAsync(Guid orderId)
    {
        return _context.Orders
            .FirstOrDefaultAsync(x => x.Id == orderId);
    }

    public Task<Payment?> GetPendingPaymentForOrderAsync(Guid orderId)
    {
        return _context.Payments
            .FirstOrDefaultAsync(x =>
                x.OrderId == orderId &&
                x.Status == PaymentStatus.Pending);
    }

    public Task<bool> PaymentExistsAsync(Guid paymentId)
    {
        return _context.Payments.AnyAsync(x => x.Id == paymentId);
    }

    public async Task AddPaymentAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
    }

    public Task<Payment?> GetPaymentWithOrderAsync(Guid paymentId)
    {
        return _context.Payments
            .Include(x => x.Order)
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.Id == paymentId);
    }

    public async Task<Payment?> GetPaymentWithOrderForConfirmationAsync(
        Guid paymentId)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Payment confirmation requires an active database transaction.");
        }

        _context.ChangeTracker.Clear();

        var orderId = await _context.Payments
            .AsNoTracking()
            .Where(payment => payment.Id == paymentId)
            .Select(payment => (Guid?)payment.OrderId)
            .SingleOrDefaultAsync();
        if (orderId is null)
        {
            return null;
        }

        await _context.Orders
            .FromSqlInterpolated($"""
                SELECT *
                FROM [Orders] WITH (UPDLOCK, ROWLOCK)
                WHERE [Id] = {orderId.Value}
                """)
            .SingleAsync();

        return await _context.Payments
            .Include(payment => payment.Order)
            .Include(payment => payment.Events)
            .SingleAsync(payment => payment.Id == paymentId);
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
