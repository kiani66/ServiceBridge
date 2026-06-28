using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class InitiatePaymentRequest
{
    [Required(ErrorMessage = "Order is required.")]
    public Guid OrderId { get; set; }

    [Required(ErrorMessage = "Payment ID is required.")]
    public Guid PaymentId { get; set; }
}
