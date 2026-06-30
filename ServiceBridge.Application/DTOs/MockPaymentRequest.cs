using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class MockPaymentRequest
{
    [Required(ErrorMessage = "Payment ID is required.")]
    public Guid PaymentId { get; set; }

    public bool Succeed { get; set; } = true;
}
