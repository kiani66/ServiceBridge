using Microsoft.EntityFrameworkCore.Storage;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Infrastructure.Data;

public sealed class PaymentConfirmationTransactionManager :
    IPaymentConfirmationTransactionManager
{
    private readonly ServiceBridgeDbContext _context;

    public PaymentConfirmationTransactionManager(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public async Task<IPaymentConfirmationTransaction> BeginAsync(
        CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException(
                "Payment confirmation already has an active database transaction.");
        }

        var transaction = await _context.Database.BeginTransactionAsync(
            cancellationToken);
        return new PaymentConfirmationTransaction(transaction);
    }

    private sealed class PaymentConfirmationTransaction :
        IPaymentConfirmationTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public PaymentConfirmationTransaction(IDbContextTransaction transaction)
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
