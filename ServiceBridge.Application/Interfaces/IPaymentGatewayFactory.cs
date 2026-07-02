using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IPaymentGatewayFactory
{
    ServiceResult<IPaymentGateway> GetGateway(string? provider);
}
