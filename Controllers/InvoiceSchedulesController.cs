using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;
using TaskManagementMvc.Services;

namespace TaskManagementMvc.Controllers
{
    [Authorize(Policy = Permissions.ManageInvoices)]
    public class InvoiceSchedulesController : Controller
    {
        private readonly TaskManagementContext _context;
        private readonly IInvoiceAutomationService _automationService;
        private readonly ILogger<InvoiceSchedulesController> _logger;

        public InvoiceSchedulesController(TaskManagementContext context, IInvoiceAutomationService automationService, ILogger<InvoiceSchedulesController> logger)
        {
            _context = context;
            _automationService = automationService;
            _logger = logger;
        }

        // GET: InvoiceSchedules
        public async Task<IActionResult> Index()
        {
            var list = await _context.InvoiceSchedules
                .Include(s => s.Company)
                .OrderBy(s => s.CompanyId).ThenBy(s => s.Name)
                .ToListAsync();
            var lastRuns = await _context.InvoiceJobRunLogs
                .OrderByDescending(r => r.RunStartedAt)
                .Take(100)
                .Include(r => r.Schedule)
                .Include(r => r.Invoice)
                .ToListAsync();
            ViewBag.RecentRuns = lastRuns;
            return View(list);
        }

        // GET: InvoiceSchedules/Create
        public IActionResult Create()
        {
            PopulateCompanies();
            return View(new InvoiceSchedule { IsActive = true, HourOfDay = 6, PeriodType = InvoiceSchedulePeriodType.Daily });
        }

        // POST: InvoiceSchedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceSchedule schedule)
        {
            ValidateSchedule(schedule);
            if (ModelState.IsValid)
            {
                schedule.NextRunAt = ComputeInitialNextRun(schedule);
                _context.Add(schedule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "زمان‌بندی ایجاد شد.";
                return RedirectToAction(nameof(Index));
            }
            PopulateCompanies();
            return View(schedule);
        }

        // GET: InvoiceSchedules/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var schedule = await _context.InvoiceSchedules.FindAsync(id);
            if (schedule == null) return NotFound();
            PopulateCompanies();
            return View(schedule);
        }

        // POST: InvoiceSchedules/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InvoiceSchedule schedule)
        {
            if (id != schedule.Id) return BadRequest();
            ValidateSchedule(schedule);
            if (ModelState.IsValid)
            {
                try
                {
                    var db = await _context.InvoiceSchedules.FirstAsync(s => s.Id == id);
                    db.Name = schedule.Name;
                    db.CompanyId = schedule.CompanyId;
                    db.PeriodType = schedule.PeriodType;
                    db.DayOfWeek = schedule.DayOfWeek;
                    db.DayOfMonth = schedule.DayOfMonth;
                    db.HourOfDay = schedule.HourOfDay;
                    db.RecipientEmails = schedule.RecipientEmails;
                    db.IsActive = schedule.IsActive;
                    db.Description = schedule.Description;
                    if (!db.NextRunAt.HasValue || Request.Form["recomputeNext"] == "on")
                    {
                        db.NextRunAt = ComputeInitialNextRun(db);
                    }
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "زمان‌بندی بروزرسانی شد.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Edit schedule failed");
                    ModelState.AddModelError(string.Empty, "خطا در ذخیره تغییرات");
                }
            }
            PopulateCompanies();
            return View(schedule);
        }

        // GET: InvoiceSchedules/Details/5
        public async Task<IActionResult> Details(int id)
        {
            // EF Core does not support ordering inside Include; load then order in view
            var schedule = await _context.InvoiceSchedules
                .Include(s => s.Company)
                .Include(s => s.RunLogs)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (schedule == null) return NotFound();
            return View(schedule);
        }

