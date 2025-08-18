using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace TaskManagementMvc.Services
{
    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class NotificationMessage
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool Persistent { get; set; } = false;
        public int Duration { get; set; } = 5000;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public interface INotificationService
    {
        void AddNotification(NotificationType type, string title, string message, bool persistent = false);
        void AddSuccess(string title, string message = "");
        void AddError(string title, string message = "");
        void AddWarning(string title, string message = "");
        void AddInfo(string title, string message = "");
        void AddAuthError(string message = "");
        void AddValidationError(string message = "");
        void AddPermissionError(string message = "");
        void AddServerError(string message = "");
        List<NotificationMessage> GetNotifications();
        void ClearNotifications();
        string GetNotificationsJson();
    }

    public class NotificationService : INotificationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string NotificationKey = "Notifications";

        public NotificationService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private List<NotificationMessage> GetNotificationsList()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return new List<NotificationMessage>();

            var notificationsJson = session.GetString(NotificationKey);
            if (string.IsNullOrEmpty(notificationsJson))
            {
                return new List<NotificationMessage>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<NotificationMessage>>(notificationsJson) ?? new List<NotificationMessage>();
            }
            catch
            {
                return new List<NotificationMessage>();
            }
        }

        private void SaveNotifications(List<NotificationMessage> notifications)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return;

            var notificationsJson = JsonSerializer.Serialize(notifications);
            session.SetString(NotificationKey, notificationsJson);
        }

        public void AddNotification(NotificationType type, string title, string message, bool persistent = false)
        {
            var notifications = GetNotificationsList();
            
            notifications.Add(new NotificationMessage
            {
                Type = type.ToString().ToLowerInvariant(),
                Title = title,
                Message = message,
                Persistent = persistent,
                Duration = persistent ? 0 : 5000,
                CreatedAt = DateTime.Now
            });

            // Keep only the last 10 notifications
            if (notifications.Count > 10)
            {
                notifications = notifications.TakeLast(10).ToList();
            }

            SaveNotifications(notifications);
        }

        public void AddSuccess(string title, string message = "")
        {
            AddNotification(NotificationType.Success, title, message);
        }

        public void AddError(string title, string message = "")
        {
            AddNotification(NotificationType.Error, title, message, persistent: true);
        }

        public void AddWarning(string title, string message = "")
        {
            AddNotification(NotificationType.Warning, title, message);
        }

        public void AddInfo(string title, string message = "")
        {
            AddNotification(NotificationType.Info, title, message);
        }

        public void AddAuthError(string message = "")
        {
            AddError("خطای احراز هویت", string.IsNullOrEmpty(message) ? "خطای احراز هویت رخ داده است" : message);
        }

        public void AddValidationError(string message = "")
        {
            AddError("خطای اعتبارسنجی", string.IsNullOrEmpty(message) ? "لطفاً اطلاعات وارد شده را بررسی کنید" : message);
        }

        public void AddPermissionError(string message = "")
        {
            AddError("خطای دسترسی", string.IsNullOrEmpty(message) ? "شما مجوز لازم برای این عملیات را ندارید" : message);
        }

        public void AddServerError(string message = "")
        {
            AddError("خطای سرور", string.IsNullOrEmpty(message) ? "خطا در پردازش درخواست" : message);
        }

        public List<NotificationMessage> GetNotifications()
        {
            var notifications = GetNotificationsList();
            ClearNotifications(); // Clear after retrieving
            return notifications;
        }

        public void ClearNotifications()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            session?.Remove(NotificationKey);
        }

        public string GetNotificationsJson()
        {
            var notifications = GetNotifications();
            return JsonSerializer.Serialize(notifications);
        }
    }

    // Extension methods for Controller
    public static class ControllerNotificationExtensions
    {
        public static void NotifySuccess(this Controller controller, string title, string message = "")
        {
            var notificationService = controller.HttpContext.RequestServices.GetService<INotificationService>();
            notificationService?.AddSuccess(title, message);
        }

        public static void NotifyError(this Controller controller, string title, string message = "")
        {
            var notificationService = controller.HttpContext.RequestServices.GetService<INotificationService>();
            notificationService?.AddError(title, message);
        }

        public static void NotifyWarning(this Controller controller, string title, string message = "")
        {
            var notificationService = controller.HttpContext.RequestServices.GetService<INotificationService>();
            notificationService?.AddWarning(title, message);
        }

        public static void NotifyInfo(this Controller controller, string title, string message = "")
        {
            var notificationService = controller.HttpContext.RequestServices.GetService<INotificationService>();
            notificationService?.AddInfo(title, message);
        }

        public static void NotifyAuthError(this Controller controller, string message = "")
        {
            var notificationService = controller.HttpContext.RequestServices.GetService<INotificationService>();
            notificationService?.AddAuthError(message);
        }

        public static void NotifyValidationError(this Controller controller, string message = "")
        {
            var notificationService = controller.HttpContext.RequestServices.GetService<INotificationService>();
            notificationService?.AddValidationError(message);
        }

        public static void NotifyPermissionError(this Controller controller, string message = "")
        {
            var notificationService = controller.HttpContext.RequestServices.GetService<INotificationService>();
            notificationService?.AddPermissionError(message);
        }

        public static void NotifyServerError(this Controller controller, string message = "")
        {
            var notificationService = controller.HttpContext.RequestServices.GetService<INotificationService>();
            notificationService?.AddServerError(message);
        }

        public static IActionResult RedirectWithSuccess(this Controller controller, string action, string title, string message = "")
        {
            controller.NotifySuccess(title, message);
            return controller.RedirectToAction(action);
        }

        public static IActionResult RedirectWithError(this Controller controller, string action, string title, string message = "")
        {
            controller.NotifyError(title, message);
            return controller.RedirectToAction(action);
        }
    }
}
