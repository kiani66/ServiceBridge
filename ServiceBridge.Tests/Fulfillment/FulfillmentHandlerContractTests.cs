using ServiceBridge.Application.Fulfillment;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.ValueObjects;
using Xunit;

namespace ServiceBridge.Tests.Fulfillment;

public sealed class FulfillmentHandlerContractTests
{
    [Fact]
    public void SucceededResult_CarriesNoFailureValues()
    {
        var result = FulfillmentHandlerResult.Succeeded();

        Assert.Equal(FulfillmentHandlerOutcome.Succeeded, result.Outcome);
        Assert.Null(result.FailureCode);
        Assert.Null(result.FailureMessage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FailureResults_RequireFailureCode(bool retryable)
    {
        Assert.Throws<ArgumentNullException>(() => retryable
            ? FulfillmentHandlerResult.RetryableFailure(null!)
            : FulfillmentHandlerResult.TerminalFailure(null!));
    }

    [Fact]
    public void FailureResult_IsExplicitAndSanitizesMessage()
    {
        var code = FulfillmentFailureCode.Create("Temporary.Unavailable");

        var retryable = FulfillmentHandlerResult.RetryableFailure(
            code,
            "  retry\r\n later ");
        var terminal = FulfillmentHandlerResult.TerminalFailure(code);

        Assert.Equal(FulfillmentHandlerOutcome.RetryableFailure, retryable.Outcome);
        Assert.Equal("retry later", retryable.FailureMessage);
        Assert.Equal(FulfillmentHandlerOutcome.TerminalFailure, terminal.Outcome);
        Assert.Same(code, terminal.FailureCode);
    }

    [Fact]
    public void FailureResult_RejectsMessageBeyondBound()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FulfillmentHandlerResult.RetryableFailure(
                FulfillmentFailureCode.Create("Temporary.Unavailable"),
                new string('x', FulfillmentHandlerResult.FailureMessageMaxLength + 1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Context_RejectsEmptyIdentifier(int emptyIdentifierIndex)
    {
        var identifiers = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        identifiers[emptyIdentifierIndex] = Guid.Empty;

        Assert.Throws<ArgumentException>(() => FulfillmentHandlerContext.Create(
            identifiers[0],
            identifiers[1],
            identifiers[2],
            ProcurementStrategy.OnDemandManual,
            FulfillmentMode.ManualAgent,
            DeliveryArtifactType.AccountAccess));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Context_RejectsUndefinedSnapshotEnum(int enumIndex)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FulfillmentHandlerContext.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                enumIndex == 0
                    ? (ProcurementStrategy)999
                    : ProcurementStrategy.OnDemandManual,
                enumIndex == 1
                    ? (FulfillmentMode)999
                    : FulfillmentMode.ManualAgent,
                enumIndex == 2
                    ? (DeliveryArtifactType)999
                    : DeliveryArtifactType.AccountAccess));
    }

    [Fact]
    public void Context_IsImmutableAndContainsOnlyApprovedScalarDecisions()
    {
        var properties = typeof(FulfillmentHandlerContext).GetProperties();

        Assert.Equal(
            new[]
            {
                nameof(FulfillmentHandlerContext.OrderId),
                nameof(FulfillmentHandlerContext.FulfillmentExecutionId),
                nameof(FulfillmentHandlerContext.FulfillmentAttemptId),
                nameof(FulfillmentHandlerContext.ProcurementStrategy),
                nameof(FulfillmentHandlerContext.PolicyFulfillmentMode),
                nameof(FulfillmentHandlerContext.DeliveryArtifactType)
            },
            properties.Select(property => property.Name));
        Assert.All(properties, property => Assert.Null(property.SetMethod));
        Assert.DoesNotContain(properties, property =>
            typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType));
    }

    [Fact]
    public void HandlerMode_IsAnExplicitFulfillmentModeProperty()
    {
        var mode = typeof(IFulfillmentHandler)
            .GetProperty(nameof(IFulfillmentHandler.Mode));
        var execute = typeof(IFulfillmentHandler)
            .GetMethod(nameof(IFulfillmentHandler.ExecuteAsync));

        Assert.NotNull(mode);
        Assert.Equal(typeof(FulfillmentMode), mode!.PropertyType);
        Assert.NotNull(execute);
        Assert.Equal(typeof(Task<FulfillmentHandlerResult>), execute!.ReturnType);
    }
}
