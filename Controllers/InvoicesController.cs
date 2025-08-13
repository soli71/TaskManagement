using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;
using TaskManagementMvc.Services;
using TaskStatus = TaskManagementMvc.Models.TaskStatus;

namespace TaskManagementMvc.Controllers
{
    [Authorize]
    public class InvoicesController : Controller
    {
        private readonly TaskManagementContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _email;
        private readonly ITelegramSender _telegram;

        public InvoicesController(TaskManagementContext context, UserManager<ApplicationUser> userManager, IEmailSender email, ITelegramSender telegram)
        {
            _context = context;
            _userManager = userManager;
            _email = email;
            _telegram = telegram;
        }

        // GET: Invoices
        [Authorize(Policy = "Invoices.View")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<Invoice> invoicesQuery;

            if (User.IsInRole("Admin"))
            {
                // Admin can see all invoices
                invoicesQuery = _context.Invoices
                    .Include(i => i.Project)
                    .ThenInclude(p => p.Company)
                    .Include(i => i.CreatedBy)
                    .Include(i => i.Lines);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's invoices
                invoicesQuery = _context.Invoices
                    .Include(i => i.Project)
                    .ThenInclude(p => p.Company)
                    .Include(i => i.CreatedBy)
                    .Include(i => i.Lines)
                    .Where(i => i.Project.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any invoices
                invoicesQuery = _context.Invoices.Where(i => false);
            }

            var invoices = await invoicesQuery
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(invoices);
        }

        // GET: Invoices/Create
        [Authorize(Policy = "Invoices.Create")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole("Admin"))
            {
                return Forbid("شما باید به یک شرکت تخصیص داده شده باشید.");
            }

            // Get completed tasks that haven't been invoiced yet
            var tasksQuery = _context.Tasks
                .Include(t => t.Performer)
                .ThenInclude(p => p.Grade)
                .Include(t => t.Project)
                .Where(t => t.Status == TaskStatus.Completed && t.Hours > 0);

            if (!User.IsInRole("Admin"))
            {
                tasksQuery = tasksQuery.Where(t => t.Project.CompanyId == user.CompanyId);
            }

            var tasks = await tasksQuery.ToListAsync();

            ViewBag.Tasks = tasks;
            ViewBag.Projects = await GetProjectsForUser();
            return View();
        }

        // POST: Invoices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Invoices.Create")]
        public async Task<IActionResult> Create([Bind("InvoiceNumber,InvoiceDate,DueDate,Description,CustomerEmail,CustomerName,CustomerAddress,ProjectId")] Invoice invoice, List<int> taskIds)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole("Admin"))
            {
                return Forbid("شما باید به یک شرکت تخصیص داده شده باشید.");
            }

