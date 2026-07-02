using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Api.Controllers;

[Authorize]
[Route("api/notifications")]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = INotificationService.DefaultPageSize,
        [FromQuery] bool unreadOnly = false)
    {
        return TryGetUserId(out var userId)
            ? ToActionResult(await _notificationService.GetForUserAsync(
                userId,
                page,
                pageSize,
                unreadOnly))
            : UnauthorizedServiceResult();
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        return TryGetUserId(out var userId)
            ? ToActionResult(await _notificationService.GetUnreadCountAsync(userId))
            : UnauthorizedServiceResult();
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        return TryGetUserId(out var userId)
            ? ToActionResult(await _notificationService.MarkAsReadAsync(userId, id))
            : UnauthorizedServiceResult();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        return TryGetUserId(out var userId)
            ? ToActionResult(await _notificationService.MarkAllAsReadAsync(userId))
            : UnauthorizedServiceResult();
    }
}
