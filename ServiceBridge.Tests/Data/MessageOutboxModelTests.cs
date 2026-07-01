using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;
using Xunit;

namespace ServiceBridge.Tests.Data;

public class MessageOutboxModelTests
{
    private readonly ServiceBridgeDbContext _context;

    public MessageOutboxModelTests()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ServiceBridgeMessageOutboxModelTests")
            .Options;

        _context = new ServiceBridgeDbContext(options);
    }

    [Fact]
    public void MessageOutbox_UsesClientGeneratedIds()
    {
        var entityType = _context.Model.FindEntityType(typeof(MessageOutbox))!;
        var idProperty = entityType.FindProperty(nameof(BaseEntity.Id))!;

        Assert.Equal(ValueGenerated.Never, idProperty.ValueGenerated);
    }

    [Fact]
    public void MessageOutbox_HasExpectedIndexes()
    {
        var entityType = _context.Model.FindEntityType(typeof(MessageOutbox))!;

        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Select(x => x.Name)
                .SequenceEqual(new[]
                {
                    nameof(MessageOutbox.Status),
                    nameof(MessageOutbox.NextAttemptAt),
                    nameof(MessageOutbox.CreatedAt)
                }));

        Assert.Contains(
            entityType.GetIndexes(),
            index => index.Properties.Select(x => x.Name)
                .SequenceEqual(new[]
                {
                    nameof(MessageOutbox.RelatedEntityType),
                    nameof(MessageOutbox.RelatedEntityId)
                }));
    }

    [Fact]
    public void MessageOutbox_HasIntegrityConstraints()
    {
        var designTimeModel = _context.GetService<IDesignTimeModel>().Model;
        var entityType = designTimeModel.FindEntityType(typeof(MessageOutbox))!;
        var constraintNames = entityType
            .GetCheckConstraints()
            .Select(x => x.Name)
            .ToArray();

        Assert.Contains("CK_MessageOutbox_Channel_Valid", constraintNames);
        Assert.Contains("CK_MessageOutbox_Status_Valid", constraintNames);
        Assert.Contains("CK_MessageOutbox_AttemptCount_NonNegative", constraintNames);
    }
}
