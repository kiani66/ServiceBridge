using ServiceBridge.Application.DTOs;

namespace ServiceBridge.Application.Interfaces;

public interface IOrderFieldValidator
{
    Dictionary<string, string[]> Validate(
        IReadOnlyCollection<OrderFieldDefinition> definitions,
        IReadOnlyCollection<CreateOrderFieldRequest> inputs);
}
