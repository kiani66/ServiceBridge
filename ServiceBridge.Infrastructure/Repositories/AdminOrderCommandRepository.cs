using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class AdminOrderCommandRepository : IAdminOrderCommandRepository
{
    private readonly ServiceBridgeDbContext _context;

    public AdminOrderCommandRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public Task<Order?> GetOrderForAssignmentAsync(Guid orderId)
    {
        return _context.Orders
            .Include(x => x.Assignments)
            .FirstOrDefaultAsync(x => x.Id == orderId);
    }

    public Task<Order?> LockOrderForInventoryFulfillmentAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Inventory code fulfillment requires an active database transaction.");
        }

        return _context.Orders
            .FromSqlInterpolated($"""
                SELECT *
                FROM [Orders] WITH (UPDLOCK, ROWLOCK)
                WHERE [Id] = {orderId}
                """)
            .Include(x => x.PaymentRecords)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ActiveAgentExistsAsync(Guid agentId)
    {
        return _context.Users.AnyAsync(x =>
            x.Id == agentId &&
            x.Role == UserRole.Agent &&
            x.IsActive);
    }

    public async Task<bool> SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return false;
        }
    }
}
