using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Order?> GetOrderAsync(Guid orderId);
    Task<Payment?> GetPendingPaymentForOrderAsync(Guid orderId);
    Task<bool> PaymentExistsAsync(Guid paymentId);
    Task AddPaymentAsync(Payment payment);
    Task<Payment?> GetPaymentWithOrderAsync(Guid paymentId);
    Task<Payment?> GetPaymentWithOrderForConfirmationAsync(Guid paymentId);
    Task<bool> SaveChangesAsync();
}
