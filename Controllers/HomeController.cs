using Microsoft.AspNetCore.Mvc;

namespace TaskManagementMvc.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View();
    }
}
