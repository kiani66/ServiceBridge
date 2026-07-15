using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakePaymentConfirmationTransactionManager :
    IPaymentConfirmationTransactionManager
{
    private readonly IList<string>? _operations;

    public FakePaymentConfirmationTransactionManager(IList<string>? operations = null)
    {
        _operations = operations;
    }

    public bool IsActive { get; private set; }
    public int BeginCount { get; private set; }
    public int CommitCount { get; private set; }
    public int RollbackCount { get; private set; }

    public Task<IPaymentConfirmationTransaction> BeginAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsActive)
        {
            throw new InvalidOperationException(
                "A fake payment confirmation transaction is already active.");
        }

        IsActive = true;
        BeginCount++;
        _operations?.Add("transaction.begin");

        return Task.FromResult<IPaymentConfirmationTransaction>(
            new FakePaymentConfirmationTransaction(this));
    }

    private sealed class FakePaymentConfirmationTransaction :
        IPaymentConfirmationTransaction
    {
        private readonly FakePaymentConfirmationTransactionManager _owner;
        private bool _committed;
        private bool _disposed;

        public FakePaymentConfirmationTransaction(
            FakePaymentConfirmationTransactionManager owner)
        {
            _owner = owner;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            _committed = true;
            _owner.CommitCount++;
            _owner._operations?.Add("transaction.commit");
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _disposed = true;
            if (!_committed)
            {
                _owner.RollbackCount++;
                _owner._operations?.Add("transaction.rollback");
            }

            _owner.IsActive = false;
            return ValueTask.CompletedTask;
        }
    }
}