            if (ModelState.IsValid)
            {
                // If user is not admin, ensure the project belongs to user's company
                if (!User.IsInRole("Admin") && invoice.ProjectId.HasValue)
                {
                    var project = await _context.Projects.FindAsync(invoice.ProjectId.Value);
                    if (project?.CompanyId != user.CompanyId)
                    {
                        ModelState.AddModelError("ProjectId", "شما فقط می‌توانید فاکتورهایی برای پروژه‌های شرکت خود ایجاد کنید.");
                        ViewBag.Tasks = await GetCompletedTasksForUser();
                        ViewBag.Projects = await GetProjectsForUser();
                        return View(invoice);
                    }
                }

                invoice.CreatedAt = DateTime.Now;
                invoice.CreatedById = user.Id;
                invoice.Status = InvoiceStatus.Draft;

                _context.Add(invoice);
                await _context.SaveChangesAsync();

                // Create invoice lines for selected tasks
                if (taskIds != null && taskIds.Any())
                {
                    var tasks = await _context.Tasks
                        .Include(t => t.Performer)
                        .ThenInclude(p => p.Grade)
                        .Where(t => taskIds.Contains(t.Id))
                        .ToListAsync();

                    foreach (var task in tasks)
                    {
                        var line = new InvoiceLine
                        {
                            InvoiceId = invoice.Id,
                            TaskItemId = task.Id,
                            Title = task.Title,
                            PerformerName = task.Performer?.Name,
                            GradeName = task.Performer?.Grade?.Name,
                            HourlyRate = task.Performer?.Grade?.HourlyRate ?? 0,
                            Hours = task.Hours,
                            Amount = (task.Performer?.Grade?.HourlyRate ?? 0) * (decimal)task.Hours
                        };
                        _context.InvoiceLines.Add(line);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "فاکتور با موفقیت ایجاد شد.";
                return RedirectToAction(nameof(Details), new { id = invoice.Id });
            }

            ViewBag.Tasks = await GetCompletedTasksForUser();
            ViewBag.Projects = await GetProjectsForUser();
            return View(invoice);
        }

        // GET: Invoices/Details/5
        [Authorize(Policy = "Invoices.View")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            IQueryable<Invoice> invoiceQuery;

            if (User.IsInRole("Admin"))
            {
                // Admin can see all invoices
                invoiceQuery = _context.Invoices
                    .Include(i => i.Project)
                    .ThenInclude(p => p.Company)
                    .Include(i => i.CreatedBy)
                    .Include(i => i.Lines)
                    .ThenInclude(l => l.TaskItem)
                    .Include(i => i.EmailLogs)
                    .Include(i => i.TelegramLogs);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's invoices
                invoiceQuery = _context.Invoices
                    .Include(i => i.Project)
                    .ThenInclude(p => p.Company)
                    .Include(i => i.CreatedBy)
                    .Include(i => i.Lines)
                    .ThenInclude(l => l.TaskItem)
                    .Include(i => i.EmailLogs)
                    .Include(i => i.TelegramLogs)
                    .Where(i => i.Project.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any invoices
                invoiceQuery = _context.Invoices.Where(i => false);
            }

            var invoice = await invoiceQuery.FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // GET: Invoices/CompletedUninvoiced
        [Authorize(Policy = "Invoices.View")]
        public async Task<IActionResult> CompletedUninvoiced()
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> tasksQuery;

            if (User.IsInRole("Admin"))
            {
                // Admin can see all completed tasks
                tasksQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Where(t => t.Status == TaskStatus.Completed && t.Hours > 0);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's completed tasks
                tasksQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Where(t => t.Status == TaskStatus.Completed && t.Hours > 0 && t.Project.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any tasks
                tasksQuery = _context.Tasks.Where(t => false);
            }

            var tasks = await tasksQuery
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tasks);
        }

        [HttpPost]
        [Authorize(Policy = "Invoices.Create")]
        public async Task<IActionResult> SendEmail(int id)
        {
            var inv = await _context.Invoices
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (inv is null) return NotFound();

            var (subject, body) = await GenerateSubjectAndBody(inv);

            var success = false;
            string? error = null;
            try
            {
                await _email.SendAsync(inv.CustomerEmail ?? string.Empty, subject, body);
                inv.EmailSentAt = DateTime.UtcNow;
                success = true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            _context.InvoiceEmailLogs.Add(new InvoiceEmailLog
            {
                InvoiceId = inv.Id,
                SentAt = DateTime.UtcNow,
                ToEmail = inv.CustomerEmail ?? string.Empty,
                Subject = subject,
                Body = body,
                IsSuccess = success,
                Error = error,
                SentById = int.TryParse(_userManager.GetUserId(User), out var uid) ? uid : (int?)null
            });
            await _context.SaveChangesAsync();

            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "ایمیل فاکتور ارسال شد." : ($"ارسال ایمیل失败: {error}");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Policy = "Invoices.Create")]
        public async Task<IActionResult> SendTelegram(int id, string chatId)
        {
            var inv = await _context.Invoices
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (inv is null) return NotFound();

            var message = BuildTelegramMessage(inv);

            var success = false;
            string? error = null;
            try
            {
                success = await _telegram.SendMessageAsync(chatId, message);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            _context.InvoiceTelegramLogs.Add(new InvoiceTelegramLog
            {
                InvoiceId = inv.Id,
                ChatId = chatId,
                MessageText = message,
                SentAt = DateTime.UtcNow,
                IsSuccess = success,
                Error = error,
                SentById = int.TryParse(_userManager.GetUserId(User), out var uid) ? uid : (int?)null
            });
            await _context.SaveChangesAsync();

            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "ارسال به تلگرام انجام شد." : ($"ارسال تلگرام失败: {error}");
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<(string subject, string body)> GenerateSubjectAndBody(Invoice invoice)
        {
            var template = await _context.EmailTemplates
                .Where(t => t.IsActive && t.Name.Contains("Invoice"))
                .FirstOrDefaultAsync();

            if (template == null)
            {
                var subject = $"فاکتور شماره {invoice.Id}";
                var body = GenerateDefaultInvoiceEmail(invoice);
                return (subject, body);
            }
            else
            {
                var subject = ReplaceTemplateVariables(template.Subject, invoice);
                var body = ReplaceTemplateVariables(template.Body, invoice);
                return (subject, body);
            }
        }

        private string BuildTelegramMessage(Invoice invoice)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>فاکتور شماره {invoice.Id}</b>");
            sb.AppendLine($"تاریخ: {invoice.InvoiceDate:yyyy/MM/dd}");
            sb.AppendLine($"مبلغ کل: {invoice.Lines.Sum(l => l.Amount):C}");
            sb.AppendLine("\n<b>جزئیات:</b>");
            foreach (var l in invoice.Lines)
            {
                sb.AppendLine($"• {l.Title} — {l.Hours} ساعت × {l.HourlyRate:C} = {l.Amount:C}");
            }
            return sb.ToString();
        }

        private string GenerateDefaultInvoiceEmail(Invoice invoice)
        {
            var taskList = string.Join("<br/>", invoice.Lines.Select(l =>
                $"- {l.Title}: {l.Hours} ساعت × {l.HourlyRate:C} = {l.Amount:C}"));

            return $@"
                <div dir='rtl' style='font-family: Tahoma, Arial, sans-serif;'>
                    <h2>فاکتور شماره {invoice.Id}</h2>
                    <p>تاریخ صدور: {invoice.InvoiceDate:yyyy/MM/dd}</p>
                    <p>مبلغ کل: <strong>{invoice.Lines.Sum(l => l.Amount):C}</strong></p>

                    <h3>جزئیات تسک‌ها:</h3>
                    {taskList}

                    <p>با تشکر</p>
                </div>";
        }

        private string ReplaceTemplateVariables(string template, Invoice invoice)
        {
            var taskList = string.Join("<br/>", invoice.Lines.Select(l =>
                $"- {l.Title}: {l.Hours} ساعت × {l.HourlyRate:C} = {l.Amount:C}"));

            return template
                .Replace("{{InvoiceNumber}}", invoice.InvoiceNumber)
                .Replace("{{TotalAmount}}", invoice.Lines.Sum(l => l.Amount).ToString("C"))
                .Replace("{{IssueDate}}", invoice.InvoiceDate.ToString("yyyy/MM/dd"))
                .Replace("{{TaskList}}", taskList)
                .Replace("{{CustomerName}}", invoice.CustomerName ?? string.Empty)
                .Replace("{{LineCount}}", invoice.Lines.Count.ToString());
        }

        private async Task<List<Project>> GetProjectsForUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Admin"))
            {
                return await _context.Projects.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            }
            else if (user?.CompanyId != null)
            {
                return await _context.Projects.Where(p => p.CompanyId == user.CompanyId && p.IsActive).OrderBy(p => p.Name).ToListAsync();
            }
            return new List<Project>();
        }

        private async Task<List<TaskItem>> GetCompletedTasksForUser()
        {
            var user = await _userManager.GetUserAsync(User);
            var tasksQuery = _context.Tasks
                .Include(t => t.Performer)
                .ThenInclude(p => p.Grade)
                .Include(t => t.Project)
                .Where(t => t.Status == TaskStatus.Completed && t.Hours > 0);

            if (!User.IsInRole("Admin"))
            {
                tasksQuery = tasksQuery.Where(t => t.Project.CompanyId == user.CompanyId);
            }

            return await tasksQuery.ToListAsync();
        }
    }
}