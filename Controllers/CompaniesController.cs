using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;
using TaskManagementMvc.Models.ViewModels;
using TaskManagementMvc.Services;

namespace TaskManagementMvc.Controllers
{
    [Authorize(Policy = Permissions.ViewCompany)]
    public class CompaniesController : Controller
    {
        private readonly TaskManagementContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IScalableNotificationService _notificationService;

        public CompaniesController(
            TaskManagementContext context, 
            UserManager<ApplicationUser> userManager,
            IScalableNotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }
        // GET: Companies/Overview (Admin can select a company and see projects and tasks)
        [Authorize(Roles = Roles.SystemAdmin)]
        public async Task<IActionResult> Overview(int? companyId)
        {
            var model = new CompanyOverviewViewModel
            {
                SelectedCompanyId = companyId,
                Companies = await _context.Companies.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync()
            };

            if (companyId.HasValue)
            {
                model.Projects = await _context.Projects
                    .Include(p => p.ProjectManager)
                    .Where(p => p.CompanyId == companyId.Value)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                model.Tasks = await _context.Tasks
                    .Include(t => t.Performer).ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .Where(t => t.ProjectId != null && _context.Projects.Any(p => p.Id == t.ProjectId && p.CompanyId == companyId.Value))
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }

            return View(model);
        }

        // GET: Companies
        [Authorize(Roles = Roles.SystemAdmin)]
        public async Task<IActionResult> Index()
        {
            var companies = await _context.Companies
                .Include(c => c.Users)
                .Include(c => c.Projects)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(companies);
        }

        // GET: Companies/MyCompany
        public async Task<IActionResult> MyCompany()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null)
            {
                return NotFound("شما به هیچ شرکتی تخصیص داده نشده‌اید.");
            }

            var company = await _context.Companies
                .Include(c => c.Users)
                .Include(c => c.Projects)
                .Include(c => c.Grades)
                .FirstOrDefaultAsync(c => c.Id == user.CompanyId);

            if (company == null)
            {
                return NotFound("شرکت یافت نشد.");
            }

            return View(company);
        }

        // GET: Companies/Details/5
        [Authorize(Roles = Roles.SystemAdmin)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Companies
                .Include(c => c.Users)
                .Include(c => c.Projects)
                .Include(c => c.Grades)
                .Include(c => c.Tasks)
                .Include(c => c.Invoices)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        // GET: Companies/Create
        [Authorize(Policy = Permissions.ManageCompany)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Companies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.ManageCompany)]
        public async Task<IActionResult> Create([Bind("Name,Description,Address,Phone,Email,Website,LogoPath")] Company company)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    company.CreatedAt = DateTime.Now;
                    company.CreatedBy = User.Identity?.Name;
                    company.IsActive = true;

                    _context.Add(company);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "شرکت با موفقیت ایجاد شد.";
                    
                    // Send success notification
                    try
                    {
                        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            await _notificationService.SendToUserAsync(
                                userId,
                                new ScalableNotificationMessage
                                {
                                    Type = "success",
                                    Title = "شرکت جدید ایجاد شد",
                                    Message = $"شرکت '{company.Name}' با موفقیت ایجاد شد.",
                                    ActionUrl = Url.Action("Details", new { id = company.Id }),
                                    ActionText = "مشاهده شرکت"
                                }
                            );
                        }
                    }
                    catch { /* Ignore notification errors */ }
                    
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "خطا در ایجاد شرکت: " + ex.Message);
                
                // Send error notification
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
                                Title = "خطا در ایجاد شرکت",
                                Message = $"خطا در ایجاد شرکت '{company.Name}': {ex.Message}",
                                ActionUrl = Url.Action("Create"),
                                ActionText = "تلاش مجدد"
                            }
                        );
                    }
                }
                catch { /* Ignore notification errors */ }
            }
            
            return View(company);
        }

        // GET: Companies/Edit/5
        [Authorize(Policy = Permissions.ManageCompany)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        // POST: Companies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.ManageCompany)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Address,Phone,Email,Website,LogoPath,IsActive")] Company company)
        {
            if (id != company.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCompany = await _context.Companies.FindAsync(id);
                    if (existingCompany == null)
                    {
                        return NotFound();
                    }

                    existingCompany.Name = company.Name;
                    existingCompany.Description = company.Description;
                    existingCompany.Address = company.Address;
                    existingCompany.Phone = company.Phone;
                    existingCompany.Email = company.Email;
                    existingCompany.Website = company.Website;
                    existingCompany.LogoPath = company.LogoPath;
                    existingCompany.IsActive = company.IsActive;
                    existingCompany.UpdatedAt = DateTime.Now;
                    existingCompany.UpdatedBy = User.Identity?.Name;

                    _context.Update(existingCompany);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "شرکت با موفقیت به‌روزرسانی شد.";
                    
                    // Send success notification
                    try
                    {
                        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            await _notificationService.SendToUserAsync(
                                userId,
                                new ScalableNotificationMessage
                                {
                                    Type = "success",
                                    Title = "شرکت به‌روزرسانی شد",
                                    Message = $"شرکت '{existingCompany.Name}' با موفقیت به‌روزرسانی شد.",
                                    ActionUrl = Url.Action("Details", new { id = existingCompany.Id }),
                                    ActionText = "مشاهده شرکت"
                                }
                            );
                        }
                    }
                    catch { /* Ignore notification errors */ }
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!CompanyExists(company.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        // Send error notification for concurrency
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
                                        Title = "خطا در به‌روزرسانی شرکت",
                                        Message = $"خطا در به‌روزرسانی شرکت '{company.Name}': تداخل در به‌روزرسانی.",
                                        ActionUrl = Url.Action("Edit", new { id = company.Id }),
                                        ActionText = "تلاش مجدد"
                                    }
                                );
                            }
                        }
                        catch { /* Ignore notification errors */ }
                        
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "خطا در به‌روزرسانی شرکت: " + ex.Message);
                    
                    // Send error notification
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
                                    Title = "خطا در به‌روزرسانی شرکت",
                                    Message = $"خطا در به‌روزرسانی شرکت '{company.Name}': {ex.Message}",
                                    ActionUrl = Url.Action("Edit", new { id = company.Id }),
                                    ActionText = "تلاش مجدد"
                                }
                            );
                        }
                    }
                    catch { /* Ignore notification errors */ }
                    
                    return View(company);
                }
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        // POST: Companies/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.SystemAdmin)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            company.IsActive = !company.IsActive;
            company.UpdatedAt = DateTime.Now;
            company.UpdatedBy = User.Identity?.Name;

            _context.Update(company);
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = company.IsActive });
        }

        // GET: Companies/Users/5
        [Authorize(Roles = Roles.SystemAdmin)]
        public async Task<IActionResult> Users(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Companies
                .Include(c => c.Users)
                .ThenInclude(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        // GET: Companies/Projects/5
        [Authorize(Roles = Roles.SystemAdmin)]
        public async Task<IActionResult> Projects(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Companies
                .Include(c => c.Projects)
                .ThenInclude(p => p.ProjectManager)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        private bool CompanyExists(int id)
        {
            return _context.Companies.Any(e => e.Id == id);
        }
    }
}
