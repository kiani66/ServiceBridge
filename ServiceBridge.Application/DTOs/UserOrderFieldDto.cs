namespace ServiceBridge.Application.DTOs;

public class UserOrderFieldDto
{
    public string FieldLabel { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsSensitive { get; set; }
    public int DisplayOrder { get; set; }
}
