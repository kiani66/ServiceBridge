using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Entities;

public sealed class FulfillmentPolicy : BaseEntity
{
    private FulfillmentPolicy()
    {
    }

    public Guid ServiceVariantId { get; private set; }
    public int Version { get; private set; }
    public ProcurementStrategy ProcurementStrategy { get; private set; }
    public FulfillmentMode FulfillmentMode { get; private set; }
    public DeliveryArtifactType DeliveryArtifactType { get; private set; }

    public ServiceVariant ServiceVariant { get; private set; } = null!;

    public static FulfillmentPolicy Create(
        Guid serviceVariantId,
        int version,
        ProcurementStrategy procurementStrategy,
        FulfillmentMode fulfillmentMode,
        DeliveryArtifactType deliveryArtifactType)
    {
        if (serviceVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Service variant ID is required.",
                nameof(serviceVariantId));
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
        EnsureCompatible(
            procurementStrategy,
            fulfillmentMode,
            deliveryArtifactType);

        return new FulfillmentPolicy
        {
            ServiceVariantId = serviceVariantId,
            Version = version,
            ProcurementStrategy = procurementStrategy,
            FulfillmentMode = fulfillmentMode,
            DeliveryArtifactType = deliveryArtifactType
        };
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

    private static void EnsureCompatible(
        ProcurementStrategy procurementStrategy,
        FulfillmentMode fulfillmentMode,
        DeliveryArtifactType deliveryArtifactType)
    {
        if (fulfillmentMode == FulfillmentMode.InstructionOnly ||
            procurementStrategy == ProcurementStrategy.NotRequired ||
            deliveryArtifactType == DeliveryArtifactType.Instructions)
        {
            if (fulfillmentMode != FulfillmentMode.InstructionOnly ||
                procurementStrategy != ProcurementStrategy.NotRequired ||
                deliveryArtifactType != DeliveryArtifactType.Instructions)
            {
                throw new InvalidOperationException(
                    "Instruction-only fulfillment requires no procurement and an Instructions artifact.");
            }

            return;
        }
        var isCompatible = fulfillmentMode switch
        {
            FulfillmentMode.ManualAgent =>
                procurementStrategy == ProcurementStrategy.OnDemandManual,
            FulfillmentMode.InventoryCode =>
                procurementStrategy is ProcurementStrategy.PreStockedInventory or
                    ProcurementStrategy.PreferInventoryThenManual,
            FulfillmentMode.ExternalAutomation =>
                procurementStrategy == ProcurementStrategy.OnDemandSupplierApi,
            _ => false
        };

        if (!isCompatible)
        {
            throw new InvalidOperationException(
                "Procurement strategy is incompatible with the fulfillment mode.");
        }
    }
}
