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

        var order = new Order
        {
            OrderNumber = context.OrderNumber,
            UserId = context.UserId,
            ServiceId = context.Service.ServiceId,
            ServiceVariantId = context.Service.ServiceVariantId,
            OrderType = context.Request.OrderType,
            FulfillmentMode = context.Request.FulfillmentMode,
            ServiceFulfillmentModeSnapshot = context.Service.ServiceFulfillmentMode,
            Status = OrderStatus.Created,
            ForeignPrice = context.Pricing.ForeignPrice,
            CurrencyCode = context.Service.CurrencyCode,
            ExchangeRateSnapshot = context.Pricing.ExchangeRate,
            MarginPercentSnapshot = context.Pricing.MarginPercent,
            BaseAmountIrr = context.Pricing.BaseAmountIrr,
            ProfitAmountIrr = context.Pricing.ProfitAmountIrr,
            FinalAmountIrr = context.Pricing.FinalAmountIrr,
            VariantNameSnapshot = context.Service.VariantName,
            VariantDurationDaysSnapshot = context.Service.VariantDurationDays,
            VariantRegionCodeSnapshot = context.Service.VariantRegionCode,
            CreatedAt = context.CreatedAtUtc
        };

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
