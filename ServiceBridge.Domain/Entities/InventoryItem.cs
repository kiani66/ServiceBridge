using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.Exceptions;

namespace ServiceBridge.Domain.Entities;

public class InventoryItem : BaseEntity
{
    public Guid ServiceVariantId { get; private set; }
    public string ProtectedCode { get; private set; } = string.Empty;
    public string CodeFingerprint { get; private set; } = string.Empty;
    public InventoryItemStatus Status { get; private set; }
    public Guid CreatedByAdminId { get; private set; }
    public Guid? ReservedForOrderId { get; private set; }
    public DateTime? ReservedAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public Guid? DisabledByAdminId { get; private set; }
    public DateTime? DisabledAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public ServiceVariant ServiceVariant { get; private set; } = null!;
    public Order? ReservedForOrder { get; private set; }
    public User CreatedByAdmin { get; private set; } = null!;
    public User? DisabledByAdmin { get; private set; }

    public static InventoryItem Create(
        Guid serviceVariantId,
        FulfillmentMode fulfillmentMode,
        string protectedCode,
        string codeFingerprint,
        Guid createdByAdminId,
        DateTime createdAtUtc)
    {
        if (serviceVariantId == Guid.Empty)
        {
            throw new ArgumentException(
                "Service variant ID is required.",
                nameof(serviceVariantId));
        }

        if (fulfillmentMode != FulfillmentMode.InventoryCode)
        {
            throw new InvalidOperationException(
                "Inventory items require InventoryCode fulfillment mode.");
        }

        if (string.IsNullOrWhiteSpace(protectedCode))
        {
            throw new ArgumentException(
                "Protected inventory code is required.",
                nameof(protectedCode));
        }

        if (string.IsNullOrWhiteSpace(codeFingerprint))
        {
            throw new ArgumentException(
                "Inventory code fingerprint is required.",
                nameof(codeFingerprint));
        }

        if (createdByAdminId == Guid.Empty)
        {
            throw new ArgumentException(
                "Creating Admin ID is required.",
                nameof(createdByAdminId));
        }

        EnsureTimestamp(createdAtUtc, nameof(createdAtUtc));

        return new InventoryItem
        {
            ServiceVariantId = serviceVariantId,
            ProtectedCode = protectedCode,
            CodeFingerprint = codeFingerprint,
            Status = InventoryItemStatus.Available,
            CreatedByAdminId = createdByAdminId,
            CreatedAt = createdAtUtc
        };
    }

    public void ReserveFor(Guid orderId, DateTime reservedAtUtc)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        EnsureTimestamp(reservedAtUtc, nameof(reservedAtUtc));

        if (Status == InventoryItemStatus.Reserved)
        {
            if (ReservedForOrderId == orderId)
            {
                return;
            }

            throw new InvalidOperationException(
                "Inventory item is already reserved for another order.");
        }

        if (Status != InventoryItemStatus.Available)
        {
            throw new InvalidInventoryItemTransitionException(
                Status,
                InventoryItemStatus.Reserved);
        }

        Status = InventoryItemStatus.Reserved;
        ReservedForOrderId = orderId;
        ReservedAtUtc = reservedAtUtc;
        UpdatedAt = reservedAtUtc;
    }

    public void MarkDelivered(Guid orderId, DateTime deliveredAtUtc)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        EnsureTimestamp(deliveredAtUtc, nameof(deliveredAtUtc));

        if (Status == InventoryItemStatus.Delivered)
        {
            if (ReservedForOrderId == orderId)
            {
                return;
            }

            throw new InvalidOperationException(
                "Inventory item was delivered to another order.");
        }

        if (Status != InventoryItemStatus.Reserved)
        {
            throw new InvalidInventoryItemTransitionException(
                Status,
                InventoryItemStatus.Delivered);
        }

        if (ReservedForOrderId != orderId)
        {
            throw new InvalidOperationException(
                "Only the order that owns the reservation can receive this inventory item.");
        }

        Status = InventoryItemStatus.Delivered;
        DeliveredAtUtc = deliveredAtUtc;
        UpdatedAt = deliveredAtUtc;
    }

    public void Disable(Guid adminId, DateTime disabledAtUtc)
    {
        if (adminId == Guid.Empty)
        {
            throw new ArgumentException("Admin ID is required.", nameof(adminId));
        }

        EnsureTimestamp(disabledAtUtc, nameof(disabledAtUtc));

        if (Status != InventoryItemStatus.Available)
        {
            throw new InvalidInventoryItemTransitionException(
                Status,
                InventoryItemStatus.Disabled);
        }

        Status = InventoryItemStatus.Disabled;
        DisabledByAdminId = adminId;
        DisabledAtUtc = disabledAtUtc;
        UpdatedAt = disabledAtUtc;
    }

    private static void EnsureTimestamp(DateTime value, string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentException("Timestamp is required.", parameterName);
        }
    }
}
