namespace ServiceBridge.Domain.Entities;

public class CustomerActionRequest : BaseEntity
{
    public Guid OrderId { get; internal set; }
    public Guid RequestedByAgentId { get; internal set; }
    public string RequestText { get; internal set; } = string.Empty;
    public string? CustomerResponse { get; private set; }
    public DateTime RequestedAt { get; internal set; }
    public DateTime? RespondedAt { get; private set; }
    public bool IsResolved { get; private set; }

    public Order Order { get; set; } = null!;
    public User RequestedByAgent { get; set; } = null!;

    public void Respond(string response, DateTime respondedAtUtc)
    {
        if (IsResolved)
        {
            throw new InvalidOperationException(
                "This customer action request has already been resolved.");
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException("Customer response is required.");
        }

        CustomerResponse = response.Trim();
        RespondedAt = respondedAtUtc;
        IsResolved = true;
        UpdatedAt = respondedAtUtc;
    }
}
