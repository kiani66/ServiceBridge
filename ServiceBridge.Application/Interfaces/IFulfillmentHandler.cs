using ServiceBridge.Application.Fulfillment;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface IFulfillmentHandler
{
    FulfillmentMode Mode { get; }

    Task<FulfillmentHandlerResult> ExecuteAsync(
        FulfillmentHandlerContext context,
        CancellationToken cancellationToken);
}
