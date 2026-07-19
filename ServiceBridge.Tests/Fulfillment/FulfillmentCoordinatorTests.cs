using ServiceBridge.Application.Fulfillment;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.Orders;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Fulfillment;

public sealed class FulfillmentCoordinatorTests
{
    [Fact]
    public async Task EligiblePolicyManualOrder_CreatesOnePendingExecution()
    {
        var order = CreatePolicyOrder(
            FulfillmentMode.ManualAgent,
            FulfillmentMode.ManualAgent,
            OrderStatus.Paid);
        var repository = new FakeFulfillmentExecutionRepository();
        var coordinator = new FulfillmentCoordinator(repository);

        var result = await coordinator.EnsureForPaidOrderAsync(order);

        Assert.True(result.Success);
        Assert.Equal(FulfillmentInitializationOutcome.Created, result.Data);
        var execution = Assert.Single(repository.Executions);
        Assert.Equal(order.Id, execution.OrderId);
        Assert.Equal(FulfillmentExecutionStatus.Pending, execution.Status);
        Assert.Empty(execution.Attempts);
    }

    [Fact]
    public async Task RepeatedEnsure_ReturnsAlreadyExistsWithoutDuplicate()
    {
        var order = CreateHistoricalOrder(
            FulfillmentMode.ManualAgent,
            OrderStatus.Paid);
        var repository = new FakeFulfillmentExecutionRepository();
        var coordinator = new FulfillmentCoordinator(repository);

        await coordinator.EnsureForPaidOrderAsync(order);
        var second = await coordinator.EnsureForPaidOrderAsync(order);

        Assert.Equal(FulfillmentInitializationOutcome.AlreadyExists, second.Data);
        Assert.Single(repository.Executions);
    }

    [Fact]
    public async Task HistoricalManualOrderWithoutPolicy_IsEligible()
    {
        var order = CreateHistoricalOrder(
            FulfillmentMode.ManualAgent,
            OrderStatus.Paid);
        var repository = new FakeFulfillmentExecutionRepository();

        var result = await new FulfillmentCoordinator(repository)
            .EnsureForPaidOrderAsync(order);

        Assert.Equal(FulfillmentInitializationOutcome.Created, result.Data);
        Assert.Single(repository.Executions);
    }

    [Theory]
    [InlineData(FulfillmentMode.ManualAgent, FulfillmentMode.InventoryCode,
        FulfillmentInitializationOutcome.LegacyModeMismatch)]
    [InlineData(FulfillmentMode.InventoryCode, FulfillmentMode.ManualAgent,
        FulfillmentInitializationOutcome.LegacyModeMismatch)]
    [InlineData(FulfillmentMode.InventoryCode, FulfillmentMode.InventoryCode,
        FulfillmentInitializationOutcome.NotApplicable)]
    [InlineData(FulfillmentMode.ExternalAutomation, FulfillmentMode.ExternalAutomation,
        FulfillmentInitializationOutcome.NotApplicable)]
    [InlineData(FulfillmentMode.InstructionOnly, FulfillmentMode.InstructionOnly,
        FulfillmentInitializationOutcome.NotApplicable)]
    public async Task PolicyAndLegacyCompatibility_IsExplicit(
        FulfillmentMode policyMode,
        FulfillmentMode legacyMode,
        FulfillmentInitializationOutcome expected)
    {
        var order = CreatePolicyOrder(policyMode, legacyMode, OrderStatus.Paid);
        var repository = new FakeFulfillmentExecutionRepository();

        var result = await new FulfillmentCoordinator(repository)
            .EnsureForPaidOrderAsync(order);

        Assert.True(result.Success);
        Assert.Equal(expected, result.Data);
        Assert.Empty(repository.Executions);
    }

    [Fact]
    public async Task AssignmentEnsure_CreatesPendingExecutionWithoutAttempt()
    {
        var order = CreateHistoricalOrder(
            FulfillmentMode.ManualAgent,
            OrderStatus.Paid);
        var repository = new FakeFulfillmentExecutionRepository();

        var result = await new FulfillmentCoordinator(repository)
            .EnsureForManualAssignmentAsync(order);

        Assert.True(result.Success);
        var execution = Assert.Single(repository.Executions);
        Assert.Equal(FulfillmentExecutionStatus.Pending, execution.Status);
        Assert.Empty(execution.Attempts);
    }

