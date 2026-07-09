using Microsoft.EntityFrameworkCore;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Data;

public class ServiceBridgeDbContext : DbContext
{
    public ServiceBridgeDbContext(DbContextOptions<ServiceBridgeDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Provider> Providers { get; set; } = null!;
    public DbSet<ServiceCategory> ServiceCategories { get; set; } = null!;
    public DbSet<DigitalService> DigitalServices { get; set; } = null!;
    public DbSet<ServiceVariant> ServiceVariants { get; set; } = null!;
    public DbSet<ServiceRequiredField> ServiceRequiredFields { get; set; } = null!;
    public DbSet<ExchangeRate> ExchangeRates { get; set; } = null!;
    public DbSet<ProfitMargin> ProfitMargins { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderFieldValue> OrderFieldValues { get; set; } = null!;
    public DbSet<OrderStatusHistory> OrderStatusHistory { get; set; } = null!;
    public DbSet<OrderNote> OrderNotes { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<PaymentEvent> PaymentEvents { get; set; } = null!;
    public DbSet<RefundRequest> RefundRequests { get; set; } = null!;
    public DbSet<RefundRequestStatusHistory> RefundRequestStatusHistory { get; set; } = null!;
    public DbSet<AgentAssignment> AgentAssignments { get; set; } = null!;
    public DbSet<CustomerActionRequest> CustomerActionRequests { get; set; } = null!;
    public DbSet<OrderCredentialAccessLog> OrderCredentialAccessLogs { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<MessageOutbox> MessageOutbox { get; set; } = null!;
    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<TicketMessage> TicketMessages { get; set; } = null!;
    public DbSet<TicketCategory> TicketCategories { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServiceBridgeDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureOrderHistoryIsAppendOnly();
        EnsureAuditRecordsAreAppendOnly();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnsureOrderHistoryIsAppendOnly();
        EnsureAuditRecordsAreAppendOnly();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnsureOrderHistoryIsAppendOnly()
    {
        var invalidEntries = ChangeTracker
            .Entries<OrderStatusHistory>()
            .Where(x => x.State is EntityState.Modified or EntityState.Deleted);

        if (invalidEntries.Any())
        {
            throw new InvalidOperationException("Order status history is append-only.");
        }
    }

    private void EnsureAuditRecordsAreAppendOnly()
    {
        var paymentEventsChanged = ChangeTracker
            .Entries<PaymentEvent>()
            .Any(x => x.State is EntityState.Modified or EntityState.Deleted);
        var assignmentsChanged = ChangeTracker
            .Entries<AgentAssignment>()
            .Any(x => x.State is EntityState.Modified or EntityState.Deleted);
        var credentialLogsChanged = ChangeTracker
            .Entries<OrderCredentialAccessLog>()
            .Any(x => x.State is EntityState.Modified or EntityState.Deleted);
        var refundHistoryChanged = ChangeTracker
            .Entries<RefundRequestStatusHistory>()
            .Any(x => x.State is EntityState.Modified or EntityState.Deleted);
        var auditLogsChanged = ChangeTracker
            .Entries<AuditLog>()
            .Any(x => x.State is EntityState.Modified or EntityState.Deleted);

        if (paymentEventsChanged ||
            assignmentsChanged ||
            credentialLogsChanged ||
            refundHistoryChanged ||
            auditLogsChanged)
        {
            throw new InvalidOperationException("Audit records are append-only.");
        }
    }
}
