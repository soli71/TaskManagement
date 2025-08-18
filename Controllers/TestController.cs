using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagementMvc.Models;
using TaskManagementMvc.Services;

namespace TaskManagementMvc.Controllers
{
    [Authorize]
    public class TestController : Controller
    {
        private readonly IScalableNotificationService _notificationService;

        public TestController(IScalableNotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // GET: Test/NotificationTest
        public IActionResult NotificationTest()
        {
            return View();
        }

        // POST: Test/SendTestNotification
        [HttpPost]
        public async Task<IActionResult> SendTestNotification()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "کاربر احراز هویت نشده است." });
                }

                await _notificationService.SendToUserAsync(
                    userId,
                    new ScalableNotificationMessage
                    {
                        Type = "success",
                        Title = "تست موفق",
                        Message = $"این یک پیام تست است که در {DateTime.Now:HH:mm:ss} ارسال شده.",
                        ActionUrl = Url.Action("NotificationTest"),
                        ActionText = "بازگشت به تست"
                    }
                );

                return Json(new { success = true, message = "پیام تست ارسال شد!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطا: {ex.Message}" });
            }
        }

        // POST: Test/SendMultipleNotifications
        [HttpPost]
        public async Task<IActionResult> SendMultipleNotifications()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "کاربر احراز هویت نشده است." });
                }

                var notifications = new[]
                {
                    new ScalableNotificationMessage
                    {
                        Type = "success",
                        Title = "موفقیت",
                        Message = "عملیات با موفقیت انجام شد.",
                        ActionUrl = "#",
                        ActionText = "مشاهده"
                    },
                    new ScalableNotificationMessage
                    {
                        Type = "warning",
                        Title = "هشدار",
                        Message = "توجه: این یک پیام هشدار است.",
                        ActionUrl = "#",
                        ActionText = "بررسی"
                    },
                    new ScalableNotificationMessage
                    {
                        Type = "error",
                        Title = "خطا",
                        Message = "خطایی در سیستم رخ داده است.",
                        ActionUrl = "#",
                        ActionText = "جزئیات"
                    },
                    new ScalableNotificationMessage
                    {
                        Type = "info",
                        Title = "اطلاعات",
                        Message = "اطلاعات جدیدی برای شما موجود است.",
                        ActionUrl = "#",
                        ActionText = "مطالعه"
                    }
                };

                foreach (var notification in notifications)
                {
                    await _notificationService.SendToUserAsync(userId, notification);
                    await Task.Delay(500); // فاصله بین پیام‌ها
                }

                return Json(new { success = true, message = "همه پیام‌ها ارسال شدند!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطا: {ex.Message}" });
            }
        }
    }
}
