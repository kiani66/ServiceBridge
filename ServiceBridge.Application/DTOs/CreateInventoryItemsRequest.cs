namespace ServiceBridge.Application.DTOs;

public sealed class CreateInventoryItemsRequest
{
    public Guid ServiceVariantId { get; set; }
    public IReadOnlyCollection<string>? Codes { get; set; }
}
