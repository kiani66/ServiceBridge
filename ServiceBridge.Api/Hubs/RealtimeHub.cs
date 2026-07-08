using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Hubs;

[Authorize]
public sealed class RealtimeHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                RealtimeGroups.User(userId));
        }

        if (Context.User?.IsInRole(AppConstants.Roles.Admin) == true)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                RealtimeGroups.Admins);
        }

        if (Context.User?.IsInRole(AppConstants.Roles.Agent) == true &&
            !string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                RealtimeGroups.Agent(userId));
        }

        await base.OnConnectedAsync();
    }
}
