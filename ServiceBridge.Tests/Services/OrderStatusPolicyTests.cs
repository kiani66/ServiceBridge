using ServiceBridge.Application.Services;
using ServiceBridge.Domain.Enums;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class OrderStatusPolicyTests
{
    private readonly OrderStatusPolicy _policy = new();

    public static TheoryData<OrderStatus, OrderStatus> AllowedTransitions =>
        new()
        {
            { OrderStatus.Created, OrderStatus.WaitingForPayment },
            { OrderStatus.Created, OrderStatus.Cancelled },
            { OrderStatus.WaitingForPayment, OrderStatus.Paid },
            { OrderStatus.WaitingForPayment, OrderStatus.Cancelled },
            { OrderStatus.Paid, OrderStatus.Assigned },
            { OrderStatus.Paid, OrderStatus.Cancelled },
            { OrderStatus.Assigned, OrderStatus.Processing },
            { OrderStatus.Assigned, OrderStatus.Cancelled },
            { OrderStatus.Processing, OrderStatus.Completed },
            { OrderStatus.Processing, OrderStatus.Failed }
        };

    [Theory]
    [MemberData(nameof(AllowedTransitions))]
    public void CanTransition_ForAllowedLifecycleStep_ReturnsTrue(
        OrderStatus currentStatus,
        OrderStatus newStatus)
    {
        Assert.True(_policy.CanTransition(currentStatus, newStatus));
    }

    [Theory]
    [InlineData(OrderStatus.Created, OrderStatus.Completed)]
    [InlineData(OrderStatus.WaitingForPayment, OrderStatus.Processing)]
    [InlineData(OrderStatus.Paid, OrderStatus.Completed)]
    [InlineData(OrderStatus.Assigned, OrderStatus.Completed)]
    [InlineData(OrderStatus.Completed, OrderStatus.Processing)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.WaitingForPayment)]
    [InlineData(OrderStatus.Failed, OrderStatus.Paid)]
    public void CanTransition_ForInvalidLifecycleStep_ReturnsFalse(
        OrderStatus currentStatus,
        OrderStatus newStatus)
    {
        Assert.False(_policy.CanTransition(currentStatus, newStatus));
    }

    [Theory]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Failed)]
    public void IsTerminal_ForFinalStatus_ReturnsTrue(OrderStatus status)
    {
        Assert.True(_policy.IsTerminal(status));
        Assert.Empty(_policy.GetAllowedTransitions(status));
    }

    [Fact]
    public void GetAllowedTransitions_ReturnsOnlyConfiguredTargets()
    {
        var transitions = _policy.GetAllowedTransitions(OrderStatus.Processing);

        Assert.Equal(
            new[]
            {
                OrderStatus.Completed,
                OrderStatus.Failed
            },
            transitions);
    }

    [Fact]
    public void EveryDefinedStatus_IsConfiguredAndRejectsSelfTransition()
    {
        foreach (var status in Enum.GetValues<OrderStatus>())
        {
            Assert.False(_policy.CanTransition(status, status));

            if (!_policy.IsTerminal(status))
            {
                Assert.NotEmpty(_policy.GetAllowedTransitions(status));
            }
        }
    }
}
