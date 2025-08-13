using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Controllers
{
    [Authorize]
    public class EmailTemplatesController : Controller
    {
        private readonly TaskManagementContext _ctx;

        public EmailTemplatesController(TaskManagementContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<IActionResult> Index()
        {
            var templates = await _ctx.EmailTemplates
                .OrderBy(t => t.Name)
                .AsNoTracking()
                .ToListAsync();
            return View(templates);
        }

        public IActionResult Create()
        {
            return View(new EmailTemplate
            {
                AvailableVariables = "{{InvoiceNumber}}, {{TotalAmount}}, {{CustomerName}}, {{IssueDate}}, {{TaskList}}"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmailTemplate template)
        {
            if (ModelState.IsValid)
            {
                template.CreatedAt = DateTime.UtcNow;
                template.UpdatedBy = User?.Identity?.Name;

                _ctx.EmailTemplates.Add(template);
                await _ctx.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(template);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var template = await _ctx.EmailTemplates.FindAsync(id);
            if (template == null) return NotFound();

            return View(template);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmailTemplate template)
        {
            if (id != template.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existing = await _ctx.EmailTemplates.FindAsync(id);
                if (existing == null) return NotFound();

                existing.Name = template.Name;
                existing.Subject = template.Subject;
                existing.Body = template.Body;
                existing.Description = template.Description;
                existing.IsActive = template.IsActive;
                existing.AvailableVariables = template.AvailableVariables;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = User?.Identity?.Name;

                await _ctx.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(template);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var template = await _ctx.EmailTemplates.FindAsync(id);
            if (template == null) return NotFound();

            _ctx.EmailTemplates.Remove(template);
            await _ctx.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var template = await _ctx.EmailTemplates.FindAsync(id);
            if (template == null) return NotFound();

            template.IsActive = !template.IsActive;
            template.UpdatedAt = DateTime.UtcNow;
            template.UpdatedBy = User?.Identity?.Name;

            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}