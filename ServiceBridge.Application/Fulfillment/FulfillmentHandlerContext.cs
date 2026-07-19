using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Fulfillment;

public sealed class FulfillmentHandlerContext
{
    private FulfillmentHandlerContext(
        Guid orderId,
        Guid fulfillmentExecutionId,
        Guid fulfillmentAttemptId,
        ProcurementStrategy procurementStrategy,
        FulfillmentMode policyFulfillmentMode,
        DeliveryArtifactType deliveryArtifactType)
    {
        OrderId = orderId;
        FulfillmentExecutionId = fulfillmentExecutionId;
        FulfillmentAttemptId = fulfillmentAttemptId;
        ProcurementStrategy = procurementStrategy;
        PolicyFulfillmentMode = policyFulfillmentMode;
        DeliveryArtifactType = deliveryArtifactType;
    }

    public Guid OrderId { get; }
    public Guid FulfillmentExecutionId { get; }
    public Guid FulfillmentAttemptId { get; }
    public ProcurementStrategy ProcurementStrategy { get; }
    public FulfillmentMode PolicyFulfillmentMode { get; }
    public DeliveryArtifactType DeliveryArtifactType { get; }

    public static FulfillmentHandlerContext Create(
        Guid orderId,
        Guid fulfillmentExecutionId,
        Guid fulfillmentAttemptId,
        ProcurementStrategy procurementStrategy,
        FulfillmentMode policyFulfillmentMode,
        DeliveryArtifactType deliveryArtifactType)
    {
        EnsureIdentifier(orderId, nameof(orderId));
        EnsureIdentifier(fulfillmentExecutionId, nameof(fulfillmentExecutionId));
        EnsureIdentifier(fulfillmentAttemptId, nameof(fulfillmentAttemptId));
        EnsureDefined(procurementStrategy, nameof(procurementStrategy));
        EnsureDefined(policyFulfillmentMode, nameof(policyFulfillmentMode));
        EnsureDefined(deliveryArtifactType, nameof(deliveryArtifactType));

        return new FulfillmentHandlerContext(
            orderId,
            fulfillmentExecutionId,
            fulfillmentAttemptId,
            procurementStrategy,
            policyFulfillmentMode,
            deliveryArtifactType);
    }

    private static void EnsureIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }
    }

    private static void EnsureDefined<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Unsupported {typeof(TEnum).Name} value.");
        }
    }
}
