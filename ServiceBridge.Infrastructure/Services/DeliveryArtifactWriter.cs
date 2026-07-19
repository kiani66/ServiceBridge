using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Services;

public sealed class DeliveryArtifactWriter : IDeliveryArtifactWriter
{
    private readonly ServiceBridgeDbContext _dbContext;
    private readonly IDeliveryArtifactProtector _protector;

    public DeliveryArtifactWriter(
        ServiceBridgeDbContext dbContext,
        IDeliveryArtifactProtector protector)
    {
        _dbContext = dbContext;
        _protector = protector;
    }

    public DeliveryArtifact Stage(
        Order order,
        FulfillmentExecution execution,
        FulfillmentAttempt attempt,
        DeliveryArtifactType artifactType,
        string plaintextValue,
        DateTime createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(attempt);

        if (execution.OrderId != order.Id)
        {
            throw new InvalidOperationException(
                "Fulfillment execution does not belong to the supplied order.");
        }

        if (attempt.FulfillmentExecutionId != execution.Id)
        {
            throw new InvalidOperationException(
                "Fulfillment attempt does not belong to the supplied execution.");
        }

        var protectedValue = _protector.Protect(plaintextValue);
        var fingerprint = _protector.ComputeFingerprint(plaintextValue);
        var artifact = DeliveryArtifact.Create(
            Guid.NewGuid(),
            order.Id,
            execution.Id,
            attempt.Id,
            artifactType,
            protectedValue,
            fingerprint,
            createdAtUtc);

        _dbContext.DeliveryArtifacts.Add(artifact);
        return artifact;
    }
}
