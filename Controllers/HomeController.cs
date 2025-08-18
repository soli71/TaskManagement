using Microsoft.AspNetCore.Mvc;
using TaskManagementMvc.Services;

namespace TaskManagementMvc.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();

        public IActionResult Notifications() => View();

        public IActionResult TestNotifications()
        {
            this.NotifySuccess("تست موفق", "سیستم اعلان‌ها با موفقیت کار می‌کند!");
            this.NotifyInfo("اطلاعات", "این یک پیام اطلاعاتی تست است.");
            this.NotifyWarning("هشدار", "این یک پیام هشدار تست است.");
            this.NotifyError("خطا", "این یک پیام خطای تست است.");
            
            return RedirectToAction("Notifications");
        }
    }
}
