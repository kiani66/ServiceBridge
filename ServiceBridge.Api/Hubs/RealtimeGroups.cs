namespace ServiceBridge.Api.Hubs;

public static class RealtimeGroups
{
    public const string Admins = "admins";

    public static string User(Guid userId)
    {
        return User(userId.ToString());
    }

    public static string User(string userId)
    {
        return $"user:{userId}";
    }

    public static string Agent(Guid agentId)
    {
        return Agent(agentId.ToString());
    }

    public static string Agent(string agentId)
    {
        return $"agent:{agentId}";
    }

    public static string Order(Guid orderId)
    {
        return $"order:{orderId}";
    }
}
