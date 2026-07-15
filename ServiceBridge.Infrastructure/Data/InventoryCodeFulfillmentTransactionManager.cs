using Microsoft.EntityFrameworkCore.Storage;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Infrastructure.Data;

public sealed class InventoryCodeFulfillmentTransactionManager :
    IInventoryCodeFulfillmentTransactionManager
{
    private readonly ServiceBridgeDbContext _context;

    public InventoryCodeFulfillmentTransactionManager(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public async Task<IInventoryCodeFulfillmentTransaction> BeginAsync(
        CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException(
                "Inventory code fulfillment already has an active database transaction.");
        }

        var transaction = await _context.Database.BeginTransactionAsync(
            cancellationToken);
        return new InventoryCodeFulfillmentTransaction(transaction);
    }

    private sealed class InventoryCodeFulfillmentTransaction :
        IInventoryCodeFulfillmentTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public InventoryCodeFulfillmentTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return _transaction.CommitAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return _transaction.DisposeAsync();
        }
    }
}
