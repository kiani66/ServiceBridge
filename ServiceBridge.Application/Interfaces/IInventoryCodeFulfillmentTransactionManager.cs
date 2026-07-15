namespace ServiceBridge.Application.Interfaces;

public interface IInventoryCodeFulfillmentTransactionManager
{
    Task<IInventoryCodeFulfillmentTransaction> BeginAsync(
        CancellationToken cancellationToken = default);
}

public interface IInventoryCodeFulfillmentTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
