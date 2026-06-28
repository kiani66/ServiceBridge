using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Order?> GetOrderAsync(Guid orderId);
    Task<bool> PendingPaymentExistsAsync(Guid orderId);
    Task<bool> PaymentExistsAsync(Guid paymentId);
    Task AddPaymentAsync(Payment payment);
    Task<Payment?> GetPaymentWithOrderAsync(Guid paymentId);
    Task<bool> SaveChangesAsync();
}
