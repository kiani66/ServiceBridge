using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Settings;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public sealed class DeliveryArtifactWriterTests
{
    [Fact]
    public void Stage_ProtectsValueTracksArtifactAndDoesNotSaveChanges()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        using var context = new SaveTrackingDbContext(options);
        var protector = new DataProtectionDeliveryArtifactProtector(
            new EphemeralDataProtectionProvider(),
            Options.Create(new DeliveryArtifactProtectionOptions
            {
                FingerprintKey = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        "0123456789ABCDEF0123456789ABCDEF"))
            }));
        var writer = new DeliveryArtifactWriter(context, protector);
        const string plaintext = "customer-secret-gift-code";
        var chain = CreateChain();

        var artifact = writer.Stage(
            chain.Order,
            chain.Execution,
            chain.Attempt,
            DeliveryArtifactType.GiftLink,
            plaintext,
            new DateTime(2026, 7, 19, 10, 0, 0, DateTimeKind.Utc));

        Assert.Equal(EntityState.Added, context.Entry(artifact).State);
        Assert.NotEqual(plaintext, artifact.ProtectedValue);
        Assert.DoesNotContain(
            plaintext,
            artifact.ProtectedValue,
            StringComparison.Ordinal);
        Assert.Equal(plaintext, protector.Unprotect(artifact.ProtectedValue));
        Assert.Equal(0, context.SaveChangesCallCount);
        Assert.Equal(0, context.SaveChangesAsyncCallCount);

        AssertExecutionOwnedByAnotherOrderIsRejected();
        AssertAttemptOwnedByAnotherExecutionIsRejected();
    }

    private static void AssertExecutionOwnedByAnotherOrderIsRejected()
    {
        using var context = CreateContext();
        var protector = new RecordingProtector();
        var writer = new DeliveryArtifactWriter(context, protector);
        var order = OrderTestFactory.Create();
        var execution = FulfillmentExecution.Create(
            Guid.NewGuid(),
            Guid.NewGuid());
        var attempt = execution.StartAttempt(
            Guid.NewGuid(),
            FulfillmentMode.ManualAgent,
            Timestamp());

        Assert.Throws<InvalidOperationException>(() => writer.Stage(
            order,
            execution,
            attempt,
            DeliveryArtifactType.AccountAccess,
            "secret",
            Timestamp()));

        Assert.Equal(0, protector.CallCount);
        Assert.Empty(context.DeliveryArtifacts.Local);
    }

    private static void AssertAttemptOwnedByAnotherExecutionIsRejected()
    {
        using var context = CreateContext();
        var protector = new RecordingProtector();
        var writer = new DeliveryArtifactWriter(context, protector);
        var chain = CreateChain();
        var otherExecution = FulfillmentExecution.Create(
            Guid.NewGuid(),
            chain.Order.Id);
        var otherAttempt = otherExecution.StartAttempt(
            Guid.NewGuid(),
            FulfillmentMode.ManualAgent,
            Timestamp());

        Assert.Throws<InvalidOperationException>(() => writer.Stage(
            chain.Order,
            chain.Execution,
            otherAttempt,
            DeliveryArtifactType.AccountAccess,
            "secret",
            Timestamp()));

        Assert.Equal(0, protector.CallCount);
        Assert.Empty(context.DeliveryArtifacts.Local);
    }

    private static SaveTrackingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new SaveTrackingDbContext(options);
    }

    private static ArtifactChain CreateChain()
    {
        var order = OrderTestFactory.Create();
        var execution = FulfillmentExecution.Create(Guid.NewGuid(), order.Id);
        var attempt = execution.StartAttempt(
            Guid.NewGuid(),
            FulfillmentMode.ManualAgent,
            Timestamp());
        return new ArtifactChain(order, execution, attempt);
    }

    private static DateTime Timestamp()
    {
        return new DateTime(2026, 7, 19, 10, 0, 0, DateTimeKind.Utc);
    }

    private sealed record ArtifactChain(
        Order Order,
        FulfillmentExecution Execution,
        FulfillmentAttempt Attempt);

    private sealed class RecordingProtector : IDeliveryArtifactProtector
    {
        public int CallCount { get; private set; }

        public string Protect(string plaintext)
        {
            CallCount++;
            return "protected";
        }

        public string Unprotect(string protectedValue)
        {
            CallCount++;
            return "plaintext";
        }

        public string ComputeFingerprint(string plaintext)
        {
            CallCount++;
            return new string('A', DeliveryArtifact.FingerprintLength);
        }
    }

    private sealed class SaveTrackingDbContext : ServiceBridgeDbContext
    {
        public SaveTrackingDbContext(
            DbContextOptions<ServiceBridgeDbContext> options)
            : base(options)
        {
        }

        public int SaveChangesCallCount { get; private set; }
        public int SaveChangesAsyncCallCount { get; private set; }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            SaveChangesCallCount++;
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            SaveChangesAsyncCallCount++;
            return base.SaveChangesAsync(
                acceptAllChangesOnSuccess,
                cancellationToken);
        }
    }
}
