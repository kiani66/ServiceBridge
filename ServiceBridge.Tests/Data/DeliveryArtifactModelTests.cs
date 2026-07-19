using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;
using Xunit;

namespace ServiceBridge.Tests.Data;

public sealed class DeliveryArtifactModelTests
{
    private readonly ServiceBridgeDbContext _context;
    private readonly IEntityType _entityType;

    public DeliveryArtifactModelTests()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=ServiceBridgeDeliveryArtifactModelTests")
            .Options;
        _context = new ServiceBridgeDbContext(options);
        _entityType = _context.Model.FindEntityType(typeof(DeliveryArtifact))!;
    }

    [Fact]
    public void IdentifierIsClientGeneratedAndSensitiveColumnsAreBounded()
    {
        Assert.Equal(
            ValueGenerated.Never,
            _entityType.FindProperty(nameof(DeliveryArtifact.Id))!.ValueGenerated);
        Assert.Equal(
            DeliveryArtifact.ProtectedValueMaxLength,
            _entityType.FindProperty(nameof(DeliveryArtifact.ProtectedValue))!
                .GetMaxLength());
        Assert.Equal(
            DeliveryArtifact.FingerprintLength,
            _entityType.FindProperty(nameof(DeliveryArtifact.Fingerprint))!
                .GetMaxLength());
    }

    [Fact]
    public void RequiredRelationshipsUseRestrictDelete()
    {
        var foreignKeys = _entityType.GetForeignKeys().ToArray();

        Assert.Equal(3, foreignKeys.Length);
        Assert.All(foreignKeys, foreignKey =>
        {
            Assert.True(foreignKey.IsRequired);
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        });
        Assert.Contains(foreignKeys, foreignKey =>
            PropertyNames(foreignKey).SequenceEqual(new[]
            {
                nameof(DeliveryArtifact.OrderId)
            }));
        Assert.Contains(foreignKeys, foreignKey =>
            PropertyNames(foreignKey).SequenceEqual(new[]
            {
                nameof(DeliveryArtifact.FulfillmentExecutionId),
                nameof(DeliveryArtifact.OrderId)
            }));
        Assert.Contains(foreignKeys, foreignKey =>
            PropertyNames(foreignKey).SequenceEqual(new[]
            {
                nameof(DeliveryArtifact.FulfillmentAttemptId),
                nameof(DeliveryArtifact.FulfillmentExecutionId)
            }));

        Assert.Contains(
            _context.Model.FindEntityType(typeof(FulfillmentExecution))!
                .GetKeys(),
            key => PropertyNames(key).SequenceEqual(new[]
            {
                nameof(FulfillmentExecution.Id),
                nameof(FulfillmentExecution.OrderId)
            }));
        Assert.Contains(
            _context.Model.FindEntityType(typeof(FulfillmentAttempt))!
                .GetKeys(),
            key => PropertyNames(key).SequenceEqual(new[]
            {
                nameof(FulfillmentAttempt.Id),
                nameof(FulfillmentAttempt.FulfillmentExecutionId)
            }));
    }

    [Fact]
    public void RequiredIndexesExistWithoutGlobalFingerprintUniqueness()
    {
        var indexes = _entityType.GetIndexes().ToArray();

        Assert.Contains(indexes, index =>
            PropertyNames(index).SequenceEqual(new[]
            {
                nameof(DeliveryArtifact.OrderId)
            }));
        Assert.Contains(indexes, index =>
            PropertyNames(index).SequenceEqual(new[]
            {
                nameof(DeliveryArtifact.FulfillmentAttemptId)
            }));
        Assert.DoesNotContain(indexes, index =>
            index.Properties.Any(property =>
                property.Name == nameof(DeliveryArtifact.Fingerprint)));
    }

    [Fact]
    public void ArtifactTypeAndProtectedValueHaveDatabaseChecks()
    {
        var constraints = _context.GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(DeliveryArtifact))!
            .GetCheckConstraints()
            .ToArray();

        Assert.Contains(constraints, constraint =>
            constraint.Name == "CK_DeliveryArtifacts_ArtifactType_Valid");
        Assert.Contains(constraints, constraint =>
            constraint.Name == "CK_DeliveryArtifacts_ProtectedValue_Bounded");
    }

    private static string[] PropertyNames(IReadOnlyKey key)
    {
        return key.Properties.Select(property => property.Name).ToArray();
    }

    private static string[] PropertyNames(IReadOnlyForeignKey foreignKey)
    {
        return foreignKey.Properties.Select(property => property.Name).ToArray();
    }

    private static string[] PropertyNames(IReadOnlyIndex index)
    {
        return index.Properties.Select(property => property.Name).ToArray();
    }
}
