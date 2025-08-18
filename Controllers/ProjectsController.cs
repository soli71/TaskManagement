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
    [Authorize(Policy = Permissions.ViewProjects)]
    public class ProjectsController : Controller
    {
        private readonly TaskManagementContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ProjectAccessService _projectAccessService;
        private readonly IScalableNotificationService _notificationService;

        public ProjectsController(
            TaskManagementContext context, 
            UserManager<ApplicationUser> userManager, 
            ProjectAccessService projectAccessService,
            IScalableNotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _projectAccessService = projectAccessService;
            _notificationService = notificationService;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<Project> projectsQuery;

            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can see all projects
                projectsQuery = _context.Projects
                    .Include(p => p.Company)
                    .Include(p => p.ProjectManager)
                    .Include(p => p.Tasks);
            }
            else if (user != null)
            {
                // Non-admin users can only see projects they have explicit access to (within their company)
                var accessibleProjects = _context.Projects
                    .Where(p => _context.ProjectAccess
                        .Any(pa => pa.ProjectId == p.Id && pa.UserId == user.Id && pa.IsActive));

                projectsQuery = accessibleProjects
                    .Include(p => p.Company)
                    .Include(p => p.ProjectManager)
                    .Include(p => p.Tasks);
            }
            else
            {
                // Users without valid user info can't see any projects
                projectsQuery = _context.Projects.Where(p => false)
                    .Include(p => p.Company)
                    .Include(p => p.ProjectManager)
                    .Include(p => p.Tasks);
            }

            var projects = await projectsQuery
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        // GET: Projects/Details/5 - Redirect to Edit
        public async Task<IActionResult> Details(int? id)
        {
            return RedirectToAction(nameof(Edit), new { id });
        }

        // GET: Projects/Create
        [Authorize(Policy = Permissions.CreateProjects)]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole(Roles.SystemAdmin))
            {
                return BadRequest("شما باید به یک شرکت تخصیص داده شده باشید.");
            }

            // Auto-generate project code
            var projectCode = await GenerateProjectCode();

            var vm = new ProjectFormViewModel
            {
                Companies = await GetCompaniesForUser(),
                ProjectManagers = await GetProjectManagersForUser(),
                Code = projectCode,
                StartDate = DateTime.Today
            };

            // For non-admin users, set company automatically
            if (!User.IsInRole(Roles.SystemAdmin) && user?.CompanyId != null)
            {
                vm.CompanyId = user.CompanyId.Value;
            }

            return View(vm);
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.CreateProjects)]
        public async Task<IActionResult> Create(ProjectFormViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole(Roles.SystemAdmin))
            {
                // Send error notification for missing company assignment
                try
                {
                    if (user != null)
                    {
                        await _notificationService.SendToUserAsync(
                            user.Id.ToString(),
                            new ScalableNotificationMessage
                            {
                                Type = "error",
                                Title = "خطا در ایجاد پروژه",
                                Message = "شما باید به یک شرکت تخصیص داده شده باشید.",
                                ActionUrl = Url.Action("Index"),
                                ActionText = "بازگشت به لیست پروژه‌ها"
                            }
                        );
                    }
                }
                catch { /* Ignore notification errors */ }
                
                return BadRequest("شما باید به یک شرکت تخصیص داده شده باشید.");
            }

            // For non-admin users, force company to be their own company
            if (!User.IsInRole(Roles.SystemAdmin) && user?.CompanyId != null)
            {
                vm.CompanyId = user.CompanyId.Value;
            }

            // Auto-generate project code if empty
            if (string.IsNullOrEmpty(vm.Code))
            {
                try
                {
                    vm.Code = await GenerateProjectCode();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Code", "خطا در تولید کد پروژه: " + ex.Message);
                    
                    // Send error notification for code generation failure
                    try
                    {
                        await _notificationService.SendToUserAsync(
                            user!.Id.ToString(),
                            new ScalableNotificationMessage
                            {
                                Type = "error",
                                Title = "خطا در تولید کد پروژه",
                                Message = $"خطا در تولید کد پروژه: {ex.Message}",
                                ActionUrl = Url.Action("Create"),
                                ActionText = "تلاش مجدد"
                            }
                        );
                    }
                    catch { /* Ignore notification errors */ }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var project = new Project
                    {
                        Name = vm.Name,
                        Description = vm.Description,
                        Code = vm.Code,
                        StartDate = vm.StartDate,
                        EstimatedEndDate = vm.EstimatedEndDate,
                        Budget = vm.Budget,
                        Priority = vm.Priority,
                        CompanyId = vm.CompanyId, // Now properly set above
                        ProjectManagerId = vm.ProjectManagerId,
                        CreatedAt = DateTime.Now,
                        CreatedBy = User.Identity?.Name,
                        Status = ProjectStatus.Active,
                        IsActive = true
                    };

                    _context.Add(project);
                    await _context.SaveChangesAsync();

                    // اعمال دسترسی خودکار برای مدیران شرکت
                    try
                    {
                        await _projectAccessService.GrantManagerAccessOnProjectCreation(
                            project.Id,
                            vm.CompanyId,
                            user?.Id
                        );
                    }
                    catch (Exception accessEx)
                    {
                        // Log the access error but don't fail the project creation
                        // Send warning notification
                        try
                        {
                            await _notificationService.SendToUserAsync(
                                user!.Id.ToString(),
                                new ScalableNotificationMessage
                                {
                                    Type = "warning",
                                    Title = "هشدار در اعطای دسترسی",
                                    Message = $"پروژه ایجاد شد اما خطا در اعطای دسترسی خودکار: {accessEx.Message}",
                                    ActionUrl = Url.Action("Access", new { id = project.Id }),
                                    ActionText = "مدیریت دسترسی‌ها"
                                }
                            );
                        }
                        catch { /* Ignore notification errors */ }
                    }

                    // ارسال notification موفقیت
                    try
                    {
                        await _notificationService.SendToUserAsync(
                            user!.Id.ToString(),
                            new ScalableNotificationMessage
                            {
                                Type = "success",
                                Title = "پروژه ایجاد شد",
                                Message = $"پروژه '{project.Name}' با موفقیت ایجاد شد.",
                                ActionUrl = Url.Action("Details", new { id = project.Id }),
                                ActionText = "مشاهده پروژه"
                            }
                        );
                    }
                    catch { /* Ignore notification errors */ }

                    TempData["SuccessMessage"] = "پروژه با موفقیت ایجاد شد.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException dbEx)
                {
                    var errorMessage = "خطا در ذخیره پروژه در پایگاه داده.";
                    
                    // Check for specific database constraint violations
                    if (dbEx.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
                    {
                        if (dbEx.InnerException.Message.Contains("Code"))
                        {
                            errorMessage = "کد پروژه تکراری است. لطفاً کد دیگری انتخاب کنید.";
                            ModelState.AddModelError("Code", errorMessage);
                        }
                        else
                        {
                            errorMessage = "اطلاعات تکراری است. لطفاً مقادیر دیگری انتخاب کنید.";
                        }
                    }
                    
                    ModelState.AddModelError("", errorMessage);
                    
                    // Send specific database error notification
                    try
                    {
                        await _notificationService.SendToUserAsync(
                            user!.Id.ToString(),
                            new ScalableNotificationMessage
                            {
                                Type = "error",
                                Title = "خطا در پایگاه داده",
                                Message = $"خطا در ایجاد پروژه '{vm.Name}': {errorMessage}",
                                ActionUrl = Url.Action("Create"),
                                ActionText = "تلاش مجدد"
                            }
                        );
                    }
                    catch { /* Ignore notification errors */ }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "خطا در ایجاد پروژه: " + ex.Message);
                    
                    // Send general error notification
                    try
                    {
                        await _notificationService.SendToUserAsync(
                            user!.Id.ToString(),
                            new ScalableNotificationMessage
                            {
                                Type = "error",
                                Title = "خطا در ایجاد پروژه",
                                Message = $"خطا در ایجاد پروژه '{vm.Name}': {ex.Message}",
                                ActionUrl = Url.Action("Create"),
                                ActionText = "تلاش مجدد"
                            }
                        );
                    }
                    catch { /* Ignore notification errors */ }
                }
            }
            else
            {
                // Send validation error notification
                var validationErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                if (validationErrors.Any())
                {
                    try
                    {
                        await _notificationService.SendToUserAsync(
                            user!.Id.ToString(),
                            new ScalableNotificationMessage
                            {
                                Type = "warning",
                                Title = "خطاهای اعتبارسنجی",
                                Message = $"لطفاً خطاهای فرم را اصلاح کنید: {string.Join(", ", validationErrors.Take(3))}",
                                ActionUrl = Url.Action("Create"),
                                ActionText = "اصلاح فرم"
                            }
                        );
                    }
                    catch { /* Ignore notification errors */ }
                }
            }

            // Re-populate view model for validation errors
            vm.Companies = await GetCompaniesForUser();
            vm.ProjectManagers = await GetProjectManagersForUser();

            // Ensure CompanyId is set for non-admin users even in error case
            if (!User.IsInRole(Roles.SystemAdmin) && user?.CompanyId != null)
            {
                vm.CompanyId = user.CompanyId.Value;
            }

            return View(vm);
        }

        // GET: Projects/Edit/5
        [Authorize(Policy = Permissions.EditProjects)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var project = await _context.Projects
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            // Check if user has access to this project
            if (!await HasProjectAccess(user, project))
            {
                return Forbid();
            }

            var vm = new ProjectFormViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Code = project.Code,
                StartDate = project.StartDate,
                EstimatedEndDate = project.EstimatedEndDate,
                ActualEndDate = project.ActualEndDate,
                Budget = project.Budget,
                ActualCost = project.ActualCost,
                Priority = project.Priority,
                Status = project.Status,
                CompanyId = project.CompanyId,
                ProjectManagerId = project.ProjectManagerId,
                Companies = await GetCompaniesForUser(),
                ProjectManagers = await GetProjectManagersForUser()
            };

            // Add additional data for the view
            ViewBag.TaskCount = await _context.Tasks.CountAsync(t => t.ProjectId == id);
            ViewBag.InvoiceCount = await _context.Invoices.CountAsync(i => i.ProjectId == id);
            ViewBag.CreatedAt = project.CreatedAt;

            return View(vm);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.EditProjects)]
        public async Task<IActionResult> Edit(int id, ProjectFormViewModel vm)
        {
            if (id != vm.Id)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var existingProject = await _context.Projects.FindAsync(id);
            if (existingProject == null)
            {
                return NotFound();
            }

            // Check if user has access to this project
            if (!await HasProjectAccess(user, existingProject))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingProject.Name = vm.Name;
                    existingProject.Description = vm.Description;
                    existingProject.Code = vm.Code;
                    existingProject.StartDate = vm.StartDate;
                    existingProject.EstimatedEndDate = vm.EstimatedEndDate;
                    existingProject.ActualEndDate = vm.ActualEndDate;
                    existingProject.Budget = vm.Budget;
                    existingProject.ActualCost = vm.ActualCost;
                    existingProject.Priority = vm.Priority;
                    existingProject.Status = vm.Status;
                    existingProject.CompanyId = User.IsInRole(Roles.SystemAdmin) ? vm.CompanyId : user!.CompanyId!.Value;
                    existingProject.ProjectManagerId = vm.ProjectManagerId;
                    existingProject.UpdatedAt = DateTime.Now;
                    existingProject.UpdatedBy = User.Identity?.Name;

                    _context.Update(existingProject);
                    await _context.SaveChangesAsync();

                    // ارسال notification موفقیت
                    await _notificationService.SendToUserAsync(
                        user.Id.ToString(),
                        new ScalableNotificationMessage
                        {
                            Type = "success",
                            Title = "پروژه به‌روزرسانی شد",
                            Message = $"پروژه '{existingProject.Name}' با موفقیت به‌روزرسانی شد.",
                            ActionUrl = Url.Action("Details", new { id = existingProject.Id }),
                            ActionText = "مشاهده پروژه"
                        }
                    );

                    TempData["SuccessMessage"] = "پروژه با موفقیت به‌روزرسانی شد.";
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!ProjectExists(id))
                    {
                        // Send error notification for not found
                        try
                        {
                            await _notificationService.SendToUserAsync(
                                user!.Id.ToString(),
                                new ScalableNotificationMessage
                                {
                                    Type = "error",
                                    Title = "خطا در به‌روزرسانی پروژه",
                                    Message = "پروژه مورد نظر یافت نشد.",
                                    ActionUrl = Url.Action("Index"),
                                    ActionText = "بازگشت به لیست پروژه‌ها"
                                }
                            );
                        }
                        catch { /* Ignore notification errors */ }
                        
                        return NotFound();
                    }
                    else
                    {
                        // Send error notification for concurrency
                        try
                        {
                            await _notificationService.SendToUserAsync(
                                user!.Id.ToString(),
                                new ScalableNotificationMessage
                                {
                                    Type = "error",
                                    Title = "خطا در به‌روزرسانی پروژه",
                                    Message = "پروژه توسط کاربر دیگری تغییر یافته است. لطفاً مجدداً تلاش کنید.",
                                    ActionUrl = Url.Action("Edit", new { id }),
                                    ActionText = "تلاش مجدد"
                                }
                            );
                        }
                        catch { /* Ignore notification errors */ }
                        
                        TempData["ErrorMessage"] = "پروژه توسط کاربر دیگری تغییر یافته است. لطفاً مجدداً تلاش کنید.";
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    // Send general error notification
                    try
                    {
                        await _notificationService.SendToUserAsync(
                            user!.Id.ToString(),
                            new ScalableNotificationMessage
                            {
                                Type = "error",
                                Title = "خطا در به‌روزرسانی پروژه",
                                Message = $"خطا در به‌روزرسانی پروژه: {ex.Message}",
                                ActionUrl = Url.Action("Edit", new { id }),
                                ActionText = "تلاش مجدد"
                            }
                        );
                    }
                    catch { /* Ignore notification errors */ }
                    
                    TempData["ErrorMessage"] = $"خطا در به‌روزرسانی پروژه: {ex.Message}";
                    vm.Companies = await GetCompaniesForUser();
                    vm.ProjectManagers = await GetProjectManagersForUser();
                    return View(vm);
                }
                return RedirectToAction(nameof(Index));
            }

            vm.Companies = await GetCompaniesForUser();
            vm.ProjectManagers = await GetProjectManagersForUser();
            return View(vm);
        }

        // POST: Projects/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            // Check if user has access to this project
            if (!await HasProjectAccess(user, project))
            {
                return Forbid();
            }

            project.IsActive = !project.IsActive;
            project.UpdatedAt = DateTime.Now;
            project.UpdatedBy = User.Identity?.Name;

            _context.Update(project);
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = project.IsActive });
        }

        // GET: Projects/Tasks/5
        public async Task<IActionResult> Tasks(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var project = await _context.Projects
                .Include(p => p.Company)
                .Include(p => p.Tasks)
                .ThenInclude(t => t.Performer)
                .ThenInclude(pf => pf.Grade)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            // Check if user has access to this project
            if (!await HasProjectAccess(user, project))
            {
                return Forbid();
            }

            return View(project);
        }

        private async Task<List<Company>> GetCompaniesForUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole(Roles.SystemAdmin))
            {
                return await _context.Companies.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            }
            else if (user?.CompanyId != null)
            {
                return await _context.Companies.Where(c => c.Id == user.CompanyId && c.IsActive).ToListAsync();
            }
            return new List<Company>();
        }

        private async Task<List<ApplicationUser>> GetProjectManagersForUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole(Roles.SystemAdmin))
            {
                return await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            }
            else if (user?.CompanyId != null)
            {
                return await _context.Users.Where(u => u.CompanyId == user.CompanyId && u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            }
            return new List<ApplicationUser>();
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }

        // GET: Projects/Access/5
        public async Task<IActionResult> Access(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var project = await _context.Projects
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            // Check if user can manage access to this project
            if (!await CanManageProjectAccess(user))
            {
                return Forbid();
            }

            // Get current project access
            var currentAccess = await _context.ProjectAccess
                .Include(pa => pa.User)
                .Include(pa => pa.GrantedBy)
                .Where(pa => pa.ProjectId == id && pa.IsActive)
                .ToListAsync();

            // Get available users for this project
            List<ApplicationUser> availableUsers;
            if (User.IsInRole(Roles.SystemAdmin))
            {
                availableUsers = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }
            else
            {
                availableUsers = await _context.Users
                    .Where(u => u.CompanyId == project.CompanyId && u.IsActive)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }

            ViewData["Project"] = project;
            ViewData["CurrentAccess"] = currentAccess;
            ViewData["AvailableUsers"] = availableUsers;

            return View();
        }

        // POST: Projects/GrantAccess/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantAccess(int projectId, int userId, string? notes)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var project = await _context.Projects
                    .Include(p => p.Company)
                    .FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null)
                {
                    return NotFound();
                }

                // Check if user can manage access to this project
                if (!await CanManageProjectAccess(user))
                {
                    return Forbid();
                }

                // بررسی اینکه آیا کاربر جاری مجاز به مدیریت این پروژه است (چک شرکت)
                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Contains(Roles.SystemAdmin))
                {
                    if (user!.CompanyId != project.CompanyId)
                    {
                        // Send error notification
                        try
                        {
                            await _notificationService.SendToUserAsync(
                                user.Id.ToString(),
                                new ScalableNotificationMessage
                                {
                                    Type = "error",
                                    Title = "خطا در اعطای دسترسی",
                                    Message = "شما مجاز به مدیریت این پروژه نیستید.",
                                    ActionUrl = Url.Action("Index"),
                                    ActionText = "بازگشت به لیست پروژه‌ها"
                                }
                            );
                        }
                        catch { /* Ignore notification errors */ }
                        
                        return Json(new { success = false, message = "شما مجاز به مدیریت این پروژه نیستید." });
                    }
                }

                // Get the target user to check company validation
                var targetUser = await _context.Users.FindAsync(userId);
                if (targetUser == null)
                {
                    return Json(new { success = false, message = "کاربر یافت نشد." });
                }

                // Validate that the target user belongs to the same company as the project (for non-admin users)
                if (!userRoles.Contains(Roles.SystemAdmin))
                {
                    if (targetUser.CompanyId != project.CompanyId)
                    {
                        return Json(new { success = false, message = "فقط می‌توانید به کاربران همان شرکت دسترسی اعطا کنید." });
                    }
                }

                // Check if access already exists
                var existingAccess = await _context.ProjectAccess
                    .FirstOrDefaultAsync(pa => pa.ProjectId == projectId && pa.UserId == userId && pa.IsActive);

                if (existingAccess != null)
                {
                    return Json(new { success = false, message = "این کاربر قبلاً دسترسی دارد." });
                }

                // Grant access
                var projectAccess = new ProjectAccess
                {
                    ProjectId = projectId,
                    UserId = userId,
                    GrantedById = user!.Id,
                    Notes = notes,
                    IsActive = true
                };

                _context.ProjectAccess.Add(projectAccess);
                await _context.SaveChangesAsync();

                // Send success notification
                try
                {
                    await _notificationService.SendToUserAsync(
                        user.Id.ToString(),
                        new ScalableNotificationMessage
                        {
                            Type = "success",
                            Title = "دسترسی اعطا شد",
                            Message = $"دسترسی به پروژه '{project.Name}' برای کاربر '{targetUser.FullName}' اعطا شد.",
                            ActionUrl = Url.Action("Access", new { id = projectId }),
                            ActionText = "مدیریت دسترسی‌ها"
                        }
                    );
                    
                    // Also notify the target user
                    await _notificationService.SendToUserAsync(
                        targetUser.Id.ToString(),
                        new ScalableNotificationMessage
                        {
                            Type = "info",
                            Title = "دسترسی جدید",
                            Message = $"شما دسترسی به پروژه '{project.Name}' را دریافت کردید.",
                            ActionUrl = Url.Action("Details", new { id = projectId }),
                            ActionText = "مشاهده پروژه"
                        }
                    );
                }
                catch { /* Ignore notification errors */ }

                return Json(new { success = true, message = "دسترسی اعطا شد." });
            }
            catch (Exception ex)
            {
                // Send error notification
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        await _notificationService.SendToUserAsync(
                            user.Id.ToString(),
                            new ScalableNotificationMessage
                            {
                                Type = "error",
                                Title = "خطا در اعطای دسترسی",
                                Message = $"خطا در اعطای دسترسی: {ex.Message}",
                                ActionUrl = Url.Action("Access", new { id = projectId }),
                                ActionText = "تلاش مجدد"
                            }
                        );
                    }
                }
                catch { /* Ignore notification errors */ }
                
                return Json(new { success = false, message = $"خطا در اعطای دسترسی: {ex.Message}" });
            }
        }

        // POST: Projects/RevokeAccess/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeAccess(int projectId, int userId)
        {
            var user = await _userManager.GetUserAsync(User);
            var project = await _context.Projects
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                return NotFound();
            }

            // بررسی دسترسی کاربر جاری برای مدیریت این پروژه
            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Contains(Roles.SystemAdmin))
            {
                // کاربر باید مدیر باشد و پروژه متعلق به شرکت او باشد
                if (!userRoles.Contains(Roles.CompanyManager) && !userRoles.Contains(Roles.ProjectManager))
                {
                    return Forbid();
                }
                
                if (user?.CompanyId != project.CompanyId)
                {
                    return Json(new { success = false, message = "شما مجاز به مدیریت این پروژه نیستید." });
                }
            }

            // Get the user being revoked and check if they are a Manager
            var userToRevoke = await _userManager.FindByIdAsync(userId.ToString());
            if (userToRevoke != null)
            {
                var userToRevokeRoles = await _userManager.GetRolesAsync(userToRevoke);
                
                // بررسی اینکه آیا کاربر هدف مدیر شرکت است
                if (userToRevokeRoles.Contains(Roles.CompanyManager) && userToRevoke.CompanyId == project.CompanyId)
                {
                    return Json(new { success = false, message = "نمی‌توان دسترسی مدیر سازمان را لغو کرد." });
                }
                
                // بررسی اینکه آیا کاربر فعلی مجاز به لغو دسترسی این کاربر است
                if (!userRoles.Contains(Roles.SystemAdmin))
                {
                    // مدیر پروژه نمی‌تواند دسترسی مدیر شرکت را لغو کند
                    if (userRoles.Contains(Roles.ProjectManager) && userToRevokeRoles.Contains(Roles.CompanyManager))
                    {
                        return Json(new { success = false, message = "شما مجاز به لغو دسترسی مدیر شرکت نیستید." });
                    }
                }
            }

            // Revoke access
            var projectAccess = await _context.ProjectAccess
                .FirstOrDefaultAsync(pa => pa.ProjectId == projectId && pa.UserId == userId && pa.IsActive);

            if (projectAccess != null)
            {
                projectAccess.IsActive = false;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "دسترسی لغو شد." });
        }

        // POST: Projects/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.DeleteProjects)]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var project = await _context.Projects.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            // Access check
            if (!await HasProjectAccess(user, project))
            {
                return Forbid();
            }

            // Nullify task references
            var relatedTasks = await _context.Tasks.Where(t => t.ProjectId == id).ToListAsync();
            foreach (var t in relatedTasks)
            {
                t.ProjectId = null;
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            // ارسال notification موفقیت حذف
            await _notificationService.SendToUserAsync(
                user.Id.ToString(),
                new ScalableNotificationMessage
                {
                    Type = "warning",
                    Title = "پروژه حذف شد",
                    Message = $"پروژه '{project.Name}' با موفقیت حذف شد.",
                    ActionUrl = Url.Action("Index"),
                    ActionText = "مشاهده لیست پروژه‌ها"
                }
            );

            TempData["SuccessMessage"] = "پروژه حذف شد.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> GenerateProjectCode()
        {
            var currentYear = DateTime.Now.Year;
            var prefix = $"PRJ{currentYear}";

            // Get the last project with this year's prefix
            var lastProject = await _context.Projects
                .Where(p => p.Code!.StartsWith(prefix))
                .OrderByDescending(p => p.Code)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastProject != null && !string.IsNullOrEmpty(lastProject.Code))
            {
                var numberPart = lastProject.Code.Substring(prefix.Length);
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}"; // PRJ2025001, PRJ2025002, etc.
        }

        // Helper method to check if user can manage project access
        private async Task<bool> CanManageProjectAccess(ApplicationUser user)
        {
            return User.IsInRole(Roles.SystemAdmin) || User.IsInRole(Roles.CompanyManager);
        }

        // Helper method to check if user has access to a project
        private async Task<bool> HasProjectAccess(ApplicationUser user, Project project)
        {
            // Admin always has access
            if (User.IsInRole(Roles.SystemAdmin))
                return true;

            //// Company members and managers have access to their company projects
            //if (user.CompanyId == project.CompanyId)
            //    return true;

            // Check explicit project access
            var hasAccess = await _context.ProjectAccess
                .AnyAsync(pa => pa.ProjectId == project.Id && pa.UserId == user.Id && pa.IsActive);

            return hasAccess;
        }

        // GET: Projects/ManageAccess/5
        public async Task<IActionResult> ManageAccess(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !await CanManageProjectAccess(user))
            {
                return Forbid();
            }

            var project = await _context.Projects
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            // بررسی اینکه آیا کاربر مجاز به مدیریت این پروژه است (چک شرکت)
            var userRoles = await _userManager.GetRolesAsync(user);
            if (!userRoles.Contains(Roles.SystemAdmin))
            {
                if (user.CompanyId != project.CompanyId)
                {
                    return BadRequest("شما مجاز به مدیریت این پروژه نیستید.");
                }
            }

            var currentAccess = await _context.ProjectAccess
                .Include(pa => pa.User)
                .ThenInclude(u => u.Company)
                .Include(pa => pa.GrantedBy)
                .Where(pa => pa.ProjectId == id && pa.IsActive)
                .ToListAsync();

            var currentAccessViewModels = new List<ProjectAccessViewModel>();
            foreach (var pa in currentAccess)
            {
                var paUserRoles = await _userManager.GetRolesAsync(pa.User);
                currentAccessViewModels.Add(new ProjectAccessViewModel
                {
                    Id = pa.Id,
                    ProjectId = pa.ProjectId,
                    UserId = pa.UserId,
                    UserFullName = pa.User.FullName,
                    UserCompanyName = pa.User.Company != null ? pa.User.Company.Name : "بدون شرکت",
                    Notes = pa.Notes,
                    GrantedAt = pa.GrantedAt,
                    GrantedByName = pa.GrantedBy != null ? pa.GrantedBy.FullName : "سیستم",
                    IsActive = pa.IsActive,
                    UserRoles = paUserRoles.ToList()
                });
            }

            // دریافت کاربران قابل دسترسی (فقط از همان شرکت پروژه)
            var currentUserRoles = await _userManager.GetRolesAsync(user);
            var targetCompanyId = currentUserRoles.Contains(Roles.SystemAdmin) ? project.CompanyId : user.CompanyId;
            
            var availableUsers = await _context.Users
                .Include(u => u.Company)
                .Where(u => u.IsActive && 
                           !currentAccessViewModels.Select(ca => ca.UserId).Contains(u.Id) && 
                           u.CompanyId == targetCompanyId)
                .ToListAsync();

            var viewModel = new ManageProjectAccessViewModel
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                CurrentAccess = currentAccessViewModels,
                AvailableUsers = availableUsers
            };

            return View(viewModel);
        }

        // POST: Projects/GrantAccess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantAccess(ManageProjectAccessViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !await CanManageProjectAccess(user))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                // Reload data for the view
                model.CurrentAccess = await _context.ProjectAccess
                    .Include(pa => pa.User)
                    .ThenInclude(u => u.Company)
                    .Include(pa => pa.GrantedBy)
                    .Where(pa => pa.ProjectId == model.ProjectId && pa.IsActive)
                    .Select(pa => new ProjectAccessViewModel
                    {
                        Id = pa.Id,
                        ProjectId = pa.ProjectId,
                        UserId = pa.UserId,
                        UserFullName = pa.User.FullName,
                        UserCompanyName = pa.User.Company != null ? pa.User.Company.Name : "بدون شرکت",
                        Notes = pa.Notes,
                        GrantedAt = pa.GrantedAt,
                        GrantedByName = pa.GrantedBy != null ? pa.GrantedBy.FullName : "سیستم",
                        IsActive = pa.IsActive
                    })
                    .ToListAsync();

                model.AvailableUsers = await _context.Users
                    .Include(u => u.Company)
                    .Where(u => u.IsActive && !model.CurrentAccess.Select(ca => ca.UserId).Contains(u.Id))
                    .ToListAsync();

                return View("ManageAccess", model);
            }

            foreach (var userId in model.SelectedUserIds)
            {
                var existingAccess = await _context.ProjectAccess
                    .FirstOrDefaultAsync(pa => pa.ProjectId == model.ProjectId && pa.UserId == userId);

                if (existingAccess != null)
                {
                    existingAccess.IsActive = true;
                    existingAccess.Notes = model.Notes;
                    existingAccess.GrantedById = user.Id;
                    existingAccess.GrantedAt = DateTime.Now;
                }
                else
                {
                    var newAccess = new ProjectAccess
                    {
                        ProjectId = model.ProjectId,
                        UserId = userId,
                        Notes = model.Notes,
                        GrantedById = user.Id,
                        GrantedAt = DateTime.Now,
                        IsActive = true
                    };
                    _context.ProjectAccess.Add(newAccess);
                }
            }

            await _context.SaveChangesAsync();
            
            // ارسال notification موفقیت اعطای دسترسی
            var project = await _context.Projects.FindAsync(model.ProjectId);
            await _notificationService.SendToUserAsync(
                user.Id.ToString(),
                new ScalableNotificationMessage
                {
                    Type = "success",
                    Title = "دسترسی اعطا شد",
                    Message = $"دسترسی‌ها به پروژه '{project?.Name}' با موفقیت اعطا شد.",
                    ActionUrl = Url.Action("ManageAccess", new { id = model.ProjectId }),
                    ActionText = "مدیریت دسترسی‌ها"
                }
            );

            // ارسال notification به کاربرانی که دسترسی گرفتند
            foreach (var userId in model.SelectedUserIds)
            {
                await _notificationService.SendToUserAsync(
                    userId.ToString(),
                    new ScalableNotificationMessage
                    {
                        Type = "info",
                        Title = "دسترسی جدید به پروژه",
                        Message = $"شما دسترسی به پروژه '{project?.Name}' دریافت کردید.",
                        ActionUrl = Url.Action("Details", new { id = model.ProjectId }),
                        ActionText = "مشاهده پروژه"
                    }
                );
            }

            TempData["SuccessMessage"] = "دسترسی‌ها با موفقیت اعطا شد.";

            return RedirectToAction(nameof(ManageAccess), new { id = model.ProjectId });
        }

        // POST: Projects/RevokeAccess/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeAccess(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !await CanManageProjectAccess(user))
            {
                return Forbid();
            }

            var projectAccess = await _context.ProjectAccess
                .Include(pa => pa.User)
                .Include(pa => pa.Project)
                .FirstOrDefaultAsync(pa => pa.Id == id);
            if (projectAccess == null)
            {
                return NotFound();
            }

            // Check if the user being revoked is a Manager of the organization
            var userRoles = await _userManager.GetRolesAsync(projectAccess.User);
            if (userRoles.Contains("CompanyManager") && projectAccess.User.CompanyId == projectAccess.Project.CompanyId)
            {
                // ارسال notification خطا
                await _notificationService.SendToUserAsync(
                    user.Id.ToString(),
                    new ScalableNotificationMessage
                    {
                        Type = "error",
                        Title = "خطا در لغو دسترسی",
                        Message = "نمی‌توان دسترسی مدیر سازمان را لغو کرد.",
                        ActionUrl = Url.Action("ManageAccess", new { id = projectAccess.ProjectId }),
                        ActionText = "بازگشت به مدیریت دسترسی"
                    }
                );

                TempData["ErrorMessage"] = "نمی‌توان دسترسی مدیر سازمان را لغو کرد.";
                return RedirectToAction(nameof(ManageAccess), new { id = projectAccess.ProjectId });
            }

            projectAccess.IsActive = false;
            await _context.SaveChangesAsync();

            // ارسال notification موفقیت لغو دسترسی
            await _notificationService.SendToUserAsync(
                user.Id.ToString(),
                new ScalableNotificationMessage
                {
                    Type = "warning",
                    Title = "دسترسی لغو شد",
                    Message = $"دسترسی کاربر '{projectAccess.User.UserName}' به پروژه '{projectAccess.Project.Name}' لغو شد.",
                    ActionUrl = Url.Action("ManageAccess", new { id = projectAccess.ProjectId }),
                    ActionText = "مدیریت دسترسی‌ها"
                }
            );

            // ارسال notification به کاربری که دسترسی‌اش لغو شد
            await _notificationService.SendToUserAsync(
                projectAccess.UserId.ToString(),
                new ScalableNotificationMessage
                {
                    Type = "warning",
                    Title = "دسترسی شما لغو شد",
                    Message = $"دسترسی شما به پروژه '{projectAccess.Project.Name}' لغو شد.",
                    ActionUrl = Url.Action("Index"),
                    ActionText = "مشاهده پروژه‌های در دسترس"
                }
            );

            TempData["SuccessMessage"] = "دسترسی با موفقیت لغو شد.";
            return RedirectToAction(nameof(ManageAccess), new { id = projectAccess.ProjectId });
        }

        // GET: Projects/AccessHistory/5
        public async Task<IActionResult> AccessHistory(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !await CanManageProjectAccess(user))
            {
                return Forbid();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            var accessHistory = await _context.ProjectAccess
                .Include(pa => pa.User)
                .ThenInclude(u => u.Company)
                .Include(pa => pa.GrantedBy)
                .Where(pa => pa.ProjectId == id)
                .OrderByDescending(pa => pa.GrantedAt)
                .Select(pa => new ProjectAccessViewModel
                {
                    Id = pa.Id,
                    ProjectId = pa.ProjectId,
                    UserId = pa.UserId,
                    UserFullName = pa.User.FullName,
                    UserCompanyName = pa.User.Company != null ? pa.User.Company.Name : "بدون شرکت",
                    Notes = pa.Notes,
                    GrantedAt = pa.GrantedAt,
                    GrantedByName = pa.GrantedBy != null ? pa.GrantedBy.FullName : "سیستم",
                    IsActive = pa.IsActive
                })
                .ToListAsync();

            var viewModel = new ProjectAccessListViewModel
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                AccessList = accessHistory
            };

            return View(viewModel);
        }
    }
}