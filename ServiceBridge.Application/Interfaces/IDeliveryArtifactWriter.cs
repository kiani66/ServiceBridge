using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface IDeliveryArtifactWriter
{
    DeliveryArtifact Stage(
        Order order,
        FulfillmentExecution execution,
        FulfillmentAttempt attempt,
        DeliveryArtifactType artifactType,
        string plaintextValue,
        DateTime createdAtUtc);
}