    [Fact]
    public async Task Start_CreatesAttemptOne_AndDuplicateStartConflicts()
    {
        var order = CreatePolicyOrder(
            FulfillmentMode.ManualAgent,
            FulfillmentMode.ManualAgent,
            OrderStatus.Processing);
        var repository = new FakeFulfillmentExecutionRepository();
        var coordinator = new FulfillmentCoordinator(repository);

        var first = await coordinator.StartManualAttemptAsync(order);
        var second = await coordinator.StartManualAttemptAsync(order);

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Equal(AppConstants.ErrorCodes.FulfillmentExecutionConflict,
            second.ErrorCode);
        var execution = Assert.Single(repository.Executions);
        var attempt = Assert.Single(execution.Attempts);
        Assert.Equal(1, attempt.AttemptNumber);
        Assert.Equal(FulfillmentMode.ManualAgent, attempt.FulfillmentMode);
        Assert.Equal(FulfillmentAttemptStatus.Processing, attempt.Status);
    }

    [Fact]
    public async Task Complete_ActiveManualAttempt_CompletesExecution()
    {
        var (order, repository, coordinator) = await CreateStartedAsync();
        order.Status = OrderStatus.Completed;

        var result = await coordinator.CompleteManualAttemptAsync(order);

        Assert.True(result.Success);
        var execution = Assert.Single(repository.Executions);
        Assert.Equal(FulfillmentExecutionStatus.Completed, execution.Status);
        Assert.Equal(FulfillmentAttemptStatus.Succeeded,
            Assert.Single(execution.Attempts).Status);
    }

    [Fact]
    public async Task Fail_UsesTerminalStableCode_WithoutRawReason()
    {
        var (order, repository, coordinator) = await CreateStartedAsync();
        order.Status = OrderStatus.Failed;

        var result = await coordinator.FailManualAttemptAsync(order);

        Assert.True(result.Success);
        var execution = Assert.Single(repository.Executions);
        var attempt = Assert.Single(execution.Attempts);
        Assert.Equal(FulfillmentExecutionStatus.Failed, execution.Status);
        Assert.Equal(FulfillmentAttemptStatus.Failed, attempt.Status);
        Assert.Equal(FulfillmentFailureDisposition.Terminal,
            attempt.FailureDisposition);
        Assert.Equal("manual_agent_failed", attempt.FailureCode?.Value);
        Assert.Null(attempt.FailureMessage);
    }

    [Fact]
    public async Task NewSnapshottedCompletionWithoutExecution_ReturnsMissingConflict()
    {
        var order = CreatePolicyOrder(
            FulfillmentMode.ManualAgent,
            FulfillmentMode.ManualAgent,
            OrderStatus.Completed);

        var result = await new FulfillmentCoordinator(
                new FakeFulfillmentExecutionRepository())
            .CompleteManualAttemptAsync(order);

        Assert.False(result.Success);
        Assert.Equal(AppConstants.ErrorCodes.FulfillmentExecutionMissing,
            result.ErrorCode);
    }

    [Fact]
    public async Task HistoricalCompletionWithoutExecution_IsLegacyNoOp()
    {
        var order = CreateHistoricalOrder(
            FulfillmentMode.ManualAgent,
            OrderStatus.Completed);
        var repository = new FakeFulfillmentExecutionRepository();

        var result = await new FulfillmentCoordinator(repository)
            .CompleteManualAttemptAsync(order);

        Assert.True(result.Success);
        Assert.Empty(repository.Executions);
    }

    private static async Task<(
        Order Order,
        FakeFulfillmentExecutionRepository Repository,
        FulfillmentCoordinator Coordinator)> CreateStartedAsync()
    {
        var order = CreatePolicyOrder(
            FulfillmentMode.ManualAgent,
            FulfillmentMode.ManualAgent,
            OrderStatus.Processing);
        var repository = new FakeFulfillmentExecutionRepository();
        var coordinator = new FulfillmentCoordinator(repository);
        await coordinator.StartManualAttemptAsync(order);
        return (order, repository, coordinator);
    }

    internal static Order CreatePolicyOrder(
        FulfillmentMode policyMode,
        FulfillmentMode legacyMode,
        OrderStatus status)
    {
        var snapshot = OrderFulfillmentPolicySnapshot.Create(
            Guid.NewGuid(),
            1,
            policyMode == FulfillmentMode.InstructionOnly
                ? ProcurementStrategy.NotRequired
                : ProcurementStrategy.OnDemandManual,
            policyMode,
            policyMode == FulfillmentMode.InstructionOnly
                ? DeliveryArtifactType.Instructions
                : DeliveryArtifactType.AccountAccess);
        var order = Order.Create(snapshot, legacyMode);
        order.Id = Guid.NewGuid();
        order.UserId = Guid.NewGuid();
        order.Status = status;
        return order;
    }

    internal static Order CreateHistoricalOrder(
        FulfillmentMode legacyMode,
        OrderStatus status)
    {
        var order = OrderTestFactory.Create(legacyMode);
        order.Id = Guid.NewGuid();
        order.UserId = Guid.NewGuid();
        order.Status = status;
        return order;
    }
}
