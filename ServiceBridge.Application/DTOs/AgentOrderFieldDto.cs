namespace ServiceBridge.Application.DTOs;

public class AgentOrderFieldDto
{
    public string FieldKey { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsVisibleToCustomerAfterCompletion { get; set; }
    public int DisplayOrder { get; set; }
}
