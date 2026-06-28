using System.ComponentModel.DataAnnotations;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public class ConfirmPaymentRequest
{
    [Required(ErrorMessage = "Payment ID is required.")]
    public Guid PaymentId { get; set; }

    [MaxLength(200, ErrorMessage = "Gateway reference cannot exceed 200 characters.")]
    public string? GatewayReference { get; set; }

    [EnumDataType(typeof(PaymentStatus), ErrorMessage = "Invalid payment status.")]
    public PaymentStatus Status { get; set; } = PaymentStatus.Completed;

    [MaxLength(1000, ErrorMessage = "Failure reason cannot exceed 1000 characters.")]
    public string? FailureReason { get; set; }
}
