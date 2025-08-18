using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaskManagementMvc.Models;
using TaskManagementMvc.Models.ViewModels;
using TaskManagementMvc.Services;

namespace TaskManagementMvc.Controllers
{
    [Authorize(Policy = Permissions.ManageSystem)]
    public class AdminController : Controller
    {
        private readonly IOptionsSnapshot<NotificationSettings> _notificationSettings;
        private readonly IScalableNotificationService _notificationService;
        private readonly IConfiguration _configuration;

        public AdminController(
            IOptionsSnapshot<NotificationSettings> notificationSettings,
            IScalableNotificationService notificationService,
            IConfiguration configuration)
        {
            _notificationSettings = notificationSettings;
            _notificationService = notificationService;
            _configuration = configuration;
        }

        // GET: Admin/Settings
        public IActionResult Settings()
        {
            var model = new AdminSettingsViewModel
            {
                UseRedis = _notificationSettings.Value.UseRedis,
                RedisTtlDays = _notificationSettings.Value.RedisTtlDays,
                RedisConnectionString = _configuration.GetConnectionString("Redis") ?? "localhost:6379"
            };

            return View(model);
        }

        // POST: Admin/TestNotification
        [HttpPost]
        public async Task<IActionResult> TestNotification()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "کاربر احراز هویت نشده است.";
                    
                    // Send error notification to user
                    try
                    {
                        await _notificationService.SendToUserAsync(
                            userId ?? "system",
                            new ScalableNotificationMessage
                            {
                                Type = "error",
                                Title = "خطا در احراز هویت",
                                Message = "کاربر احراز هویت نشده است.",
                                ActionUrl = Url.Action("Login", "Account"),
                                ActionText = "ورود مجدد"
                            }
                        );
                    }
                    catch { /* Ignore notification errors */ }
                    
                    return RedirectToAction(nameof(Settings));
                }

                await _notificationService.SendToUserAsync(
                    userId,
                    new ScalableNotificationMessage
                    {
                        Type = "info",
                        Title = "آزمایش سیستم اعلان‌رسانی",
                        Message = $"این یک پیام آزمایشی است. Redis فعال: {_notificationSettings.Value.UseRedis}",
                        ActionUrl = Url.Action("Settings"),
                        ActionText = "بازگشت به تنظیمات"
                    }
                );

                TempData["SuccessMessage"] = "پیام آزمایشی با موفقیت ارسال شد.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"خطا در ارسال پیام آزمایشی: {ex.Message}";
                
                // Send error notification to user
                try
                {
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        await _notificationService.SendToUserAsync(
                            userId,
                            new ScalableNotificationMessage
                            {
                                Type = "error",
                                Title = "خطا در آزمایش سیستم اعلان‌رسانی",
                                Message = $"خطا در ارسال پیام آزمایشی: {ex.Message}",
                                ActionUrl = Url.Action("Settings"),
                                ActionText = "بازگشت به تنظیمات"
                            }
                        );
                    }
                }
                catch { /* Ignore notification errors */ }
            }

            return RedirectToAction(nameof(Settings));
        }

        // POST: Admin/TestRedisConnection
        [HttpPost]
        public async Task<IActionResult> TestRedisConnection()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            try
            {
                if (!_notificationSettings.Value.UseRedis)
                {
                    TempData["WarningMessage"] = "Redis غیرفعال است. برای آزمایش ابتدا آن را فعال کنید.";
                    
                    // Send warning notification to user
                    try
                    {
                        if (!string.IsNullOrEmpty(userId))
                        {
                            await _notificationService.SendToUserAsync(
                                userId,
                                new ScalableNotificationMessage
                                {
                                    Type = "warning",
                                    Title = "Redis غیرفعال",
                                    Message = "Redis غیرفعال است. برای آزمایش ابتدا آن را فعال کنید.",
                                    ActionUrl = Url.Action("Settings"),
                                    ActionText = "رفتن به تنظیمات"
                                }
                            );
                        }
                    }
                    catch { /* Ignore notification errors */ }
                    
                    return RedirectToAction(nameof(Settings));
                }

                // Test Redis by getting user notifications
                if (!string.IsNullOrEmpty(userId))
                {
                    var notifications = await _notificationService.GetUserNotificationsAsync(userId, 1);
                    TempData["SuccessMessage"] = $"اتصال Redis موفق است. تعداد اعلان‌های کاربر: {notifications.Count}";
                    
                    // Send success notification to user
                    try
                    {
                        await _notificationService.SendToUserAsync(
                            userId,
                            new ScalableNotificationMessage
                            {
                                Type = "success",
                                Title = "آزمایش Redis موفق",
                                Message = $"اتصال Redis موفق است. تعداد اعلان‌های کاربر: {notifications.Count}",
                                ActionUrl = Url.Action("Settings"),
                                ActionText = "بازگشت به تنظیمات"
                            }
                        );
                    }
                    catch { /* Ignore notification errors */ }
                }
                else
                {
                    TempData["ErrorMessage"] = "کاربر احراز هویت نشده است.";
                    
                    // Send error notification for authentication
                    try
                    {
                        await _notificationService.SendToUserAsync(
                            "system",
                            new ScalableNotificationMessage
                            {
                                Type = "error",
                                Title = "خطا در احراز هویت",
                                Message = "کاربر احراز هویت نشده است.",
                                ActionUrl = Url.Action("Login", "Account"),
                                ActionText = "ورود مجدد"
                            }
                        );
                    }
                    catch { /* Ignore notification errors */ }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"خطا در اتصال به Redis: {ex.Message}";
                
                // Send error notification to user
                try
                {
                    if (!string.IsNullOrEmpty(userId))
                    {
                        await _notificationService.SendToUserAsync(
                            userId,
                            new ScalableNotificationMessage
                            {
                                Type = "error",
                                Title = "خطا در اتصال به Redis",
                                Message = $"خطا در اتصال به Redis: {ex.Message}",
                                ActionUrl = Url.Action("Settings"),
                                ActionText = "بازگشت به تنظیمات"
                            }
                        );
                    }
                }
                catch { /* Ignore notification errors */ }
            }

            return RedirectToAction(nameof(Settings));
        }
    }
}
