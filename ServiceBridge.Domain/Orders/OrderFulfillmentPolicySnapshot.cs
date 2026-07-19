using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Orders;

public sealed class OrderFulfillmentPolicySnapshot
{
    private OrderFulfillmentPolicySnapshot(
        Guid policyId,
        int version,
        ProcurementStrategy procurementStrategy,
        FulfillmentMode fulfillmentMode,
        DeliveryArtifactType deliveryArtifactType)
    {
        PolicyId = policyId;
        Version = version;
        ProcurementStrategy = procurementStrategy;
        FulfillmentMode = fulfillmentMode;
        DeliveryArtifactType = deliveryArtifactType;
    }

    public Guid PolicyId { get; }
    public int Version { get; }
    public ProcurementStrategy ProcurementStrategy { get; }
    public FulfillmentMode FulfillmentMode { get; }
    public DeliveryArtifactType DeliveryArtifactType { get; }

    public static OrderFulfillmentPolicySnapshot Create(
        Guid policyId,
        int version,
        ProcurementStrategy procurementStrategy,
        FulfillmentMode fulfillmentMode,
        DeliveryArtifactType deliveryArtifactType)
    {
        if (policyId == Guid.Empty)
        {
            throw new ArgumentException(
                "Fulfillment policy ID is required.",
                nameof(policyId));
        }

        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(version),
                "Fulfillment policy version must be positive.");
        }

        EnsureDefined(procurementStrategy, nameof(procurementStrategy));
        EnsureDefined(fulfillmentMode, nameof(fulfillmentMode));
        EnsureDefined(deliveryArtifactType, nameof(deliveryArtifactType));

        return new OrderFulfillmentPolicySnapshot(
            policyId,
            version,
            procurementStrategy,
            fulfillmentMode,
            deliveryArtifactType);
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
