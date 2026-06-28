using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IAgentOrderProcessingService
{
    Task<ServiceResult> StartProcessingAsync(
        Guid agentId,
        Guid orderId,
        AgentOrderTransitionRequest request);

    Task<ServiceResult> RequestCustomerActionAsync(
        Guid agentId,
        Guid orderId,
        RequestCustomerActionRequest request);

    Task<ServiceResult> ResumeProcessingAsync(
        Guid agentId,
        Guid orderId,
        AgentOrderTransitionRequest request);

    Task<ServiceResult> CompleteAsync(
        Guid agentId,
        Guid orderId,
        AgentOrderTransitionRequest request);

    Task<ServiceResult> SaveDeliveryCredentialsAsync(
        Guid agentId,
        Guid orderId,
        SaveAgentDeliveryCredentialsRequest request);

    Task<ServiceResult> FailAsync(
        Guid agentId,
        Guid orderId,
        FailOrderRequest request);

    Task<ServiceResult> AddNoteAsync(
        Guid agentId,
        Guid orderId,
        AddAgentOrderNoteRequest request);
}
