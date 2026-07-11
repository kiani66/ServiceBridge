using ServiceBridge.Domain.Enums;
using Xunit;

namespace ServiceBridge.Tests.Inventory;

public class AuditActionTypeStabilityTests
{
    [Fact]
    public void InventoryAuditActionValues_AreStableAndAppendOnly()
    {
        Assert.Equal(13, (int)AuditActionType.TicketClosed);
        Assert.Equal(14, (int)AuditActionType.InventoryItemCreated);
        Assert.Equal(15, (int)AuditActionType.InventoryItemDisabled);
    }
}
