using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TaskManagementMvc.Services;
using System.Security.Claims;

namespace TaskManagementMvc.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly IScalableNotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            IScalableNotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingNotifications([FromQuery] int limit = 50)
        {
            try
            {
                var userId = GetCurrentUserId();
                var notifications = await _notificationService.GetUserNotificationsAsync(userId, limit);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get pending notifications");
                return StatusCode(500, "خطا در دریافت اعلان‌ها");
            }
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _notificationService.MarkAsReadAsync(userId, id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark notification as read: {Id}", id);
                return StatusCode(500, "خطا در علامت‌گذاری اعلان");
            }
        }

        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                var notifications = await _notificationService.GetUserNotificationsAsync(userId);
                
                foreach (var notification in notifications.Where(n => !n.IsRead))
                {
                    await _notificationService.MarkAsReadAsync(userId, notification.Id);
                }
                
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark all notifications as read");
                return StatusCode(500, "خطا در علامت‌گذاری همه اعلان‌ها");
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearNotifications()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _notificationService.ClearUserNotificationsAsync(userId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear notifications");
                return StatusCode(500, "خطا در پاک کردن اعلان‌ها");
            }
        }

        [HttpPost("send-to-user")]
        public async Task<IActionResult> SendToUser([FromBody] SendNotificationRequest request)
        {
            try
            {
                var notification = new ScalableNotificationMessage
                {
                    Type = request.Type,
                    Title = request.Title,
                    Message = request.Message,
                    Persistent = request.Persistent,
                    Duration = request.Duration,
                    ActionUrl = request.ActionUrl,
                    ActionText = request.ActionText
                };

                await _notificationService.SendToUserAsync(request.UserId, notification);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to user: {UserId}", request.UserId);
                return StatusCode(500, "خطا در ارسال اعلان");
            }
        }

        [HttpPost("send-to-company")]
        public async Task<IActionResult> SendToCompany([FromBody] SendCompanyNotificationRequest request)
        {
            try
            {
                var notification = new ScalableNotificationMessage
                {
                    Type = request.Type,
                    Title = request.Title,
                    Message = request.Message,
                    Persistent = request.Persistent,
                    Duration = request.Duration,
                    ActionUrl = request.ActionUrl,
                    ActionText = request.ActionText
                };

                await _notificationService.SendToCompanyAsync(request.CompanyId, notification);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to company: {CompanyId}", request.CompanyId);
                return StatusCode(500, "خطا در ارسال اعلان به شرکت");
            }
        }

        [HttpPost("send-to-role")]
        public async Task<IActionResult> SendToRole([FromBody] SendRoleNotificationRequest request)
        {
            try
            {
                var notification = new ScalableNotificationMessage
                {
                    Type = request.Type,
                    Title = request.Title,
                    Message = request.Message,
                    Persistent = request.Persistent,
                    Duration = request.Duration,
                    ActionUrl = request.ActionUrl,
                    ActionText = request.ActionText
                };

                await _notificationService.SendToRoleAsync(request.Role, notification);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to role: {Role}", request.Role);
                return StatusCode(500, "خطا در ارسال اعلان به نقش");
            }
        }

        [HttpPost("test")]
        public async Task<IActionResult> SendTestNotification()
        {
            try
            {
                var userId = GetCurrentUserId();
                var notification = new ScalableNotificationMessage
                {
                    Type = "info",
                    Title = "اعلان آزمایشی",
                    Message = "این یک اعلان آزمایشی از سیستم SignalR است.",
                    Duration = 5000
                };

                await _notificationService.SendToUserAsync(userId, notification);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test notification");
                return StatusCode(500, "خطا در ارسال اعلان آزمایشی");
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetNotificationStats()
        {
            try
            {
                var userId = GetCurrentUserId();
                var notifications = await _notificationService.GetUserNotificationsAsync(userId);
                
                var stats = new
                {
                    Total = notifications.Count,
                    Unread = notifications.Count(n => !n.IsRead),
                    Read = notifications.Count(n => n.IsRead),
                    ByType = notifications.GroupBy(n => n.Type)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    Recent = notifications.Take(5).Select(n => new
                    {
                        n.Id,
                        n.Type,
                        n.Title,
                        n.CreatedAt,
                        n.IsRead
                    })
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get notification stats");
                return StatusCode(500, "خطا در دریافت آمار اعلان‌ها");
            }
        }
    }

    // Request models
    public class SendNotificationRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Persistent { get; set; } = false;
        public int Duration { get; set; } = 5000;
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
    }

    public class SendCompanyNotificationRequest
    {
        public int CompanyId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Persistent { get; set; } = false;
        public int Duration { get; set; } = 5000;
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
    }

    public class SendRoleNotificationRequest
    {
        public string Role { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Persistent { get; set; } = false;
        public int Duration { get; set; } = 5000;
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
    }
}
