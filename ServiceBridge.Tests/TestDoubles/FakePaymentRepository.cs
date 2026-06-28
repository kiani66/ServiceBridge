using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakePaymentRepository : IPaymentRepository
{
    public Order? Order { get; set; }
    public Payment? Payment { get; set; }
    public bool HasPendingPayment { get; set; }
    public bool PaymentIdExists { get; set; }
    public bool SaveSucceeds { get; set; } = true;
    public Payment? AddedPayment { get; private set; }
    public int SaveCount { get; private set; }

    public Task<Order?> GetOrderAsync(Guid orderId)
    {
        return Task.FromResult(Order?.Id == orderId ? Order : null);
    }

    public Task<bool> PendingPaymentExistsAsync(Guid orderId)
    {
        return Task.FromResult(HasPendingPayment);
    }

    public Task<bool> PaymentExistsAsync(Guid paymentId)
    {
        return Task.FromResult(PaymentIdExists);
    }

    public Task AddPaymentAsync(Payment payment)
    {
        AddedPayment = payment;
        return Task.CompletedTask;
    }

    public Task<Payment?> GetPaymentWithOrderAsync(Guid paymentId)
    {
        return Task.FromResult(Payment?.Id == paymentId ? Payment : null);
    }

    public Task<bool> SaveChangesAsync()
    {
        SaveCount++;
        return Task.FromResult(SaveSucceeds);
    }
}
