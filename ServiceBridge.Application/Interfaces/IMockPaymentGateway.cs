using ServiceBridge.Application.Payments;

namespace ServiceBridge.Application.Interfaces;

public interface IMockPaymentGateway
{
    PaymentGatewayCallback CreateCallback(Guid paymentId, bool succeed);
}
