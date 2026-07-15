using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakeInventoryCodeFulfillmentTransactionManager :
    IInventoryCodeFulfillmentTransactionManager
{
    private readonly IList<string>? _operations;

    public FakeInventoryCodeFulfillmentTransactionManager(
        IList<string>? operations = null)
    {
        _operations = operations;
    }

    public bool IsActive { get; private set; }
    public int BeginCount { get; private set; }
    public int CommitCount { get; private set; }
    public int RollbackCount { get; private set; }

    public Task<IInventoryCodeFulfillmentTransaction> BeginAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsActive)
        {
            throw new InvalidOperationException(
                "A fake inventory fulfillment transaction is already active.");
        }

        IsActive = true;
        BeginCount++;
        _operations?.Add("transaction.begin");

        return Task.FromResult<IInventoryCodeFulfillmentTransaction>(
            new FakeInventoryCodeFulfillmentTransaction(this));
    }

    private sealed class FakeInventoryCodeFulfillmentTransaction :
        IInventoryCodeFulfillmentTransaction
    {
        private readonly FakeInventoryCodeFulfillmentTransactionManager _owner;
        private bool _committed;
        private bool _disposed;

        public FakeInventoryCodeFulfillmentTransaction(
            FakeInventoryCodeFulfillmentTransactionManager owner)
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
