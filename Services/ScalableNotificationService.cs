using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using StackExchange.Redis;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Services
{
    // SignalR Hub برای real-time notifications
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinUserGroup()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogInformation($"User {userId} joined notification group");
            }
        }

        public async Task LeaveUserGroup()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogInformation($"User {userId} left notification group");
            }
        }

        public override async Task OnConnectedAsync()
        {
            await JoinUserGroup();
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await LeaveUserGroup();
            await base.OnDisconnectedAsync(exception);
        }
    }

    // Interface برای notification service قابل scale
    public interface IScalableNotificationService
    {
        Task SendToUserAsync(string userId, ScalableNotificationMessage notification);
        Task SendToUsersAsync(IEnumerable<string> userIds, ScalableNotificationMessage notification);
        Task SendToCompanyAsync(int companyId, ScalableNotificationMessage notification);
        Task SendToRoleAsync(string role, ScalableNotificationMessage notification);
        Task AddToQueueAsync(string userId, ScalableNotificationMessage notification);
        Task<List<ScalableNotificationMessage>> GetUserNotificationsAsync(string userId, int limit = 50);
        Task MarkAsReadAsync(string userId, string notificationId);
        Task ClearUserNotificationsAsync(string userId);
    }

    // پیاده‌سازی قابل scale با Redis و SignalR
    public class ScalableNotificationService : IScalableNotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IConnectionMultiplexer? _redis;
        private readonly IDatabase? _database;
        private readonly ILogger<ScalableNotificationService> _logger;
        private readonly TaskManagementContext _context;
        private readonly NotificationSettings _notificationSettings;

        private const string NOTIFICATION_PREFIX = "notifications:user:";
        private const string NOTIFICATION_COUNTER = "notifications:counter";
        private const int MAX_NOTIFICATIONS_PER_USER = 100;

        public ScalableNotificationService(
            IHubContext<NotificationHub> hubContext,
            IConnectionMultiplexer? redis,
            ILogger<ScalableNotificationService> logger,
            TaskManagementContext context,
            IOptions<NotificationSettings> notificationSettings)
        {
            _hubContext = hubContext;
            _redis = redis;
            _database = redis?.GetDatabase();
            _logger = logger;
            _context = context;
            _notificationSettings = notificationSettings.Value;
        }

        public async Task SendToUserAsync(string userId, ScalableNotificationMessage notification)
        {
            try
            {
                // اضافه کردن ID منحصر به فرد
                notification.Id = await GenerateNotificationIdAsync();
                
                // ذخیره در Redis (اگر فعال باشد)
                if (_notificationSettings.UseRedis)
                {
                    await AddToQueueAsync(userId, notification);
                }

                // ارسال real-time از طریق SignalR
                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("ReceiveNotification", notification);

                _logger.LogInformation($"Notification sent to user {userId}: {notification.Title} (Redis: {_notificationSettings.UseRedis})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification to user {userId}");
                throw;
            }
        }

        public async Task SendToUsersAsync(IEnumerable<string> userIds, ScalableNotificationMessage notification)
        {
            var tasks = userIds.Select(userId => SendToUserAsync(userId, notification));
            await Task.WhenAll(tasks);
        }

        public async Task SendToCompanyAsync(int companyId, ScalableNotificationMessage notification)
        {
            try
            {
                // پیدا کردن کاربران شرکت از دیتابیس
                var userIds = await _context.Users
                    .Where(u => u.CompanyId == companyId && u.IsActive)
                    .Select(u => u.Id.ToString())
                    .ToListAsync();

                await SendToUsersAsync(userIds, notification);

                _logger.LogInformation($"Notification sent to company {companyId}, {userIds.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification to company {companyId}");
                throw;
            }
        }

        public async Task SendToRoleAsync(string role, ScalableNotificationMessage notification)
        {
            try
            {
                // پیدا کردن کاربران با نقش خاص
                var userIds = await _context.UserRoles
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur, r })
                    .Where(x => x.r.Name == role)
                    .Select(x => x.ur.UserId.ToString())
                    .ToListAsync();

                await SendToUsersAsync(userIds, notification);

                _logger.LogInformation($"Notification sent to role {role}, {userIds.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification to role {role}");
                throw;
            }
        }

        public async Task AddToQueueAsync(string userId, ScalableNotificationMessage notification)
        {
            if (!_notificationSettings.UseRedis || _database == null)
            {
                _logger.LogDebug($"Redis is disabled, skipping notification queue for user {userId}");
                return;
            }

            try
            {
                var key = NOTIFICATION_PREFIX + userId;
                var notificationJson = JsonSerializer.Serialize(notification);

                // اضافه کردن به لیست Redis (FIFO)
                await _database.ListLeftPushAsync(key, notificationJson);

                // نگه داشتن فقط آخرین 100 اعلان
                await _database.ListTrimAsync(key, 0, MAX_NOTIFICATIONS_PER_USER - 1);

                // تنظیم expire برای کلید
                await _database.KeyExpireAsync(key, TimeSpan.FromDays(_notificationSettings.RedisTtlDays));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to add notification to queue for user {userId}");
                throw;
            }
        }

        public async Task<List<ScalableNotificationMessage>> GetUserNotificationsAsync(string userId, int limit = 50)
        {
            if (!_notificationSettings.UseRedis || _database == null)
            {
                _logger.LogDebug($"Redis is disabled, returning empty notifications for user {userId}");
                return new List<ScalableNotificationMessage>();
            }

            try
            {
                var key = NOTIFICATION_PREFIX + userId;
                var notifications = new List<ScalableNotificationMessage>();

                var items = await _database.ListRangeAsync(key, 0, limit - 1);
                
                foreach (var item in items)
                {
                    try
                    {
                        var notification = JsonSerializer.Deserialize<ScalableNotificationMessage>(item);
                        if (notification != null)
                        {
                            notifications.Add(notification);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize notification: {Item}", item);
                    }
                }

                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get notifications for user {userId}");
                return new List<ScalableNotificationMessage>();
            }
        }

        public async Task MarkAsReadAsync(string userId, string notificationId)
        {
            if (!_notificationSettings.UseRedis || _database == null)
            {
                _logger.LogDebug($"Redis is disabled, skipping mark as read for user {userId}");
                return;
            }

            try
            {
                var notifications = await GetUserNotificationsAsync(userId);
                var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
                
                if (notification != null)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;

                    // به‌روزرسانی در Redis
                    var key = NOTIFICATION_PREFIX + userId;
                    var updatedNotifications = notifications.Select(n => JsonSerializer.Serialize(n)).ToArray();
                    
                    await _database.KeyDeleteAsync(key);
                    if (updatedNotifications.Length > 0)
                    {
                        await _database.ListRightPushAsync(key, updatedNotifications.Select(x => (StackExchange.Redis.RedisValue)x).ToArray());
                        await _database.KeyExpireAsync(key, TimeSpan.FromDays(_notificationSettings.RedisTtlDays));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to mark notification as read for user {userId}");
            }
        }

        public async Task ClearUserNotificationsAsync(string userId)
        {
            if (!_notificationSettings.UseRedis || _database == null)
            {
                _logger.LogDebug($"Redis is disabled, skipping clear notifications for user {userId}");
                return;
            }

            try
            {
                var key = NOTIFICATION_PREFIX + userId;
                await _database.KeyDeleteAsync(key);
                
                _logger.LogInformation($"Cleared all notifications for user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to clear notifications for user {userId}");
            }
        }

        private async Task<string> GenerateNotificationIdAsync()
        {
            if (_notificationSettings.UseRedis && _database != null)
            {
                var counter = await _database.StringIncrementAsync(NOTIFICATION_COUNTER);
                return $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{counter}";
            }
            else
            {
                // Fallback اگر Redis فعال نباشد
                return $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Guid.NewGuid().ToString("N")[..8]}";
            }
        }
    }

    // مدل اعلان بهبود یافته برای سیستم قابل scale  
    public class ScalableNotificationMessage
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Persistent { get; set; } = false;
        public int Duration { get; set; } = 5000;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    // Extension methods برای کنترلرها
    public static class ScalableNotificationExtensions
    {
        public static async Task NotifyUserAsync(this Controller controller, string userId, NotificationType type, string title, string message = "", bool persistent = false)
        {
            var notificationService = controller.HttpContext.RequestServices.GetService<IScalableNotificationService>();
            if (notificationService != null)
            {
                var notification = new ScalableNotificationMessage
                {
                    Type = type.ToString().ToLowerInvariant(),
                    Title = title,
                    Message = message,
                    Persistent = persistent,
                    Duration = persistent ? 0 : 5000
                };

                await notificationService.SendToUserAsync(userId, notification);
            }
        }

        public static async Task NotifyUserAsync(this Controller controller, string userId, string title, string message, NotificationType type, string actionUrl = "", string actionText = "")
        {
            var notificationService = controller.HttpContext.RequestServices.GetService<IScalableNotificationService>();
            if (notificationService != null)
            {
                var notification = new ScalableNotificationMessage
                {
                    Type = type.ToString().ToLowerInvariant(),
                    Title = title,
                    Message = message,
                    ActionUrl = actionUrl,
                    ActionText = actionText,
                    Duration = 5000
                };

                await notificationService.SendToUserAsync(userId, notification);
            }
        }

        public static async Task NotifyUsersAsync(this Controller controller, List<string> userIds, string title, string message, NotificationType type, string actionUrl = "", string actionText = "")
        {
            var notificationService = controller.HttpContext.RequestServices.GetService<IScalableNotificationService>();
            if (notificationService != null)
            {
                var notification = new ScalableNotificationMessage
                {
                    Type = type.ToString().ToLowerInvariant(),
                    Title = title,
                    Message = message,
                    ActionUrl = actionUrl,
                    ActionText = actionText,
                    Duration = 5000
                };

                await notificationService.SendToUsersAsync(userIds, notification);
            }
        }

        public static async Task NotifyCurrentUserAsync(this Controller controller, NotificationType type, string title, string message = "", bool persistent = false)
        {
            var userId = controller.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await controller.NotifyUserAsync(userId, type, title, message, persistent);
            }
        }

        public static async Task NotifyCurrentUserAsync(this Controller controller, string title, string message, NotificationType type, string actionUrl = "", string actionText = "")
        {
            var userId = controller.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await controller.NotifyUserAsync(userId, title, message, type, actionUrl, actionText);
            }
        }

        public static async Task NotifyCompanyAsync(this Controller controller, int companyId, NotificationType type, string title, string message = "")
        {
            var notificationService = controller.HttpContext.RequestServices.GetService<IScalableNotificationService>();
            if (notificationService != null)
            {
                var notification = new ScalableNotificationMessage
                {
                    Type = type.ToString().ToLowerInvariant(),
                    Title = title,
                    Message = message,
                    Duration = 5000
                };

                await notificationService.SendToCompanyAsync(companyId, notification);
            }
        }

        public static async Task NotifyRoleAsync(this Controller controller, string role, NotificationType type, string title, string message = "")
        {
            var notificationService = controller.HttpContext.RequestServices.GetService<IScalableNotificationService>();
            if (notificationService != null)
            {
                var notification = new ScalableNotificationMessage
                {
                    Type = type.ToString().ToLowerInvariant(),
                    Title = title,
                    Message = message,
                    Duration = 5000
                };

                await notificationService.SendToRoleAsync(role, notification);
            }
        }

        // Error notification methods using ScalableNotificationService
        public static async Task NotifyAuthErrorAsync(this Controller controller, string message = "خطای احراز هویت")
        {
            await controller.NotifyCurrentUserAsync("خطای احراز هویت", message, NotificationType.Error);
        }

        public static async Task NotifyValidationErrorAsync(this Controller controller, string message = "خطای اعتبارسنجی")
        {
            await controller.NotifyCurrentUserAsync("خطای اعتبارسنجی", message, NotificationType.Error);
        }

        public static async Task NotifyPermissionErrorAsync(this Controller controller, string message = "عدم دسترسی")
        {
            await controller.NotifyCurrentUserAsync("عدم دسترسی", message, NotificationType.Warning);
        }

        public static async Task NotifyServerErrorAsync(this Controller controller, string message = "خطای سرور")
        {
            await controller.NotifyCurrentUserAsync("خطای سرور", message, NotificationType.Error);
        }
    }
}
