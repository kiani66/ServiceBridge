using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.AgentOrders;

public class AgentOrderProcessingTests
{
    [Fact]
    public async Task StartProcessing_WhenAssigned_TransitionsAndWritesAgentLog()
    {
        var (agentId, order, service, repository, realtimeNotifier) =
            CreateService(OrderStatus.Assigned);

        var result = await service.StartProcessingAsync(
            agentId,
            order.Id,
            new AgentOrderTransitionRequest { Note = "Started activation." });

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Processing, order.Status);
        Assert.Equal(1, repository.SaveCount);
        var message = Assert.Single(realtimeNotifier.Messages);
        Assert.Equal(order.Id, message.OrderId);
        Assert.Equal(OrderStatus.Processing.ToString(), message.NewStatus);
        Assert.Equal(order.UserId, message.UserId);
        Assert.Equal(agentId, message.AgentId);
        AssertAgentLog(order, agentId, OrderStatus.Assigned, OrderStatus.Processing);
    }

    [Fact]
    public async Task RequestCustomerAction_WhenProcessing_StaysProcessingAndCreatesRequest()
    {
        var (agentId, order, service, _, _) =
            CreateService(OrderStatus.Processing);

        var result = await service.RequestCustomerActionAsync(
            agentId,
            order.Id,
            new RequestCustomerActionRequest { Note = "Send the verification code." });

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Processing, order.Status);
        Assert.Empty(order.StatusHistory);
        var action = Assert.Single(order.CustomerActionRequests);
        Assert.False(action.IsResolved);
        Assert.Equal("Send the verification code.", action.RequestText);
        var customerNote = Assert.Single(order.Notes);
        Assert.True(customerNote.IsVisibleToCustomer);
        Assert.Equal("Send the verification code.", customerNote.Text);
    }

    [Fact]
    public async Task RequestCustomerAction_WhenNoteIsEmpty_ReturnsConflict()
    {
        var (agentId, order, service, repository, _) =
            CreateService(OrderStatus.Processing);

        var result = await service.RequestCustomerActionAsync(
            agentId,
            order.Id,
            new RequestCustomerActionRequest { Note = " " });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task RequestCustomerAction_WhenOrderBelongsToAnotherAgent_ReturnsNotFound()
    {
        var (_, order, service, repository, _) =
            CreateService(OrderStatus.Processing);

        var result = await service.RequestCustomerActionAsync(
            Guid.NewGuid(),
            order.Id,
            new RequestCustomerActionRequest { Note = "Please confirm your account email." });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Empty(order.CustomerActionRequests);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task ResumeProcessing_WhenCustomerResponded_AcknowledgesWithoutStatusChange()
    {
        var (agentId, order, service, _, _) =
            CreateService(OrderStatus.Processing);
        order.RequestCustomerAction(agentId, "Send OTP.");
        var action = Assert.Single(order.CustomerActionRequests);
        order.RespondToCustomerAction(order.UserId, action.Id, "123456");

        var result = await service.ResumeProcessingAsync(
            agentId,
            order.Id,
            new AgentOrderTransitionRequest());

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Processing, order.Status);
        Assert.Empty(order.StatusHistory);
    }

    [Fact]
    public async Task Complete_WhenProcessing_SetsCompletedAtAndWritesLog()
    {
        var (agentId, order, service, _, _) =
            CreateService(OrderStatus.Processing);

        var result = await service.CompleteAsync(
            agentId,
            order.Id,
            new AgentOrderTransitionRequest());

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.NotNull(order.CompletedAt);
        AssertAgentLog(order, agentId, OrderStatus.Processing, OrderStatus.Completed);
    }

    [Fact]
    public async Task Complete_WhenCreateNewAccountDeliveryFieldsAreMissing_ReturnsValidationFailure()
    {
        var (agentId, order, service, repository, _) =
            CreateService(OrderStatus.Processing);
        ConfigureCreateNewAccountDelivery(order);

        var result = await service.CompleteAsync(
            agentId,
            order.Id,
            new AgentOrderTransitionRequest());

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(OrderStatus.Processing, order.Status);
        Assert.Equal(0, repository.SaveCount);
        Assert.NotNull(result.ValidationErrors);
        Assert.Contains("created_password", result.ValidationErrors.Keys);
    }

    [Fact]
    public async Task SaveDeliveryCredentials_WhenAssignedAgentSubmitsFields_EncryptsAndSaves()
    {
        var (agentId, order, service, repository, _) =
            CreateService(OrderStatus.Processing);
        ConfigureCreateNewAccountDelivery(order);

        var result = await service.SaveDeliveryCredentialsAsync(
            agentId,
            order.Id,
            new SaveAgentDeliveryCredentialsRequest
            {
                Credentials =
                [
                    new CreateOrderFieldRequest
                    {
                        FieldKey = "created_email",
                        Value = "new@example.com"
                    },
                    new CreateOrderFieldRequest
                    {
                        FieldKey = "created_password",
                        Value = "created-secret"
                    }
                ]
            });

        Assert.True(result.Success);
        Assert.Equal(1, repository.SaveCount);
        Assert.All(
            order.FieldValues,
            value => Assert.StartsWith("protected::", value.EncryptedValue));
        Assert.DoesNotContain(
            order.FieldValues,
            value => value.EncryptedValue == "created-secret");
        Assert.All(order.FieldValues, value => Assert.Equal(CredentialActor.Agent, value.Actor));
    }

    [Fact]
    public async Task Complete_WhenCreateNewAccountDeliveryFieldsAreSaved_CompletesOrder()
    {
        var (agentId, order, service, _, _) =
            CreateService(OrderStatus.Processing);
        ConfigureCreateNewAccountDelivery(order);

        await service.SaveDeliveryCredentialsAsync(
            agentId,
            order.Id,
            new SaveAgentDeliveryCredentialsRequest
            {
                Credentials =
                [
                    new CreateOrderFieldRequest
                    {
                        FieldKey = "created_email",
                        Value = "new@example.com"
                    },
                    new CreateOrderFieldRequest
                    {
                        FieldKey = "created_password",
                        Value = "created-secret"
                    }
                ]
            });

        order.StatusHistory.ToList().Clear();

        var result = await service.CompleteAsync(
            agentId,
            order.Id,
            new AgentOrderTransitionRequest());

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    [Fact]
    public async Task SaveDeliveryCredentials_WhenOrderBelongsToAnotherAgent_ReturnsNotFound()
    {
        var (_, order, service, repository, _) =
            CreateService(OrderStatus.Processing);
        ConfigureCreateNewAccountDelivery(order);

        var result = await service.SaveDeliveryCredentialsAsync(
            Guid.NewGuid(),
            order.Id,
            new SaveAgentDeliveryCredentialsRequest
            {
                Credentials =
                [
                    new CreateOrderFieldRequest
                    {
                        FieldKey = "created_email",
                        Value = "new@example.com"
                    }
                ]
            });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task Fail_WhenProcessing_StoresReasonAndWritesLog()
    {
        var (agentId, order, service, _, _) =
            CreateService(OrderStatus.Processing);

        var result = await service.FailAsync(
            agentId,
            order.Id,
            new FailOrderRequest { Reason = "Provider rejected activation." });

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Failed, order.Status);
        var log = Assert.Single(order.StatusHistory);
        Assert.Equal("Provider rejected activation.", log.Note);
        AssertAgentLog(order, agentId, OrderStatus.Processing, OrderStatus.Failed);
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Completed)]
    public async Task StartProcessing_WhenStateIsInvalid_ReturnsConflict(
        OrderStatus status)
    {
        var (agentId, order, service, repository, _) = CreateService(status);

        var result = await service.StartProcessingAsync(
            agentId,
            order.Id,
            new AgentOrderTransitionRequest());

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task ProcessingOperation_WhenOrderBelongsToAnotherAgent_ReturnsNotFound()
    {
        var (_, order, service, repository, _) =
            CreateService(OrderStatus.Assigned);

        var result = await service.StartProcessingAsync(
            Guid.NewGuid(),
            order.Id,
            new AgentOrderTransitionRequest());

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task AddNote_WhenAssigned_AddsVisibilityControlledNote()
    {
        var (agentId, order, service, repository, _) =
            CreateService(OrderStatus.Assigned);

        var result = await service.AddNoteAsync(
            agentId,
            order.Id,
            new AddAgentOrderNoteRequest
            {
                Text = "Customer can see this.",
                IsVisibleToCustomer = true
            });

        Assert.True(result.Success);
        Assert.Equal(1, repository.SaveCount);
        var note = Assert.Single(order.Notes);
        Assert.Equal(agentId, note.AuthorId);
        Assert.True(note.IsVisibleToCustomer);
        Assert.Equal("Customer can see this.", note.Text);
    }

    [Fact]
    public async Task SaveConflict_ReturnsConflict()
    {
        var (agentId, order, service, repository, realtimeNotifier) =
            CreateService(OrderStatus.Assigned);
        repository.SaveSucceeds = false;

        var result = await service.StartProcessingAsync(
            agentId,
            order.Id,
            new AgentOrderTransitionRequest());

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Empty(realtimeNotifier.Messages);
    }

    private static (
        Guid AgentId,
        Order Order,
        AgentOrderProcessingService Service,
        FakeAgentOrderCommandRepository Repository,
        FakeOrderRealtimeNotifier RealtimeNotifier)
        CreateService(OrderStatus status)
    {
        var agentId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            AssignedAgentId = agentId,
            Status = status,
            Service = new DigitalService
            {
                Id = Guid.NewGuid(),
                SupportsUseExistingAccount = true
            }
        };
        var repository = new FakeAgentOrderCommandRepository { Order = order };
        var realtimeNotifier = new FakeOrderRealtimeNotifier();
        return (
            agentId,
            order,
            new AgentOrderProcessingService(
                repository,
                new FakeOrderFieldProtector(),
                new FakeNotificationService(),
                realtimeNotifier),
            repository,
            realtimeNotifier);
    }

    private static void ConfigureCreateNewAccountDelivery(Order order)
    {
        order.FulfillmentMode = OrderFulfillmentMode.CreateNewAccount;
        order.Service.SupportsCreateNewAccount = true;
        order.Service.RequiredFields =
        [
            new ServiceRequiredField
            {
                Id = Guid.NewGuid(),
                ServiceId = order.Service.Id,
                FieldKey = "created_email",
                Label = "Created email",
                FieldType = ServiceFieldType.Email,
                FulfillmentMode = OrderFulfillmentMode.CreateNewAccount,
                Actor = CredentialActor.Agent,
                IsRequired = true,
                DisplayOrder = 1,
                IsVisibleToCustomerAfterCompletion = true
            },
            new ServiceRequiredField
            {
                Id = Guid.NewGuid(),
                ServiceId = order.Service.Id,
                FieldKey = "created_password",
                Label = "Created password",
                FieldType = ServiceFieldType.Password,
                FulfillmentMode = OrderFulfillmentMode.CreateNewAccount,
                Actor = CredentialActor.Agent,
                IsRequired = true,
                IsSensitive = true,
                DisplayOrder = 2,
                IsVisibleToCustomerAfterCompletion = true
            }
        ];
    }

    private static void AssertAgentLog(
        Order order,
        Guid agentId,
        OrderStatus previousStatus,
        OrderStatus newStatus)
    {
        var log = Assert.Single(order.StatusHistory);
        Assert.Equal(previousStatus, log.PreviousStatus);
        Assert.Equal(newStatus, log.NewStatus);
        Assert.Equal(OrderLogActorType.Agent, log.ActorType);
        Assert.Equal(agentId, log.ActorId);
        Assert.Equal(agentId, log.ChangedByUserId);
    }
}
