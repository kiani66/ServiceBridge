namespace ServiceBridge.Application.Interfaces;

public interface IPaymentConfirmationTransactionManager
{
    Task<IPaymentConfirmationTransaction> BeginAsync(
        CancellationToken cancellationToken = default);
}

public interface IPaymentConfirmationTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
