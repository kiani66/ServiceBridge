using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class SaveAgentDeliveryCredentialsRequest
{
    [Required(ErrorMessage = "Credential fields are required.")]
    public List<CreateOrderFieldRequest> Credentials { get; set; } = new();
}
