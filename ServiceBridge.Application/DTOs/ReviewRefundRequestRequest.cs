using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class ReviewRefundRequestRequest
{
    [StringLength(1000)]
    public string? AdminNote { get; set; }
}
