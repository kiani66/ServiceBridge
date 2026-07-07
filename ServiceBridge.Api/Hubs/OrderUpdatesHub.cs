using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Hubs;

[Authorize]
public sealed class OrderUpdatesHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"user:{userId}");
        }

        if (Context.User?.IsInRole(AppConstants.Roles.Admin) == true)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"role:{AppConstants.Roles.Admin}");
        }

        if (Context.User?.IsInRole(AppConstants.Roles.Agent) == true)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"role:{AppConstants.Roles.Agent}");

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    $"agent:{userId}");
            }
        }

        await base.OnConnectedAsync();
    }
}
