using InstagramClone.Application.Common.DTOs;
using InstagramClone.Application.Features.Notifications.DTOs;
using InstagramClone.Application.Features.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace InstagramClone.API.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationController(INotificationServices notificationServices) : BaseApiController
    {
        // GET: api/notifications
        [HttpGet]
        public async Task<ActionResult<CursorPagedResponse<NotificationDto>>> GetNotifications([FromQuery] CursorPaginationRequest request)
        {
            var result = await notificationServices.GetUserNotificationsAsync(request);
            return ToActionResult(result);
        }

        // GET: api/notifications/unread-count
        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var result = await notificationServices.GetUnreadCountAsync();
            return ToActionResult(result);
        }

        // PUT: api/notifications/{id}/read
        [HttpPut("{id:guid}/read")]
        public async Task<ActionResult<bool>> MarkAsRead([FromRoute] Guid id)
        {
            var result = await notificationServices.MarkAsReadAsync(id);
            return ToActionResult(result);
        }

        // PUT: api/notifications/read-all
        [HttpPut("read-all")]
        public async Task<ActionResult<bool>> MarkAllAsRead()
        {
            var result = await notificationServices.MarkAllAsReadAsync();
            return ToActionResult(result);
        }
    }
}
