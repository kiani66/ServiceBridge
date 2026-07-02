using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class InitiatePaymentRequest
{
    [Required(ErrorMessage = "Order is required.")]
    public Guid OrderId { get; set; }

    [Required(ErrorMessage = "Payment ID is required.")]
    public Guid PaymentId { get; set; }

    [MaxLength(50, ErrorMessage = "Gateway provider cannot exceed 50 characters.")]
    public string? GatewayProvider { get; set; }
}
