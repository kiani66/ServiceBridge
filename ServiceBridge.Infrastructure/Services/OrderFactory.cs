using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class OrderFactory : IOrderFactory
{
    private readonly IOrderFieldProtector _fieldProtector;

    public OrderFactory(IOrderFieldProtector fieldProtector)
    {
        _fieldProtector = fieldProtector;
    }

    public Order Create(OrderBuildContext context)
    {
        var definitions = context.Service.RequiredFields
            .Where(x =>
                x.FulfillmentMode == context.Request.FulfillmentMode &&
                x.Actor == CredentialActor.Customer)
            .ToDictionary(
            x => x.FieldKey,
            StringComparer.OrdinalIgnoreCase);

        var order = Order.Create(
            context.FulfillmentPolicySnapshot,
            context.Service.ServiceFulfillmentMode);
        order.OrderNumber = context.OrderNumber;
        order.UserId = context.UserId;
        order.ServiceId = context.Service.ServiceId;
        order.ServiceVariantId = context.Service.ServiceVariantId;
        order.OrderType = context.Request.OrderType;
        order.FulfillmentMode = context.Request.FulfillmentMode;
        order.Status = OrderStatus.Created;
        order.ForeignPrice = context.Pricing.ForeignPrice;
        order.CurrencyCode = context.Service.CurrencyCode;
        order.ExchangeRateSnapshot = context.Pricing.ExchangeRate;
        order.MarginPercentSnapshot = context.Pricing.MarginPercent;
        order.BaseAmountIrr = context.Pricing.BaseAmountIrr;
        order.ProfitAmountIrr = context.Pricing.ProfitAmountIrr;
        order.FinalAmountIrr = context.Pricing.FinalAmountIrr;
        order.VariantNameSnapshot = context.Service.VariantName;
        order.VariantDurationDaysSnapshot = context.Service.VariantDurationDays;
        order.VariantRegionCodeSnapshot = context.Service.VariantRegionCode;
        order.CreatedAt = context.CreatedAtUtc;

        foreach (var input in OrderCreationService.GetCredentialInputs(context.Request))
        {
            var definition = definitions[input.FieldKey.Trim()];

            order.FieldValues.Add(new OrderFieldValue
            {
                FieldKey = definition.FieldKey,
                FieldLabel = definition.Label,
                FulfillmentMode = definition.FulfillmentMode,
                Actor = definition.Actor,
                EncryptedValue = _fieldProtector.Protect(input.Value),
                IsSensitive = definition.IsSensitive,
                IsVisibleToCustomerAfterCompletion =
                    definition.IsVisibleToCustomerAfterCompletion,
                DisplayOrder = definition.DisplayOrder,
                CreatedAt = context.CreatedAtUtc
            });
        }

        order.RecordInitialStatus(
            context.UserId,
            context.CreatedAtUtc,
            AppMessages.OrderCreated);
        order.MoveToWaitingForPayment(
            context.UserId,
            context.CreatedAtUtc.AddTicks(1));

        return order;
    }
}
