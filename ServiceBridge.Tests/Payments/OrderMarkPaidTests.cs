using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using Xunit;

namespace ServiceBridge.Tests.Payments;

public class OrderMarkPaidTests
{
    [Fact]
    public void MarkPaid_WhenWaitingForPayment_TransitionsAndAppendsSystemLog()
    {
        var paymentId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.WaitingForPayment
        };

        order.MarkPaid(paymentId, "Payment confirmed.");

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.NotNull(order.PaidAt);

        var log = Assert.Single(order.StatusHistory);
        Assert.Equal(OrderStatus.WaitingForPayment, log.PreviousStatus);
        Assert.Equal(OrderStatus.Paid, log.NewStatus);
        Assert.Equal(OrderLogActorType.System, log.ActorType);
        Assert.Equal(paymentId, log.ActorId);
        Assert.Null(log.ChangedByUserId);
    }

    [Fact]
    public void MarkPaid_WhenOrderIsNotWaitingForPayment_Throws()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Processing
        };

        Assert.Throws<ServiceBridge.Domain.Exceptions.InvalidOrderTransitionException>(
            () => order.MarkPaid(Guid.NewGuid(), "Payment confirmed."));

        Assert.Equal(OrderStatus.Processing, order.Status);
        Assert.Null(order.PaidAt);
        Assert.Empty(order.StatusHistory);
    }
}
