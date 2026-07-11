using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.Exceptions;
using Xunit;

namespace ServiceBridge.Tests.InventoryDomain;

public class InventoryItemTests
{
    [Fact]
    public void Create_WithValidInput_CreatesAvailableItemWithClientGeneratedId()
    {
        var item = CreateItem();

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(InventoryItemStatus.Available, item.Status);
        Assert.Null(item.ReservedForOrderId);
        Assert.Null(item.ReservedAtUtc);
        Assert.Null(item.DeliveredAtUtc);
        Assert.Null(item.DisabledByAdminId);
        Assert.Null(item.DisabledAtUtc);
    }

    [Fact]
    public void Create_WithEmptyServiceVariantId_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateItem(serviceVariantId: Guid.Empty));
    }

    [Fact]
    public void Create_WithNonInventoryFulfillmentMode_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CreateItem(fulfillmentMode: FulfillmentMode.ManualAgent));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyProtectedCode_Throws(string? protectedCode)
    {
        Assert.Throws<ArgumentException>(() => CreateItem(protectedCode: protectedCode!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyCodeFingerprint_Throws(string? codeFingerprint)
    {
        Assert.Throws<ArgumentException>(() => CreateItem(codeFingerprint: codeFingerprint!));
    }

    [Fact]
    public void Create_WithEmptyCreatedByAdminId_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateItem(createdByAdminId: Guid.Empty));
    }

    [Fact]
    public void Create_WithDefaultCreatedAtUtc_Throws()
    {
        Assert.Throws<ArgumentException>(() => InventoryItem.Create(
            Guid.NewGuid(),
            FulfillmentMode.InventoryCode,
            "protected-code",
            "fingerprint",
            Guid.NewGuid(),
            default));
    }

    [Fact]
    public void ReserveFor_WithEmptyOrderId_ThrowsWithoutChangingItem()
    {
        var item = CreateItem();
        var before = Capture(item);

        Assert.Throws<ArgumentException>(() => item.ReserveFor(Guid.Empty, UtcNow));

        AssertUnchanged(item, before);
    }

    [Fact]
    public void ReserveFor_WithDefaultTimestamp_ThrowsWithoutChangingItem()
    {
        var item = CreateItem();
        var before = Capture(item);

        Assert.Throws<ArgumentException>(() => item.ReserveFor(Guid.NewGuid(), default));

        AssertUnchanged(item, before);
    }

    [Fact]
    public void ReserveFor_WhenAvailable_ReservesItem()
    {
        var item = CreateItem();
        var orderId = Guid.NewGuid();
        var reservedAtUtc = UtcNow.AddMinutes(1);

        item.ReserveFor(orderId, reservedAtUtc);

        Assert.Equal(InventoryItemStatus.Reserved, item.Status);
        Assert.Equal(orderId, item.ReservedForOrderId);
        Assert.Equal(reservedAtUtc, item.ReservedAtUtc);
        Assert.Equal(reservedAtUtc, item.UpdatedAt);
    }

    [Fact]
    public void ReserveFor_WhenRepeatedForSameOrder_IsIdempotent()
    {
        var item = CreateItem();
        var orderId = Guid.NewGuid();
        var firstReservedAtUtc = UtcNow.AddMinutes(1);
        item.ReserveFor(orderId, firstReservedAtUtc);

        item.ReserveFor(orderId, UtcNow.AddMinutes(2));

        Assert.Equal(firstReservedAtUtc, item.ReservedAtUtc);
        Assert.Equal(firstReservedAtUtc, item.UpdatedAt);
    }

    [Fact]
    public void ReserveFor_WhenReservedForDifferentOrder_Throws()
    {
        var item = CreateItem();
        item.ReserveFor(Guid.NewGuid(), UtcNow.AddMinutes(1));
        var before = Capture(item);

        Assert.Throws<InvalidOperationException>(() =>
            item.ReserveFor(Guid.NewGuid(), UtcNow.AddMinutes(2)));

        AssertUnchanged(item, before);
    }

    [Fact]
    public void MarkDelivered_WithEmptyOrderId_ThrowsWithoutChangingItem()
    {
        var item = CreateReservedItem(out _);
        var before = Capture(item);

        Assert.Throws<ArgumentException>(() => item.MarkDelivered(Guid.Empty, UtcNow));

        AssertUnchanged(item, before);
    }

    [Fact]
    public void MarkDelivered_WithDefaultTimestamp_ThrowsWithoutChangingItem()
    {
        var item = CreateReservedItem(out var orderId);
        var before = Capture(item);

        Assert.Throws<ArgumentException>(() => item.MarkDelivered(orderId, default));

        AssertUnchanged(item, before);
    }

    [Fact]
    public void MarkDelivered_WhenReservedForOrder_DeliversItem()
    {
        var item = CreateItem();
        var orderId = Guid.NewGuid();
        var deliveredAtUtc = UtcNow.AddMinutes(2);
        item.ReserveFor(orderId, UtcNow.AddMinutes(1));

        item.MarkDelivered(orderId, deliveredAtUtc);

        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.Equal(deliveredAtUtc, item.DeliveredAtUtc);
        Assert.Equal(deliveredAtUtc, item.UpdatedAt);
    }

    [Fact]
    public void MarkDelivered_WhenRepeatedForSameOrder_IsIdempotent()
    {
        var item = CreateItem();
        var orderId = Guid.NewGuid();
        var firstDeliveredAtUtc = UtcNow.AddMinutes(2);
        item.ReserveFor(orderId, UtcNow.AddMinutes(1));
        item.MarkDelivered(orderId, firstDeliveredAtUtc);

        item.MarkDelivered(orderId, UtcNow.AddMinutes(3));

        Assert.Equal(firstDeliveredAtUtc, item.DeliveredAtUtc);
        Assert.Equal(firstDeliveredAtUtc, item.UpdatedAt);
    }

    [Fact]
    public void MarkDelivered_ForDifferentOrder_Throws()
    {
        var item = CreateItem();
        item.ReserveFor(Guid.NewGuid(), UtcNow.AddMinutes(1));
        var before = Capture(item);

        Assert.Throws<InvalidOperationException>(() =>
            item.MarkDelivered(Guid.NewGuid(), UtcNow.AddMinutes(2)));

        AssertUnchanged(item, before);
    }

    [Fact]
    public void Disable_WithEmptyAdminId_ThrowsWithoutChangingItem()
    {
        var item = CreateItem();
        var before = Capture(item);

        Assert.Throws<ArgumentException>(() => item.Disable(Guid.Empty, UtcNow));

        AssertUnchanged(item, before);
    }

    [Fact]
    public void Disable_WithDefaultTimestamp_ThrowsWithoutChangingItem()
    {
        var item = CreateItem();
        var before = Capture(item);

        Assert.Throws<ArgumentException>(() => item.Disable(Guid.NewGuid(), default));

        AssertUnchanged(item, before);
    }

    [Fact]
    public void Disable_WhenAvailable_DisablesItem()
    {
        var item = CreateItem();
        var adminId = Guid.NewGuid();
        var disabledAtUtc = UtcNow.AddMinutes(1);

        item.Disable(adminId, disabledAtUtc);

        Assert.Equal(InventoryItemStatus.Disabled, item.Status);
        Assert.Equal(adminId, item.DisabledByAdminId);
        Assert.Equal(disabledAtUtc, item.DisabledAtUtc);
        Assert.Equal(disabledAtUtc, item.UpdatedAt);
    }

    [Fact]
    public void Disable_WhenRepeatedBySameAdmin_IsIdempotent()
    {
        var item = CreateItem();
        var adminId = Guid.NewGuid();
        var firstDisabledAtUtc = UtcNow.AddMinutes(1);
        item.Disable(adminId, firstDisabledAtUtc);

        item.Disable(adminId, UtcNow.AddMinutes(2));

        Assert.Equal(firstDisabledAtUtc, item.DisabledAtUtc);
        Assert.Equal(firstDisabledAtUtc, item.UpdatedAt);
    }

    [Fact]
    public void Disable_WhenRepeatedByDifferentAdmin_ThrowsWithoutChangingItem()
    {
        var item = CreateItem();
        item.Disable(Guid.NewGuid(), UtcNow.AddMinutes(1));
        var before = Capture(item);

        Assert.Throws<InvalidOperationException>(() =>
            item.Disable(Guid.NewGuid(), UtcNow.AddMinutes(2)));

        AssertUnchanged(item, before);
    }

    [Fact]
    public void Disable_WhenReserved_ThrowsTransitionException()
    {
        var item = CreateItem();
        item.ReserveFor(Guid.NewGuid(), UtcNow.AddMinutes(1));

        var exception = Assert.Throws<InvalidInventoryItemTransitionException>(() =>
            item.Disable(Guid.NewGuid(), UtcNow.AddMinutes(2)));

        Assert.Equal(InventoryItemStatus.Reserved, exception.CurrentStatus);
        Assert.Equal(InventoryItemStatus.Disabled, exception.RequestedStatus);
    }

    [Fact]
    public void Disable_WhenDelivered_ThrowsTransitionException()
    {
        var item = CreateDeliveredItem();
        var before = Capture(item);

        var exception = Assert.Throws<InvalidInventoryItemTransitionException>(() =>
            item.Disable(Guid.NewGuid(), UtcNow.AddMinutes(3)));

        Assert.Equal(InventoryItemStatus.Delivered, exception.CurrentStatus);
        Assert.Equal(InventoryItemStatus.Disabled, exception.RequestedStatus);
        AssertUnchanged(item, before);
    }

    [Fact]
    public void ReserveFor_WhenDisabled_ThrowsTransitionException()
    {
        var item = CreateItem();
        item.Disable(Guid.NewGuid(), UtcNow.AddMinutes(1));
        var before = Capture(item);

        var exception = Assert.Throws<InvalidInventoryItemTransitionException>(() =>
            item.ReserveFor(Guid.NewGuid(), UtcNow.AddMinutes(2)));

        Assert.Equal(InventoryItemStatus.Disabled, exception.CurrentStatus);
        Assert.Equal(InventoryItemStatus.Reserved, exception.RequestedStatus);
        AssertUnchanged(item, before);
    }

    [Fact]
    public void DeliveredAndDisabled_AreTerminal()
    {
        var delivered = CreateDeliveredItem();
        var disabled = CreateItem();
        disabled.Disable(Guid.NewGuid(), UtcNow.AddMinutes(1));
        var deliveredBefore = Capture(delivered);
        var disabledBefore = Capture(disabled);

        var deliveredException = Assert.Throws<InvalidInventoryItemTransitionException>(() =>
            delivered.ReserveFor(Guid.NewGuid(), UtcNow.AddMinutes(3)));
        var disabledException = Assert.Throws<InvalidInventoryItemTransitionException>(() =>
            disabled.MarkDelivered(Guid.NewGuid(), UtcNow.AddMinutes(2)));

        Assert.Equal(InventoryItemStatus.Delivered, deliveredException.CurrentStatus);
        Assert.Equal(InventoryItemStatus.Reserved, deliveredException.RequestedStatus);
        Assert.Equal(InventoryItemStatus.Disabled, disabledException.CurrentStatus);
        Assert.Equal(InventoryItemStatus.Delivered, disabledException.RequestedStatus);
        AssertUnchanged(delivered, deliveredBefore);
        AssertUnchanged(disabled, disabledBefore);
    }

    private static readonly DateTime UtcNow =
        new(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc);

    private static InventoryItem CreateDeliveredItem()
    {
        var item = CreateReservedItem(out var orderId);
        item.MarkDelivered(orderId, UtcNow.AddMinutes(2));
        return item;
    }

    private static InventoryItem CreateReservedItem(out Guid orderId)
    {
        var item = CreateItem();
        orderId = Guid.NewGuid();
        item.ReserveFor(orderId, UtcNow.AddMinutes(1));
        return item;
    }

    private static InventoryItemState Capture(InventoryItem item)
    {
        return new InventoryItemState(
            item.Status,
            item.ReservedForOrderId,
            item.ReservedAtUtc,
            item.DeliveredAtUtc,
            item.DisabledByAdminId,
            item.DisabledAtUtc,
            item.UpdatedAt);
    }

    private static void AssertUnchanged(InventoryItem item, InventoryItemState expected)
    {
        Assert.Equal(expected.Status, item.Status);
        Assert.Equal(expected.ReservedForOrderId, item.ReservedForOrderId);
        Assert.Equal(expected.ReservedAtUtc, item.ReservedAtUtc);
        Assert.Equal(expected.DeliveredAtUtc, item.DeliveredAtUtc);
        Assert.Equal(expected.DisabledByAdminId, item.DisabledByAdminId);
        Assert.Equal(expected.DisabledAtUtc, item.DisabledAtUtc);
        Assert.Equal(expected.UpdatedAt, item.UpdatedAt);
    }

    private static InventoryItem CreateItem(
        Guid? serviceVariantId = null,
        FulfillmentMode fulfillmentMode = FulfillmentMode.InventoryCode,
        string protectedCode = "protected-code",
        string codeFingerprint = "fingerprint",
        Guid? createdByAdminId = null)
    {
        return InventoryItem.Create(
            serviceVariantId ?? Guid.NewGuid(),
            fulfillmentMode,
            protectedCode,
            codeFingerprint,
            createdByAdminId ?? Guid.NewGuid(),
            UtcNow);
    }

    private sealed record InventoryItemState(
        InventoryItemStatus Status,
        Guid? ReservedForOrderId,
        DateTime? ReservedAtUtc,
        DateTime? DeliveredAtUtc,
        Guid? DisabledByAdminId,
        DateTime? DisabledAtUtc,
        DateTime? UpdatedAt);
}
