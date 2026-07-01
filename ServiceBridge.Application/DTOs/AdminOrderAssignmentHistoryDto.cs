namespace ServiceBridge.Application.DTOs;

public class AdminOrderAssignmentHistoryDto
{
    public string AgentName { get; set; } = string.Empty;
    public string? AssignedByAdminName { get; set; }
    public DateTime AssignedAt { get; set; }
    public string? Notes { get; set; }
}
