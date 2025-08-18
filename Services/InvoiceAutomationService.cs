using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;
using TaskStatus = TaskManagementMvc.Models.TaskStatus;

namespace TaskManagementMvc.Services
{
    public class InvoiceAutomationService : IInvoiceAutomationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InvoiceAutomationService> _logger;

        public InvoiceAutomationService(IServiceProvider serviceProvider, ILogger<InvoiceAutomationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<int> ProcessDueSchedulesAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TaskManagementContext>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var razor = scope.ServiceProvider.GetRequiredService<IRazorViewRenderer>();

            if (!await context.Database.CanConnectAsync(cancellationToken)) return 0;
            try
            {
                _ = await context.InvoiceSchedules.Take(1).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "InvoiceSchedules table not ready yet");
                return 0;
            }

            var nowUtc = DateTime.UtcNow;
            var due = await context.InvoiceSchedules
                .Where(s => s.IsActive && s.NextRunAt != null && s.NextRunAt <= nowUtc)
                .ToListAsync(cancellationToken);

            if (!due.Any()) return 0;

            int processed = 0;
            foreach (var schedule in due)
            {
                if (cancellationToken.IsCancellationRequested) break;
                processed++;
                var log = new InvoiceJobRunLog { ScheduleId = schedule.Id, RunStartedAt = DateTime.UtcNow };
                context.InvoiceJobRunLogs.Add(log);
                try
                {
                    var invoice = await GenerateInvoiceForScheduleAsync(context, schedule, razor, emailSender, cancellationToken);
                    log.InvoiceId = invoice?.Id;
                    log.TasksCount = invoice?.Lines.Count ?? 0;
                    log.IsSuccess = true;
                }
                catch (Exception ex)
                {
                    log.IsSuccess = false;
                    log.Error = ex.Message;
                    _logger.LogError(ex, "Failed processing invoice schedule {Id}", schedule.Id);
                }
                finally
                {
                    log.RunCompletedAt = DateTime.UtcNow;
                    schedule.LastRunAt = log.RunCompletedAt;
                    schedule.NextRunAt = ComputeNextRun(schedule, schedule.LastRunAt!.Value);
                }
                await context.SaveChangesAsync(cancellationToken);
            }
            return processed;
        }

        private DateTime? ComputeNextRun(InvoiceSchedule schedule, DateTime fromUtc)
        {
            var baseDate = fromUtc.ToLocalTime().Date;
            DateTime nextLocal;
            switch (schedule.PeriodType)
            {
                case InvoiceSchedulePeriodType.Daily:
                    nextLocal = baseDate.AddDays(1).AddHours(schedule.HourOfDay);
                    break;
                case InvoiceSchedulePeriodType.Weekly:
                    var targetDow = schedule.DayOfWeek ?? DayOfWeek.Saturday;
                    int daysUntil = ((int)targetDow - (int)baseDate.DayOfWeek + 7) % 7;
                    if (daysUntil == 0) daysUntil = 7;
                    nextLocal = baseDate.AddDays(daysUntil).AddHours(schedule.HourOfDay);
                    break;
                case InvoiceSchedulePeriodType.Monthly:
                    int dom = schedule.DayOfMonth ?? 1;
                    var nextMonth = baseDate.AddMonths(1);
                    var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                    dom = Math.Min(dom, daysInMonth);
                    nextLocal = new DateTime(nextMonth.Year, nextMonth.Month, dom, schedule.HourOfDay, 0, 0);
                    break;
                default:
                    return null;
            }
            return nextLocal.ToUniversalTime();
        }

        private async Task<Invoice?> GenerateInvoiceForScheduleAsync(TaskManagementContext context, InvoiceSchedule schedule, IRazorViewRenderer razor, IEmailSender emailSender, CancellationToken ct)
        {
            if (schedule.CompanyId == null)
            {
                throw new InvalidOperationException("Schedule without CompanyId is not supported yet.");
            }

            var tasks = await context.Tasks
                .Include(t => t.Performer).ThenInclude(p => p.Grade)
                .Include(t => t.Project)
                .Where(t => t.Status == TaskStatus.Completed && t.Hours > 0 && t.Project.CompanyId == schedule.CompanyId)
                .ToListAsync(ct);

            if (!tasks.Any())
            {
                _logger.LogInformation("No completed tasks for schedule {Id}", schedule.Id);
                return null;
            }

            var invoice = new Invoice
            {
                InvoiceDate = DateTime.Now,
                Description = $"فاکتور خودکار برای تسک‌های تکمیل شده تا {DateTime.Now:yyyy/MM/dd}",
                CustomerName = tasks.First().Project?.Company?.Name,
                ProjectId = tasks.First().ProjectId,
                CreatedAt = DateTime.Now,
                Status = InvoiceStatus.Draft
            };
            invoice.InvoiceNumber = await GenerateNextInvoiceNumberForCompany(context, schedule.CompanyId.Value, ct);

            context.Invoices.Add(invoice);
            await context.SaveChangesAsync(ct);

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
                context.InvoiceLines.Add(line);
                task.Status = TaskStatus.Invoiced;
            }
            await context.SaveChangesAsync(ct);

            string subject = $"فاکتور شماره {invoice.InvoiceNumber}";
            string body;
            try
            {
                body = await razor.RenderViewToStringAsync(null, "Invoices/EmailTemplates/InvoicePerformerTotals", invoice);
            }
            catch
            {
                body = $"<div dir='rtl'><h2>فاکتور شماره {invoice.InvoiceNumber}</h2><p>مبلغ کل: {invoice.Lines.Sum(l => l.Amount):N0} تومان</p></div>";
            }

            var emails = (schedule.RecipientEmails ?? string.Empty)
                .Replace('\r', '\n')
                .Split(new[] { '\n', ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var to in emails)
            {
                bool success = false; string? error = null;
                try
                {
                    await emailSender.SendAsync(to, subject, body);
                    success = true;
                    invoice.EmailSentAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                context.InvoiceEmailLogs.Add(new InvoiceEmailLog
                {
                    InvoiceId = invoice.Id,
                    ToEmail = to,
                    Subject = subject,
                    Body = body,
                    IsSuccess = success,
                    Error = error,
                    SentAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync(ct);
            return invoice;
        }

        private async Task<string> GenerateNextInvoiceNumberForCompany(TaskManagementContext context, int companyId, CancellationToken ct)
        {
            var numbers = await context.Invoices
                .Include(i => i.Project)
                .Where(i => i.Project.CompanyId == companyId && i.InvoiceNumber.StartsWith(companyId + "-"))
                .Select(i => i.InvoiceNumber)
                .ToListAsync(ct);

            int maxSeq = 0;
            foreach (var n in numbers)
            {
                var parts = n.Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && int.TryParse(parts[1], out var seq) && seq > maxSeq)
                {
                    maxSeq = seq;
                }
            }
            var next = maxSeq + 1;
            return $"{companyId}-{next:00000}";
        }
    }
}
