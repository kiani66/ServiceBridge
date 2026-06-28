using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IOrderFactory
{
    Order Create(OrderBuildContext context);
}
