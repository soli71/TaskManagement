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
        private readonly IRazorViewRenderer _razorRenderer;

        public InvoicesController(TaskManagementContext context, UserManager<ApplicationUser> userManager, IEmailSender email, ITelegramSender telegram, IRazorViewRenderer razorRenderer)
        {
            _context = context;
            _userManager = userManager;
            _email = email;
            _telegram = telegram;
            _razorRenderer = razorRenderer;
        }

        // GET: Invoices
        [Authorize(Policy = Permissions.ViewInvoices)]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<Invoice> invoicesQuery;

            if (User.IsInRole(Roles.SystemAdmin))
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
        [Authorize(Policy = Permissions.CreateInvoices)]
    public async Task<IActionResult> Create(string? taskIds)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole(Roles.SystemAdmin))
            {
                return BadRequest("شما باید به یک شرکت تخصیص داده شده باشید.");
            }

            // Get completed tasks that haven't been invoiced yet
            var tasksQuery = _context.Tasks
                .Include(t => t.Performer)
                .ThenInclude(p => p.Grade)
                .Include(t => t.Project)
                .Where(t => t.Status == TaskStatus.Completed && t.Hours > 0);

            if (!User.IsInRole(Roles.SystemAdmin))
            {
                tasksQuery = tasksQuery.Where(t => t.Project.CompanyId == user.CompanyId);
            }

            var tasks = await tasksQuery.ToListAsync();

            // If specific task IDs are provided, pre-select them
            if (!string.IsNullOrEmpty(taskIds))
            {
                var selectedTaskIds = taskIds.Split(',').Select(int.Parse).ToList();
                ViewBag.SelectedTaskIds = selectedTaskIds;

                // Filter tasks to show only selected ones first
                var selectedTasks = tasks.Where(t => selectedTaskIds.Contains(t.Id)).ToList();
                var otherTasks = tasks.Where(t => !selectedTaskIds.Contains(t.Id)).ToList();
                ViewBag.Tasks = selectedTasks.Concat(otherTasks).ToList();
            }
            else
            {
                ViewBag.Tasks = tasks;
            }

            var vm = new TaskManagementMvc.Models.ViewModels.InvoiceCreateViewModel
            {
                InvoiceDate = DateTime.Now
            };

            vm.Tasks = ((List<TaskItem>)ViewBag.Tasks).Select(t => new TaskManagementMvc.Models.ViewModels.TaskSelectionViewModel
            {
                TaskId = t.Id,
                Title = t.Title,
                Description = t.Description,
                HoursAvailable = t.Hours,
                Selected = ViewBag.SelectedTaskIds != null && ((List<int>)ViewBag.SelectedTaskIds).Contains(t.Id),
                HoursForInvoice = t.Hours,
                PerformerName = t.Performer?.Name,
                StartAt = t.StartAt
            }).ToList();

            return View(vm);
        }

        // POST: Invoices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.CreateInvoices)]
        public async Task<IActionResult> Create(TaskManagementMvc.Models.ViewModels.InvoiceCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole(Roles.SystemAdmin))
            {
                return BadRequest("شما باید به یک شرکت تخصیص داده شده باشید.");
            }
            // Selected tasks & their override hours
            var selectedTasks = model.Tasks?.Where(t => t.Selected && t.HoursForInvoice >= 0).ToList() ?? new List<TaskManagementMvc.Models.ViewModels.TaskSelectionViewModel>();

            // Validation: at least one task selected
            if (!selectedTasks.Any())
            {
                ModelState.AddModelError("Tasks", "حداقل یک تسک باید انتخاب شود.");
            }

            // Fetch selected task entities early for company/invoice number determination
            var selectedTaskIds = selectedTasks.Select(t => t.TaskId).ToList();
            var taskEntitiesForCompany = await _context.Tasks
                .Include(t => t.Project)
                .ThenInclude(p => p.Company)
                .Where(t => selectedTaskIds.Contains(t.Id))
                .ToListAsync();

            int? companyIdForInvoice = null;
            if (User.IsInRole(Roles.SystemAdmin))
            {
                var distinctCompanies = taskEntitiesForCompany.Select(t => t.Project?.CompanyId).Where(cid => cid.HasValue).Distinct().ToList();
                if (distinctCompanies.Count > 1)
                {
                    ModelState.AddModelError("Tasks", "نمی‌توانید تسک‌هایی از چند شرکت مختلف در یک فاکتور قرار دهید.");
                }
                else if (distinctCompanies.Count == 1)
                {
                    companyIdForInvoice = distinctCompanies.First();
                }
            }
            else
            {
                companyIdForInvoice = user.CompanyId; // already validated not null for non-admin
            }

            if (companyIdForInvoice == null)
            {
                ModelState.AddModelError("Tasks", "امکان تعیین شرکت برای فاکتور وجود ندارد.");
            }

            // Prepare invoice (ProjectId intentionally null in new simplified flow)
            var invoice = new Invoice
            {
                InvoiceDate = model.InvoiceDate,
                Description = model.Description,
                CustomerName = model.CustomerName
            };

            // Keep linkage to a project (for company scoping) by using the first selected task's project
            var firstProjectId = taskEntitiesForCompany.Select(t => t.ProjectId).FirstOrDefault(pid => pid.HasValue);
            if (firstProjectId.HasValue)
            {
                invoice.ProjectId = firstProjectId.Value;
            }

            if (companyIdForInvoice.HasValue)
            {
                invoice.InvoiceNumber = await GenerateNextInvoiceNumberForCompany(companyIdForInvoice.Value);
            }
            else
            {
                invoice.InvoiceNumber = $"TMP-{DateTime.UtcNow:yyyyMMddHHmmss}"; // fallback
            }

            // Re-validate after setting InvoiceNumber automatically
            if (!ModelState.IsValid)
            {
                // Repopulate tasks for redisplay
                var tasksList = await GetCompletedTasksForUser();
                var vm = model;
                vm.Tasks = tasksList.Select(t => new TaskManagementMvc.Models.ViewModels.TaskSelectionViewModel
                {
                    TaskId = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    HoursAvailable = t.Hours,
                    Selected = selectedTasks.Any(st => st.TaskId == t.Id),
                    HoursForInvoice = selectedTasks.FirstOrDefault(st => st.TaskId == t.Id)?.HoursForInvoice ?? t.Hours,
                    PerformerName = t.Performer?.Name,
                    StartAt = t.StartAt
                }).ToList();
                return View(vm);
            }

            invoice.CreatedAt = DateTime.Now;
            invoice.CreatedById = user.Id;
            invoice.Status = InvoiceStatus.Draft;

            _context.Add(invoice);
            await _context.SaveChangesAsync();

            // Fetch selected task entities (with performer & grade) for line creation
            var taskEntities = await _context.Tasks
                .Include(t => t.Performer)
                .ThenInclude(p => p.Grade)
                .Where(t => selectedTaskIds.Contains(t.Id))
                .ToListAsync();

            foreach (var task in taskEntities)
            {
                var overrideVm = selectedTasks.First(st => st.TaskId == task.Id);
                double hoursForInvoice = Math.Min(overrideVm.HoursForInvoice, task.Hours); // cap at available
                if (hoursForInvoice < 0) hoursForInvoice = 0;
                var line = new InvoiceLine
                {
                    InvoiceId = invoice.Id,
                    TaskItemId = task.Id,
                    Title = task.Title,
                    PerformerName = task.Performer?.Name,
                    GradeName = task.Performer?.Grade?.Name,
                    HourlyRate = task.Performer?.Grade?.HourlyRate ?? 0,
                    Hours = hoursForInvoice,
                    Amount = (task.Performer?.Grade?.HourlyRate ?? 0) * (decimal)hoursForInvoice
                };
                _context.InvoiceLines.Add(line);

                // Update task status to Invoiced after adding line
                task.Status = TaskStatus.Invoiced;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "فاکتور با موفقیت ایجاد شد.";
            return RedirectToAction(nameof(Details), new { id = invoice.Id });
        }

        // GET: Invoices/Details/5
        [Authorize(Policy = Permissions.ViewInvoices)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            IQueryable<Invoice> invoiceQuery;

            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can see all invoices
                invoiceQuery = _context.Invoices
                    .Include(i => i.Project).ThenInclude(p => p.Company)
                    .Include(i => i.CreatedBy)
                    .Include(i => i.Lines).ThenInclude(l => l.TaskItem)
                    .Include(i => i.EmailLogs).ThenInclude(l => l.SentBy)
                    .Include(i => i.TelegramLogs).ThenInclude(l => l.SentBy);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's invoices
                invoiceQuery = _context.Invoices
                    .Include(i => i.Project).ThenInclude(p => p.Company)
                    .Include(i => i.CreatedBy)
                    .Include(i => i.Lines).ThenInclude(l => l.TaskItem)
                    .Include(i => i.EmailLogs).ThenInclude(l => l.SentBy)
                    .Include(i => i.TelegramLogs).ThenInclude(l => l.SentBy)
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

            // Provide email logs (ordered) for the view (current view expects ViewData["EmailLogs"]).
            ViewData["EmailLogs"] = invoice.EmailLogs
                .OrderByDescending(l => l.SentAt)
                .ToList();

            return View(invoice);
        }

        // GET: Invoices/CompletedUninvoiced
        [Authorize(Policy = Permissions.ViewInvoices)]
        public async Task<IActionResult> CompletedUninvoiced()
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> tasksQuery;

            if (User.IsInRole(Roles.SystemAdmin))
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
        [Authorize(Policy = Permissions.CreateInvoices)]
        public async Task<IActionResult> SendEmail(int id, string customerEmail)
        {
            var inv = await _context.Invoices
                .Include(i => i.Lines)
                    .ThenInclude(l => l.TaskItem)
                        .ThenInclude(t => t.Performer)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (inv is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                inv.CustomerEmail = customerEmail.Trim();
            }

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


        private async Task<(string subject, string body)> GenerateSubjectAndBody(Invoice invoice)
        {
            const string preferredViewKey = "InvoicePerformerTotals"; // Razor view file name without folders
            const string razorPrefix = "RAZOR:";

            // 1) First try to find an active template explicitly pointing to our preferred Razor view
            var template = await _context.EmailTemplates
                .Where(t => t.IsActive && t.Body.StartsWith(razorPrefix))
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync(t => t.Body.Contains(preferredViewKey));

            // 2) Otherwise fall back to any active invoice template (previous logic)
            template ??= await _context.EmailTemplates
                .Where(t => t.IsActive && t.Name.Contains("فاکتور"))
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            // Subject fallback
            var subjectTemplate = template?.Subject ?? $"فاکتور شماره {invoice.InvoiceNumber ?? invoice.Id.ToString()}";
            var subject = ReplaceTemplateVariables(subjectTemplate, invoice);

            string body;
            // HARD PREFERENCE: Always try to render the Razor view (even if DB template body is plain HTML)
            try
            {
                body = await _razorRenderer.RenderViewToStringAsync(this, $"Invoices/EmailTemplates/{preferredViewKey}", invoice);
            }
            catch
            {
                // Fallback to DB template logic only if Razor view not found / rendering failed
                if (template != null && template.Body.StartsWith(razorPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var viewName = template.Body.Substring(razorPrefix.Length).Trim();
                    if (!viewName.Contains('/'))
                    {
                        viewName = $"Invoices/EmailTemplates/{viewName}";
                    }
                    body = await _razorRenderer.RenderViewToStringAsync(this, viewName, invoice);
                }
                else if (template != null)
                {
                    body = ReplaceTemplateVariables(template.Body, invoice);
                }
                else
                {
                    body = GenerateDefaultInvoiceEmail(invoice);
                }
            }

            return (subject, body);
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

            var performerTotals = BuildPerformerTotals(invoice);

            return template
                .Replace("{{InvoiceNumber}}", invoice.InvoiceNumber)
                .Replace("{{TotalAmount}}", invoice.Lines.Sum(l => l.Amount).ToString("C"))
                .Replace("{{IssueDate}}", invoice.InvoiceDate.ToString("yyyy/MM/dd"))
                .Replace("{{TaskList}}", taskList)
                .Replace("{{CustomerName}}", invoice.CustomerName ?? string.Empty)
                .Replace("{{LineCount}}", invoice.Lines.Count.ToString())
                .Replace("{{PerformerTotals}}", performerTotals);
        }

        private string BuildPerformerTotals(Invoice invoice)
        {
            // Group by performer (prefer TaskItem.Performer.FullName, fallback to InvoiceLine.PerformerName)
            var groups = invoice.Lines
                .Select(l => new
                {
                    Line = l,
                    Performer = l.TaskItem?.Performer,
                    Name = l.TaskItem?.Performer?.FullName ?? l.PerformerName ?? "نامشخص"
                })
                .GroupBy(x => x.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    Hours = g.Sum(x => x.Line.Hours),
                    Amount = g.Sum(x => x.Line.Amount),
                    Iban = g.Select(x => x.Performer?.IbanNumber).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty,
                    Card = g.Select(x => x.Performer?.CardNumber).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty
                })
                .OrderByDescending(g => g.Amount)
                .ToList();

            if (!groups.Any()) return string.Empty;

            var sb = new System.Text.StringBuilder();
            sb.Append("<table dir='rtl' cellspacing='0' cellpadding='6' style='border-collapse:collapse;font-family:Tahoma,Arial,sans-serif;font-size:13px;width:100%;margin-top:15px'>");
            sb.Append("<thead><tr style='background:#f2f2f2;text-align:center'>");
            sb.Append("<th style='border:1px solid #ccc'>نام فرد</th>");
            sb.Append("<th style='border:1px solid #ccc'>مجموع ساعات</th>");
            sb.Append("<th style='border:1px solid #ccc'>مبلغ</th>");
            sb.Append("<th style='border:1px solid #ccc'>شماره شبا</th>");
            sb.Append("<th style='border:1px solid #ccc'>شماره کارت</th>");
            sb.Append("</tr></thead><tbody>");
            foreach (var g in groups)
            {
                sb.Append("<tr style='text-align:center'>");
                sb.Append($"<td style='border:1px solid #ccc'>{System.Net.WebUtility.HtmlEncode(g.Name)}</td>");
                sb.Append($"<td style='border:1px solid #ccc'>{g.Hours:0.##}</td>");
                sb.Append($"<td style='border:1px solid #ccc'>{g.Amount:C}</td>");
                sb.Append($"<td style='border:1px solid #ccc'>{System.Net.WebUtility.HtmlEncode(g.Iban)}</td>");
                sb.Append($"<td style='border:1px solid #ccc'>{System.Net.WebUtility.HtmlEncode(g.Card)}</td>");
                sb.Append("</tr>");
            }
            // Total row
            sb.Append("<tr style='font-weight:bold;background:#fafafa;text-align:center'>");
            sb.Append("<td style='border:1px solid #ccc'>جمع کل</td>");
            sb.Append($"<td style='border:1px solid #ccc'>{groups.Sum(g => g.Hours):0.##}</td>");
            sb.Append($"<td style='border:1px solid #ccc'>{groups.Sum(g => g.Amount):C}</td>");
            sb.Append("<td style='border:1px solid #ccc'></td><td style='border:1px solid #ccc'></td>");
            sb.Append("</tr>");
            sb.Append("</tbody></table>");
            return sb.ToString();
        }

        private async Task<string> GenerateNextInvoiceNumberForCompany(int companyId)
        {
            // Fetch existing invoice numbers for this company with the pattern <CompanyId>-NNNNN
            var numbers = await _context.Invoices
                .Include(i => i.Project)
                .Where(i => i.Project.CompanyId == companyId && i.InvoiceNumber.StartsWith(companyId + "-"))
                .Select(i => i.InvoiceNumber)
                .ToListAsync();

            int maxSeq = 0;
            foreach (var n in numbers)
            {
                var parts = n.Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && int.TryParse(parts[1], out var seq))
                {
                    if (seq > maxSeq) maxSeq = seq;
                }
            }
            var next = maxSeq + 1;
            return $"{companyId}-{next:00000}"; // Pattern: <CompanyId>-00001
        }

        private async Task<List<Project>> GetProjectsForUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole(Roles.SystemAdmin))
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

            if (!User.IsInRole(Roles.SystemAdmin))
            {
                tasksQuery = tasksQuery.Where(t => t.Project.CompanyId == user.CompanyId);
            }

            return await tasksQuery.ToListAsync();
        }
    }
}