        // GET: InvoiceSchedules/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var schedule = await _context.InvoiceSchedules
                .Include(s => s.Company)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (schedule == null) return NotFound();
            return View(schedule);
        }

        // POST: InvoiceSchedules/Delete/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.InvoiceSchedules.FindAsync(id);
            if (schedule == null) return NotFound();
            _context.InvoiceSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "حذف شد";
            return RedirectToAction(nameof(Index));
        }

        // POST: InvoiceSchedules/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var schedule = await _context.InvoiceSchedules.FindAsync(id);
            if (schedule == null) return NotFound();
            schedule.IsActive = !schedule.IsActive;
            if (schedule.IsActive && !schedule.NextRunAt.HasValue)
            {
                schedule.NextRunAt = ComputeInitialNextRun(schedule);
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = schedule.IsActive ? "فعال شد" : "غیرفعال شد";
            return RedirectToAction(nameof(Index));
        }

        // POST: InvoiceSchedules/Trigger/5 (manual run)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Trigger(int id)
        {
            var schedule = await _context.InvoiceSchedules.FindAsync(id);
            if (schedule == null) return NotFound();
            if (!schedule.IsActive)
            {
                TempData["ErrorMessage"] = "زمان‌بندی غیرفعال است.";
                return RedirectToAction(nameof(Index));
            }
            // Force due
            schedule.NextRunAt = DateTime.UtcNow.AddSeconds(-5);
            await _context.SaveChangesAsync();
            var processed = await _automationService.ProcessDueSchedulesAsync();
            TempData[processed > 0 ? "SuccessMessage" : "InfoMessage"] = processed > 0 ? "اجرا شد" : "موردی برای اجرا نبود";
            return RedirectToAction(nameof(Index));
        }

        private void PopulateCompanies()
        {
            ViewBag.Companies = _context.Companies.OrderBy(c => c.Name).ToList();
        }

        private void ValidateSchedule(InvoiceSchedule schedule)
        {
            if (schedule.PeriodType == InvoiceSchedulePeriodType.Weekly && schedule.DayOfWeek == null)
            {
                ModelState.AddModelError(nameof(schedule.DayOfWeek), "روز هفته را انتخاب کنید");
            }
            if (schedule.PeriodType == InvoiceSchedulePeriodType.Monthly && schedule.DayOfMonth == null)
            {
                ModelState.AddModelError(nameof(schedule.DayOfMonth), "روز ماه را وارد کنید");
            }
            if (schedule.HourOfDay < 0 || schedule.HourOfDay > 23)
            {
                ModelState.AddModelError(nameof(schedule.HourOfDay), "ساعت بین ۰ تا ۲۳");
            }
        }

        private DateTime? ComputeInitialNextRun(InvoiceSchedule s)
        {
            var nowLocal = DateTime.Now;
            DateTime candidate;
            switch (s.PeriodType)
            {
                case InvoiceSchedulePeriodType.Daily:
                    candidate = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, s.HourOfDay, 0, 0);
                    if (candidate <= nowLocal) candidate = candidate.AddDays(1);
                    break;
                case InvoiceSchedulePeriodType.Weekly:
                    var dow = s.DayOfWeek ?? DayOfWeek.Saturday;
                    candidate = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, s.HourOfDay, 0, 0);
                    int diff = ((int)dow - (int)candidate.DayOfWeek + 7) % 7;
                    if (diff == 0 && candidate <= nowLocal) diff = 7;
                    candidate = candidate.AddDays(diff);
                    break;
                case InvoiceSchedulePeriodType.Monthly:
                    int dom = s.DayOfMonth ?? 1;
                    var daysInMonth = DateTime.DaysInMonth(nowLocal.Year, nowLocal.Month);
                    dom = Math.Min(dom, daysInMonth);
                    candidate = new DateTime(nowLocal.Year, nowLocal.Month, dom, s.HourOfDay, 0, 0);
                    if (candidate <= nowLocal)
                    {
                        var nextMonth = nowLocal.AddMonths(1);
                        daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                        dom = Math.Min(s.DayOfMonth ?? 1, daysInMonth);
                        candidate = new DateTime(nextMonth.Year, nextMonth.Month, dom, s.HourOfDay, 0, 0);
                    }
                    break;
                default:
                    return null;
            }
            return candidate.ToUniversalTime();
        }
    }
}
