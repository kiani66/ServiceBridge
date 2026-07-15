using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakePaymentRepository : IPaymentRepository
{
    public Order? Order { get; set; }
    public Payment? Payment { get; set; }
    public Payment? ExistingPendingPayment { get; set; }
    public bool PaymentIdExists { get; set; }
    public bool SaveSucceeds { get; set; } = true;
    public IList<string>? Operations { get; set; }
    public Payment? AddedPayment { get; private set; }
    public int SaveCount { get; private set; }

    public Task<Order?> GetOrderAsync(Guid orderId)
    {
        return Task.FromResult(Order?.Id == orderId ? Order : null);
    }

    public Task<Payment?> GetPendingPaymentForOrderAsync(Guid orderId)
    {
        return Task.FromResult(
            ExistingPendingPayment is
            {
                OrderId: var pendingOrderId,
                Status: PaymentStatus.Pending
            } && pendingOrderId == orderId
                ? ExistingPendingPayment
                : null);
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
        Operations?.Add("payment.load");
        return Task.FromResult(Payment?.Id == paymentId ? Payment : null);
    }

    public Task<Payment?> GetPaymentWithOrderForConfirmationAsync(Guid paymentId)
    {
        Operations?.Add("payment.confirmation-lock-load");
        return Task.FromResult(Payment?.Id == paymentId ? Payment : null);
    }

    public Task<bool> SaveChangesAsync()
    {
        SaveCount++;
        Operations?.Add("payment.save");
        return Task.FromResult(SaveSucceeds);
    }
}
