using System.ComponentModel.DataAnnotations;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public class CreateOrderRequest
{
    [Required(ErrorMessage = "Service is required.")]
    public Guid ServiceId { get; set; }

    public Guid? ServiceVariantId { get; set; }

    public Guid? PlanId { get; set; }

    [EnumDataType(typeof(OrderType), ErrorMessage = "Invalid order type.")]
    public OrderType OrderType { get; set; }

    [EnumDataType(typeof(OrderFulfillmentMode), ErrorMessage = "Invalid fulfillment mode.")]
    public OrderFulfillmentMode FulfillmentMode { get; set; } =
        OrderFulfillmentMode.UseExistingAccount;

    [Required(ErrorMessage = "Order fields are required.")]
    public List<CreateOrderFieldRequest> Fields { get; set; } = new();

    public List<CreateOrderFieldRequest> Credentials { get; set; } = new();
}
