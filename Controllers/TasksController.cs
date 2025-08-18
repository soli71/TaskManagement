using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;
using TaskManagementMvc.Models.ViewModels;
using TaskManagementMvc.Services;
using TaskStatus = TaskManagementMvc.Models.TaskStatus;

namespace TaskManagementMvc.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly TaskManagementContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IScalableNotificationService _notificationService;

        public TasksController(
            TaskManagementContext context,
            UserManager<ApplicationUser> userManager,
            IScalableNotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: Tasks
        [Authorize(Policy = "Tasks.View")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> tasksQuery;

            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can see all tasks
                tasksQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Include(t => t.CreatedBy);
            }
            else if (user != null)
            {
                // Non-admin users can only see tasks from projects they have explicit access to (within their company)
                var accessibleProjectIds = await _context.ProjectAccess
                    .Where(pa => pa.UserId == user.Id && pa.IsActive)
                    .Select(pa => pa.ProjectId)
                    .ToListAsync();

                tasksQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Include(t => t.CreatedBy)
                    .Where(t => t.ProjectId != null && accessibleProjectIds.Contains(t.ProjectId.Value));
            }
            else
            {
                // Users without valid user info can't see any tasks
                tasksQuery = _context.Tasks.Where(t => false);
            }

            var tasks = await tasksQuery
                .Where(t => !t.IsArchived)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tasks);
        }

        // GET: Tasks/Archived
        [Authorize(Policy = "Tasks.View")]
        public async Task<IActionResult> Archived()
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> tasksQuery;

            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can see all archived tasks
                tasksQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Include(t => t.CreatedBy);
            }
            else if (user != null)
            {
                // Non-admin users can only see archived tasks from projects they have explicit access to (within their company)
                var accessibleProjectIds = await _context.ProjectAccess
                    .Where(pa => pa.UserId == user.Id && pa.IsActive)
                    .Select(pa => pa.ProjectId)
                    .ToListAsync();

                tasksQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Include(t => t.CreatedBy)
                    .Where(t => t.ProjectId != null && accessibleProjectIds.Contains(t.ProjectId.Value));
            }
            else
            {
                // Users without valid user info can't see any tasks
                tasksQuery = _context.Tasks.Where(t => false);
            }

            var tasks = await tasksQuery
                .Where(t => t.IsArchived)
                .OrderByDescending(t => t.ArchivedAt)
                .ToListAsync();

            return View(tasks);
        }

        // GET: Tasks/Details/5
        [Authorize(Policy = "Tasks.View")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var task = await _context.Tasks
                .Include(t => t.Performer)
                .ThenInclude(p => p.Grade)
                .Include(t => t.Project)
                .ThenInclude(p => p.Company)
                .Include(t => t.CreatedBy)
                .Include(t => t.UpdatedBy)
                .Include(t => t.Attachments)
                .Include(t => t.HistoryEntries)
                .ThenInclude(h => h.ChangedBy)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            // Check if user has access to this task's project
            if (task.Project != null && !await HasProjectAccess(user, task.Project))
            {
                return Forbid();
            }

            return View(task);
        }

        // GET: Tasks/Create
        [Authorize(Policy = Permissions.CreateTasks)]
        public async Task<IActionResult> Create(int? projectId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole(Roles.SystemAdmin))
            {
                await this.NotifyAuthErrorAsync("شما باید به یک شرکت تخصیص داده شده باشید.");
                return BadRequest();
            }

            // Check if user has access to the specified project
            if (projectId != null && !await HasProjectAccessById(user, projectId))
            {
                await this.NotifyPermissionErrorAsync("شما به این پروژه دسترسی ندارید.");
                return BadRequest();
            }

            var vm = new TaskFormViewModel
            {
                Performers = (await GetPerformersForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList(),
                Projects = (await GetProjectsForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList(),
                Companies = (await GetCompaniesForUser()).Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList(),
                ProjectId = projectId,
                CompanyId = User.IsInRole(Roles.SystemAdmin) ? null : user.CompanyId
            };

            // Check if this is an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_CreateTaskModal", vm);
            }

            return View(vm);
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.CreateTasks)]
        public async Task<IActionResult> Create(TaskFormViewModel vm, IFormFileCollection files, string? initialComment)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.CompanyId == null && !User.IsInRole(Roles.SystemAdmin))
                {
                    await this.NotifyAuthErrorAsync("شما باید به یک شرکت تخصیص داده شده باشید.");
                    return BadRequest();
                }

                if (ModelState.IsValid)
                {
                    // Check if user has access to the specified project
                    if (vm.ProjectId != null && !await HasProjectAccessById(user, vm.ProjectId))
                    {
                        await this.NotifyPermissionErrorAsync("شما به این پروژه دسترسی ندارید.");
                        ModelState.AddModelError("ProjectId", "شما به این پروژه دسترسی ندارید.");
                        vm.Performers = (await GetPerformersForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
                        vm.Projects = (await GetProjectsForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
                        vm.Companies = (await GetCompaniesForUser()).Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            Response.StatusCode = 400;
                            return PartialView("_CreateTaskModal", vm);
                        }
                        return View(vm);
                    }

                    var task = new TaskItem
                    {
                        Title = vm.Title,
                        Description = vm.Description,
                        Status = vm.Status,
                        Priority = vm.Priority,
                        Hours = vm.Hours,
                        OriginalEstimateHours = vm.OriginalEstimateHours,
                        StartAt = vm.StartAt,
                        EndAt = vm.EndAt,
                        PerformerId = vm.PerformerId,
                        ProjectId = vm.ProjectId
                    };

                    // Set company ID based on user role
                    if (User.IsInRole(Roles.SystemAdmin))
                    {
                        // Admin can choose company
                        if (vm.CompanyId.HasValue)
                        {
                            // Validate that the company exists
                            var company = await _context.Companies.FindAsync(vm.CompanyId.Value);
                            if (company == null)
                            {
                                await this.NotifyValidationErrorAsync("شرکت انتخاب شده یافت نشد.");
                                ModelState.AddModelError("CompanyId", "شرکت انتخاب شده یافت نشد.");
                                vm.Performers = (await GetPerformersForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
                                vm.Projects = (await GetProjectsForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
                                vm.Companies = (await GetCompaniesForUser()).Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
                                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                                {
                                    Response.StatusCode = 400;
                                    return PartialView("_CreateTaskModal", vm);
                                }
                                return View(vm);
                            }
                        }
                    }
                    else
                    {
                        // Non-admin users use their assigned company
                        vm.CompanyId = user.CompanyId;
                    }

                    // If user is not admin, ensure the project belongs to user's company
                    if (!User.IsInRole(Roles.SystemAdmin) && task.ProjectId.HasValue)
                    {
                        var project = await _context.Projects.FindAsync(task.ProjectId.Value);
                        if (project?.CompanyId != user.CompanyId)
                        {
                            await this.NotifyPermissionErrorAsync("شما فقط می‌توانید تسک‌هایی در پروژه‌های شرکت خود ایجاد کنید.");
                            ModelState.AddModelError("ProjectId", "شما فقط می‌توانید تسک‌هایی در پروژه‌های شرکت خود ایجاد کنید.");
                            vm.Performers = (await GetPerformersForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
                            vm.Projects = (await GetProjectsForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
                            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                            {
                                Response.StatusCode = 400;
                                return PartialView("_CreateTaskModal", vm);
                            }
                            return View(vm);
                        }
                    }

                    task.CreatedAt = DateTime.Now;
                    task.CreatedById = user.Id;
                    task.IsArchived = false;

                    _context.Add(task);
                    await _context.SaveChangesAsync();

                    // Log the creation
                    var history = new TaskHistory
                    {
                        TaskId = task.Id,
                        Field = "Created",
                        OldValue = "",
                        NewValue = "Task created",
                        ChangedAt = DateTime.Now,
                        ChangedById = user.Id
                    };
                    _context.TaskHistories.Add(history);

                    // Add initial comment if provided
                    if (!string.IsNullOrWhiteSpace(initialComment))
                    {
                        var commentHistory = new TaskHistory
                        {
                            TaskId = task.Id,
                            Field = "Comment",
                            OldValue = "",
                            NewValue = initialComment,
                            ChangedAt = DateTime.Now,
                            ChangedById = user.Id
                        };
                        _context.TaskHistories.Add(commentHistory);
                    }

                    // Process uploaded files
                    if (files != null && files.Count > 0)
                    {
                        foreach (var file in files)
                        {
                            if (file.Length > 0)
                            {
                                // Generate unique filename
                                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                                var filePath = Path.Combine("wwwroot", "uploads", "attachments", fileName);

                                // Ensure directory exists
                                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                                // Save file
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                // Create attachment record
                                var attachment = new TaskAttachment
                                {
                                    TaskId = task.Id,
                                    FileName = file.FileName,
                                    FilePath = fileName,
                                    ContentType = file.ContentType,
                                    FileSize = file.Length,
                                    UploadedAt = DateTime.Now,
                                    UploadedById = user.Id
                                };

                                _context.TaskAttachments.Add(attachment);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();

                    // Send notifications
                    try
                    {
                        // Notify task creator
                        // Corrected parameter order: (controller, NotificationType, title, message)
                        await ScalableNotificationExtensions.NotifyCurrentUserAsync(this, NotificationType.Success, "تسک جدید ایجاد شد", $"تسک '{task.Title}' با موفقیت ایجاد شد.");

                        // Notify performer if different from creator
                        if (task.PerformerId.HasValue && task.PerformerId != user.Id)
                        {
                            await ScalableNotificationExtensions.NotifyUserAsync(
                                this,
                                task.PerformerId.ToString(),
                                "تسک جدید تخصیص یافت",
                                $"تسک '{task.Title}' به شما تخصیص داده شد.",
                                NotificationType.Info,
                                Url.Action("Details", "Tasks", new { id = task.Id }),
                                "مشاهده تسک"
                            );
                        }

                        // Notify project managers if exists
                        if (task.ProjectId.HasValue)
                        {
                            var project = await _context.Projects
                                .Include(p => p.ProjectAccess)
                                .ThenInclude(pa => pa.User)
                                .FirstOrDefaultAsync(p => p.Id == task.ProjectId);

                            if (project != null)
                            {
                                // Get users with Manager role who have access to this project
                                var projectUsers = project.ProjectAccess
                                    .Where(pa => pa.IsActive)
                                    .Select(pa => pa.User)
                                    .ToList();

                                var managerIds = new List<string>();
                                foreach (var projectUser in projectUsers)
                                {
                                    var userRoles = await _userManager.GetRolesAsync(projectUser);
                                    if (userRoles.Contains("Manager"))
                                    {
                                        managerIds.Add(projectUser.Id.ToString());
                                    }
                                }

                                if (managerIds.Any())
                                {
                                    // Notify project managers
                                    await ScalableNotificationExtensions.NotifyUsersAsync(
                                        this,
                                        managerIds,
                                        "تسک جدید در پروژه",
                                        $"تسک جدید '{task.Title}' در پروژه '{project.Name}' ایجاد شد.",
                                        NotificationType.Info,
                                        Url.Action("Details", "Tasks", new { id = task.Id }),
                                        "مشاهده تسک"
                                    );
                                }
                            }
                        }
                    }
                    catch (Exception notificationEx)
                    {
                        // Log notification error but don't fail the main operation
                        // _logger.LogError(notificationEx, "Failed to send task creation notifications");
                    }

                    // AJAX success response
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true });
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await this.NotifyServerErrorAsync($"خطا در ایجاد تسک: {ex.Message}");
                // Log the exception here if you have a logging system
            }

            vm.Performers = (await GetPerformersForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
            vm.Projects = (await GetProjectsForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
            vm.Companies = (await GetCompaniesForUser()).Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();

            // Check if this is an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                Response.StatusCode = 400; // Bad Request for validation errors
                return PartialView("_CreateTaskModal", vm);
            }

            return View(vm);
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var task = await _context.Tasks
                .Include(t => t.Performer)
                .ThenInclude(p => p.Grade)
                .Include(t => t.Project)
                .ThenInclude(p => p.Company)
                .Include(t => t.Attachments)
                .Include(t => t.HistoryEntries)
                .ThenInclude(h => h.ChangedBy)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            // Check if user has access to this task's project
            if (task.Project != null && !await HasProjectAccess(user, task.Project))
            {
                return Forbid();
            }

            var vm = new TaskFormViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                Hours = task.Hours,
                OriginalEstimateHours = task.OriginalEstimateHours,
                StartAt = task.StartAt,
                EndAt = task.EndAt,
                PerformerId = task.PerformerId,
                ProjectId = task.ProjectId,
                CompanyId = task.Project?.CompanyId,
                Performers = (await GetPerformersForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString(), p.Id == task.PerformerId)).ToList(),
                Projects = (await GetProjectsForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString(), p.Id == task.ProjectId)).ToList(),
                Companies = (await GetCompaniesForUser()).Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == task.Project?.CompanyId)).ToList(),
                Attachments = task.Attachments?.ToList() ?? new List<TaskAttachment>(),
                HistoryEntries = task.HistoryEntries?.ToList() ?? new List<TaskHistory>()
            };

            // Check if this is an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView(vm);
            }

            return View(vm);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskFormViewModel vm, string? comment)
        {
            if (id != vm.Id)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole(Roles.SystemAdmin))
            {
                await this.NotifyAuthErrorAsync("شما باید به یک شرکت تخصیص داده شده باشید.");
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingTask = await _context.Tasks.FindAsync(id);
                    if (existingTask == null)
                    {
                        return NotFound();
                    }

                    // If user is not admin, ensure the project belongs to user's company
                    if (!User.IsInRole(Roles.SystemAdmin) && vm.ProjectId.HasValue)
                    {
                        var project = await _context.Projects.FindAsync(vm.ProjectId.Value);
                        if (project?.CompanyId != user.CompanyId)
                        {
                            ModelState.AddModelError("ProjectId", "شما فقط می‌توانید تسک‌هایی در پروژه‌های شرکت خود ویرایش کنید.");
                            vm.Performers = (await GetPerformersForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString(), p.Id == vm.PerformerId)).ToList();
                            vm.Projects = (await GetProjectsForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString(), p.Id == vm.ProjectId)).ToList();

                            return View(vm);
                        }
                    }

                    // Log changes
                    if (existingTask.Title != vm.Title)
                    {
                        await LogTaskChange(id, "Title", existingTask.Title, vm.Title, user.Id);
                    }
                    if (existingTask.Description != vm.Description)
                    {
                        await LogTaskChange(id, "Description", existingTask.Description, vm.Description, user.Id);
                    }
                    if (existingTask.Status != vm.Status)
                    {
                        await LogTaskChange(id, "Status", existingTask.Status.ToString(), vm.Status.ToString(), user.Id);
                    }
                    if (existingTask.Priority != vm.Priority)
                    {
                        await LogTaskChange(id, "Priority", existingTask.Priority.ToString(), vm.Priority.ToString(), user.Id);
                    }
                    if (existingTask.Hours != vm.Hours)
                    {
                        await LogTaskChange(id, "Hours", existingTask.Hours.ToString(), vm.Hours.ToString(), user.Id);
                    }
                    if (existingTask.OriginalEstimateHours != vm.OriginalEstimateHours)
                    {
                        await LogTaskChange(id, "OriginalEstimateHours", existingTask.OriginalEstimateHours?.ToString(), vm.OriginalEstimateHours?.ToString(), user.Id);
                    }
                    if (existingTask.StartAt != vm.StartAt)
                    {
                        await LogTaskChange(id, "StartAt", existingTask.StartAt?.ToString(), vm.StartAt?.ToString(), user.Id);
                    }
                    if (existingTask.EndAt != vm.EndAt)
                    {
                        await LogTaskChange(id, "EndAt", existingTask.EndAt?.ToString(), vm.EndAt?.ToString(), user.Id);
                    }
                    if (existingTask.PerformerId != vm.PerformerId)
                    {
                        await LogTaskChange(id, "PerformerId", existingTask.PerformerId?.ToString(), vm.PerformerId?.ToString(), user.Id);
                    }
                    if (existingTask.ProjectId != vm.ProjectId)
                    {
                        await LogTaskChange(id, "ProjectId", existingTask.ProjectId?.ToString(), vm.ProjectId?.ToString(), user.Id);
                    }

                    existingTask.Title = vm.Title;
                    existingTask.Description = vm.Description;
                    existingTask.Status = vm.Status;
                    existingTask.Priority = vm.Priority;
                    existingTask.Hours = vm.Hours;
                    existingTask.OriginalEstimateHours = vm.OriginalEstimateHours;
                    existingTask.StartAt = vm.StartAt;
                    existingTask.EndAt = vm.EndAt;
                    existingTask.PerformerId = vm.PerformerId;
                    existingTask.ProjectId = vm.ProjectId;
                    existingTask.UpdatedAt = DateTime.Now;
                    existingTask.UpdatedById = user.Id;

                    _context.Update(existingTask);

                    // Add new comment if provided
                    if (!string.IsNullOrWhiteSpace(comment))
                    {
                        var commentHistory = new TaskHistory
                        {
                            TaskId = existingTask.Id,
                            Field = "Comment",
                            OldValue = "",
                            NewValue = comment,
                            ChangedAt = DateTime.Now,
                            ChangedById = user.Id
                        };
                        _context.TaskHistories.Add(commentHistory);
                    }

                    await _context.SaveChangesAsync();

                    // Send notifications
                    try
                    {
                        // Track important changes for notifications
                        bool performerChanged = existingTask.PerformerId != vm.PerformerId;
                        bool statusChanged = existingTask.Status != vm.Status;

                        // Notify task editor
                        await ScalableNotificationExtensions.NotifyCurrentUserAsync(
                            this,
                            "تسک به‌روزرسانی شد",
                            $"تسک '{existingTask.Title}' با موفقیت به‌روزرسانی شد.",
                            NotificationType.Success,
                            Url.Action("Details", "Tasks", new { id = existingTask.Id }),
                            "مشاهده تسک"
                        );

                        // Notify performer about changes
                        if (existingTask.PerformerId.HasValue && existingTask.PerformerId != user.Id)
                        {
                            string message = $"تسک '{existingTask.Title}' به‌روزرسانی شد.";
                            if (performerChanged)
                            {
                                message = $"تسک '{existingTask.Title}' به شما تخصیص داده شد.";
                            }
                            else if (statusChanged)
                            {
                                message = $"وضعیت تسک '{existingTask.Title}' تغییر کرد.";
                            }

                            await ScalableNotificationExtensions.NotifyUserAsync(
                                this,
                                existingTask.PerformerId.ToString(),
                                performerChanged ? "تسک جدید تخصیص یافت" : "تسک به‌روزرسانی شد",
                                message,
                                performerChanged ? NotificationType.Info : NotificationType.Warning,
                                Url.Action("Details", "Tasks", new { id = existingTask.Id }),
                                "مشاهده تسک"
                            );
                        }

                        // Notify project managers
                        if (existingTask.ProjectId.HasValue)
                        {
                            var project = await _context.Projects
                                .Include(p => p.ProjectAccess)
                                .ThenInclude(pa => pa.User)
                                .FirstOrDefaultAsync(p => p.Id == existingTask.ProjectId);

                            if (project != null)
                            {
                                // Get users with Manager role who have access to this project (excluding current user)
                                var projectUsers = project.ProjectAccess
                                    .Where(pa => pa.IsActive && pa.UserId != user.Id)
                                    .Select(pa => pa.User)
                                    .ToList();

                                var managerIds = new List<string>();
                                foreach (var projectUser in projectUsers)
                                {
                                    var userRoles = await _userManager.GetRolesAsync(projectUser);
                                    if (userRoles.Contains("Manager"))
                                    {
                                        managerIds.Add(projectUser.Id.ToString());
                                    }
                                }

                                if (managerIds.Any())
                                {
                                    await ScalableNotificationExtensions.NotifyUsersAsync(
                                        this,
                                        managerIds,
                                        "تسک در پروژه به‌روزرسانی شد",
                                        $"تسک '{existingTask.Title}' در پروژه '{project.Name}' به‌روزرسانی شد.",
                                        NotificationType.Info,
                                        Url.Action("Details", "Tasks", new { id = existingTask.Id }),
                                        "مشاهده تسک"
                                    );
                                }
                            }
                        }
                    }
                    catch (Exception notificationEx)
                    {
                        // Log notification error but don't fail the main operation
                        // _logger.LogError(notificationEx, "Failed to send task update notifications");
                    }

                    // Handle AJAX request
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true });
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskExists(vm.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            vm.Performers = (await GetPerformersForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString(), p.Id == vm.PerformerId)).ToList();
            vm.Projects = (await GetProjectsForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString(), p.Id == vm.ProjectId)).ToList();
            vm.Companies = (await GetCompaniesForUser()).Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == vm.CompanyId)).ToList();

            // Handle AJAX request for validation errors
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                Response.StatusCode = 400; // Bad Request
                return PartialView("_EditTaskModal", vm);
            }

            return View(vm);
        }

        // GET: Tasks/Delete/5
        [Authorize(Policy = Permissions.DeleteTasks)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> taskQuery;

            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can see all tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Where(t => t.Project.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any tasks
                taskQuery = _context.Tasks.Where(t => false);
            }

            var task = await taskQuery.FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Tasks.Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> taskQuery;

            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can see all tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Where(t => t.Project.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any tasks
                taskQuery = _context.Tasks.Where(t => false);
            }

            var task = await taskQuery.FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            // Log the deletion
            await LogTaskChange(id, "Deleted", "Task exists", "Task deleted", user.Id);

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            // Send notification about successful deletion
            try
            {
                await this.NotifyCurrentUserAsync(

                    "تسک حذف شد",
                    $"تسک '{task.Title}' با موفقیت حذف شد.",
                    NotificationType.Success
                );

                // Notify performer if different from current user
                if (task.PerformerId.HasValue && task.PerformerId != user.Id)
                {
                    await this.NotifyUserAsync(

                        task.PerformerId.ToString(),
                        "تسک حذف شد",
                        $"تسک '{task.Title}' که به شما تخصیص داده شده بود حذف شد.",
                        NotificationType.Warning
                    );
                }

                // Notify project managers if exists
                if (task.ProjectId.HasValue)
                {
                    var project = await _context.Projects
                        .Include(p => p.ProjectAccess)
                        .ThenInclude(pa => pa.User)
                        .FirstOrDefaultAsync(p => p.Id == task.ProjectId);

                    if (project != null)
                    {
                        var projectUsers = project.ProjectAccess
                            .Where(pa => pa.IsActive && pa.UserId != user.Id)
                            .Select(pa => pa.User)
                            .ToList();

                        var managerIds = new List<string>();
                        foreach (var projectUser in projectUsers)
                        {
                            var userRoles = await _userManager.GetRolesAsync(projectUser);
                            if (userRoles.Contains("Manager"))
                            {
                                managerIds.Add(projectUser.Id.ToString());
                            }
                        }

                        if (managerIds.Any())
                        {
                            await this.NotifyUsersAsync(
                                managerIds,
                                "تسک در پروژه حذف شد",
                                $"تسک '{task.Title}' در پروژه '{project.Name}' حذف شد.",
                                NotificationType.Warning
                            );
                        }
                    }
                }
            }
            catch (Exception notificationEx)
            {
                // Log notification error but don't fail the main operation
                // _logger.LogError(notificationEx, "Failed to send task deletion notifications");
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Tasks/Archive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Tasks.Edit")]
        public async Task<IActionResult> Archive(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> taskQuery;

            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can see all tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Where(t => t.Project.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any tasks
                taskQuery = _context.Tasks.Where(t => false);
            }

            var task = await taskQuery.FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            task.IsArchived = true;
            task.ArchivedAt = DateTime.Now;
            task.ArchivedBy = user.Id.ToString();

            // Log the change
            var history = new TaskHistory
            {
                TaskId = task.Id,
                Field = "IsArchived",
                OldValue = "False",
                NewValue = "True",
                ChangedAt = DateTime.Now,
                ChangedById = user.Id
            };

            _context.TaskHistories.Add(history);
            _context.Update(task);
            await _context.SaveChangesAsync();

            // Send notification about successful archiving
            try
            {
                await this.NotifyCurrentUserAsync(

                    "تسک آرشیو شد",
                    $"تسک '{task.Title}' با موفقیت آرشیو شد.",
                    NotificationType.Info
                );

                // Notify performer if different from current user
                if (task.PerformerId.HasValue && task.PerformerId != user.Id)
                {
                    await this.NotifyUserAsync(

                        task.PerformerId.ToString(),
                        "تسک آرشیو شد",
                        $"تسک '{task.Title}' که به شما تخصیص داده شده بود آرشیو شد.",
                        NotificationType.Info
                    );
                }
            }
            catch (Exception notificationEx)
            {
                // Log notification error but don't fail the main operation
                // _logger.LogError(notificationEx, "Failed to send task archive notifications");
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Tasks/Unarchive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Tasks.Edit")]
        public async Task<IActionResult> Unarchive(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> taskQuery;

            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can see all tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Where(t => t.Project.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any tasks
                taskQuery = _context.Tasks.Where(t => false);
            }

            var task = await taskQuery.FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            task.IsArchived = false;
            task.ArchivedAt = null;
            task.ArchivedBy = null;

            // Log the change
            var history = new TaskHistory
            {
                TaskId = task.Id,
                Field = "IsArchived",
                OldValue = "True",
                NewValue = "False",
                ChangedAt = DateTime.Now,
                ChangedById = user.Id
            };

            _context.TaskHistories.Add(history);
            _context.Update(task);
            await _context.SaveChangesAsync();

            // Send notification about successful unarchiving
            try
            {
                await this.NotifyCurrentUserAsync(
                    "تسک از آرشیو خارج شد",
                    $"تسک '{task.Title}' با موفقیت از آرشیو خارج شد.",
                    NotificationType.Success
                );

                // Notify performer if different from current user
                if (task.PerformerId.HasValue && task.PerformerId != user.Id)
                {
                    await this.NotifyUserAsync(
                        task.PerformerId.ToString(),
                        "تسک از آرشیو خارج شد",
                        $"تسک '{task.Title}' که به شما تخصیص داده شده بود از آرشیو خارج شد.",
                        NotificationType.Info
                    );
                }
            }
            catch (Exception notificationEx)
            {
                // Log notification error but don't fail the main operation
                // _logger.LogError(notificationEx, "Failed to send task unarchive notifications");
            }

            return RedirectToAction(nameof(Archived));
        }

        // GET: Tasks/QuickView/5
        [Authorize(Policy = "Tasks.View")]
        public async Task<IActionResult> QuickView(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> taskQuery;

            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can see all tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Where(t => t.Project.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any tasks
                taskQuery = _context.Tasks.Where(t => false);
            }

            var task = await taskQuery.FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            ViewBag.Performers = (await GetPerformersForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString()));
            return PartialView("_TaskQuickView", task);
        }

        // POST: Tasks/QuickSave
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Tasks.Edit")]
        public async Task<IActionResult> QuickSave([FromBody] TaskQuickEditRequest model)
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> taskQuery;

            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can see all tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Where(t => t.Project.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any tasks
                taskQuery = _context.Tasks.Where(t => false);
            }

            var task = await taskQuery.FirstOrDefaultAsync(t => t.Id == model.Id);

            if (task == null)
            {
                return NotFound();
            }

            // Log changes
            if (task.Title != model.Title)
            {
                await LogTaskChange(model.Id, "Title", task.Title, model.Title, user.Id);
            }
            if (task.Description != model.Description)
            {
                await LogTaskChange(model.Id, "Description", task.Description, model.Description, user.Id);
            }
            if (task.Hours != model.Hours)
            {
                await LogTaskChange(model.Id, "Hours", task.Hours.ToString(), model.Hours.ToString(), user.Id);
            }

            task.Title = model.Title;
            task.Description = model.Description;
            task.Hours = model.Hours;
            task.UpdatedAt = DateTime.Now;
            task.UpdatedById = user.Id;

            _context.Update(task);
            await _context.SaveChangesAsync();

            // Send notification about quick save
            try
            {
                await ScalableNotificationExtensions.NotifyCurrentUserAsync(
                    this,
                    "تسک به‌روزرسانی شد",
                    $"تسک '{task.Title}' با موفقیت به‌روزرسانی شد.",
                    NotificationType.Success,
                    Url.Action("Details", "Tasks", new { id = task.Id }),
                    "مشاهده تسک"
                );
            }
            catch (Exception notificationEx)
            {
                // Log notification error but don't fail the main operation
                // _logger.LogError(notificationEx, "Failed to send quick save notifications");
            }

            return Json(new { success = true });
        }

        // POST: Tasks/ChangeStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, TaskStatus newStatus)
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> taskQuery;

            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can see all tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Where(t => t.Project.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any tasks
                taskQuery = _context.Tasks.Where(t => false);
            }

            var task = await taskQuery.FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            var oldStatus = task.Status;

            // Check if status is actually changing
            if (oldStatus == newStatus)
            {
                // No change needed, return success
                await this.NotifyValidationErrorAsync("وضعیت تسک تغییری نکرده است.");
                return Json(new { success = true });
            }

            task.Status = newStatus;
            task.UpdatedAt = DateTime.Now;
            task.UpdatedById = user.Id;

            // Log the status change
            await LogTaskChange(id, "Status", oldStatus.ToString(), newStatus.ToString(), user.Id);

            _context.Update(task);
            await _context.SaveChangesAsync();
            // send notify for user

            return Json(new { success = true });
        }

        // POST: Tasks/QuickCreate  (inline board add)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Tasks.Create")]
        public async Task<IActionResult> QuickCreate(string title, TaskStatus status, int? projectId)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                await this.NotifyValidationErrorAsync("عنوان الزامی است");
                return BadRequest();
            }

            var user = await _userManager.GetUserAsync(User);

            var task = new TaskItem
            {
                Title = title.Trim(),
                Status = status,
                ProjectId = projectId,
                CreatedAt = DateTime.Now,
                CreatedById = user?.Id,
                Priority = TaskPriority.Medium,
                Hours = 0
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            // Load navigation props used in card
            await _context.Entry(task).Reference(t => t.Project).LoadAsync();
            await _context.Entry(task).Reference(t => t.Performer).LoadAsync();

            return PartialView("_TaskCard", task);
        }

        // GET: Tasks/CompletedUninvoiced
        [Authorize(Policy = "Tasks.View")]
        public async Task<IActionResult> CompletedUninvoiced()
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> tasksQuery;

            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can see all completed uninvoiced tasks
                tasksQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Where(t => t.Status == TaskStatus.Completed && t.Hours > 0);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's completed uninvoiced tasks
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

        // POST: Tasks/UploadAttachment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Tasks.Edit")]
        public async Task<IActionResult> UploadAttachment(int id, IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> taskQuery;

            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can see all tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Where(t => t.Project.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any tasks
                taskQuery = _context.Tasks.Where(t => false);
            }

            var task = await taskQuery.FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            if (file == null || file.Length == 0)
            {
                await this.NotifyValidationErrorAsync("فایل انتخاب نشده است.");
                return BadRequest();
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine("wwwroot", "uploads", "attachments", fileName);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Create attachment record
            var attachment = new TaskAttachment
            {
                TaskId = id,
                FileName = file.FileName,
                FilePath = fileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.Now,
                UploadedById = user.Id
            };

            _context.TaskAttachments.Add(attachment);

            // Log the attachment upload
            await LogTaskChange(id, "Attachment", "", file.FileName, user.Id);

            await _context.SaveChangesAsync();

            // Send notification about file upload
            try
            {
                await ScalableNotificationExtensions.NotifyCurrentUserAsync(
                    this,
                    "فایل آپلود شد",
                    $"فایل '{file.FileName}' با موفقیت به تسک '{task.Title}' اضافه شد.",
                    NotificationType.Success,
                    Url.Action("Details", "Tasks", new { id = task.Id }),
                    "مشاهده تسک"
                );

                // Notify performer if different from current user
                if (task.PerformerId.HasValue && task.PerformerId != user.Id)
                {
                    await ScalableNotificationExtensions.NotifyUserAsync(
                        this,
                        task.PerformerId.ToString(),
                        "فایل جدید اضافه شد",
                        $"فایل '{file.FileName}' به تسک '{task.Title}' اضافه شد.",
                        NotificationType.Info,
                        Url.Action("Details", "Tasks", new { id = task.Id }),
                        "مشاهده تسک"
                    );
                }
            }
            catch (Exception notificationEx)
            {
                // Log notification error but don't fail the main operation
                // _logger.LogError(notificationEx, "Failed to send file upload notifications");
            }

            // خلاصه نوتیفیکیشن جداگانه حذف شد تا تکرار ایجاد نشود؛ نوتیفیکیشن کامل بالا ارسال شد
            return Json(new { success = true });
        }

        // GET: Tasks/DownloadAttachment/5
        [Authorize(Policy = "Tasks.View")]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskAttachment> attachmentQuery;

            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can see all attachments
                attachmentQuery = _context.TaskAttachments
                    .Include(a => a.Task)
                    .ThenInclude(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(a => a.Task)
                    .ThenInclude(t => t.Project)
                    .ThenInclude(p => p.Company);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's attachments
                attachmentQuery = _context.TaskAttachments
                    .Include(a => a.Task)
                    .ThenInclude(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(a => a.Task)
                    .ThenInclude(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Where(a => a.Task.Project.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any attachments
                attachmentQuery = _context.TaskAttachments.Where(a => false);
            }

            var attachment = await attachmentQuery.FirstOrDefaultAsync(a => a.Id == id);

            if (attachment == null)
            {
                return NotFound();
            }

            var filePath = Path.Combine("wwwroot", "uploads", "attachments", attachment.FilePath);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("فایل یافت نشد.");
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, attachment.ContentType, attachment.FileName);
        }

        private async Task LogTaskChanges(TaskItem oldTask, object newTask, int userId)
        {
            var properties = typeof(TaskItem).GetProperties()
                .Where(p => p.Name != "Id" && p.Name != "CreatedAt" && p.Name != "CreatedById" && p.Name != "UpdatedAt" && p.Name != "UpdatedById");

            foreach (var property in properties)
            {
                var oldValue = property.GetValue(oldTask)?.ToString() ?? "";
                var newValue = property.GetValue(newTask)?.ToString() ?? "";

                if (oldValue != newValue)
                {
                    var history = new TaskHistory
                    {
                        TaskId = oldTask.Id,
                        Field = property.Name,
                        OldValue = oldValue,
                        NewValue = newValue,
                        ChangedAt = DateTime.Now,
                        ChangedById = userId
                    };

                    _context.TaskHistories.Add(history);
                }
            }
        }

        private async Task LogTaskChange(int taskId, string field, string oldValue, string newValue, int userId)
        {
            var history = new TaskHistory
            {
                TaskId = taskId,
                Field = field,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedAt = DateTime.Now,
                ChangedById = userId
            };
            _context.TaskHistories.Add(history);
            await _context.SaveChangesAsync();
        }

        private async Task<List<ApplicationUser>> GetPerformersForUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole(Roles.SystemAdmin))
            {
                return await _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
            }
            else if (user?.CompanyId != null)
            {
                return await _context.Users.Where(u => u.CompanyId == user.CompanyId && u.IsActive).OrderBy(u => u.Name).ToListAsync();
            }
            return new List<ApplicationUser>();
        }

        private async Task<List<Project>> GetProjectsForUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole(Roles.SystemAdmin))
            {
                return await _context.Projects.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            }
            else if (user != null)
            {
                // Non-admin users can only see projects they have explicit access to (within their company)
                var accessibleProjectIds = await _context.ProjectAccess
                    .Where(pa => pa.UserId == user.Id && pa.IsActive)
                    .Select(pa => pa.ProjectId)
                    .ToListAsync();

                return await _context.Projects
                    .Where(p => p.IsActive && accessibleProjectIds.Contains(p.Id))
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            return new List<Project>();
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
                return await _context.Companies.Where(c => c.Id == user.CompanyId && c.IsActive).OrderBy(c => c.Name).ToListAsync();
            }
            return new List<Company>();
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }

        // GET: Tasks/Board/{code}
        [HttpGet("Tasks/Board/{code}")]
        public async Task<IActionResult> Board(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var project = await _context.Projects
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.Code == code);

            if (project == null)
            {
                return NotFound();
            }

            if (!User.IsInRole(Roles.SystemAdmin) && user?.CompanyId != project.CompanyId)
            {
                return Forbid();
            }

            var tasks = await _context.Tasks
                .Include(t => t.Performer).ThenInclude(p => p.Grade)
                .Include(t => t.CreatedBy)
                .Where(t => t.ProjectId == project.Id && !t.IsArchived)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewData["Project"] = project;
            ViewData["ProjectId"] = project.Id;
            ViewData["ProjectCode"] = project.Code;

            // Build lists for create-task modal (performers and projects filtered by company when not admin)
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> performerItems;
            if (User.IsInRole(Roles.SystemAdmin))
            {
                var query = _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.FullName)
                    .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(u.Name, u.Id.ToString()));
                performerItems = await query
                    .ToListAsync();
            }
            else
            {
                performerItems = await _context.Users
                    .Where(u => u.IsActive && u.CompanyId == project.CompanyId)
                    .OrderBy(u => u.FullName)
                    .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(u.Name, u.Id.ToString()))
                    .ToListAsync();
            }

            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> projectItems;
            if (User.IsInRole(Roles.SystemAdmin))
            {
                projectItems = await _context.Projects
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(p.Name, p.Id.ToString()))
                    .ToListAsync();
            }
            else
            {
                projectItems = await _context.Projects
                    .Where(p => p.IsActive && p.CompanyId == project.CompanyId)
                    .OrderBy(p => p.Name)
                    .Select(p => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(p.Name, p.Id.ToString()))
                    .ToListAsync();
            }

            ViewData["ModalPerformers"] = performerItems;
            ViewData["ModalProjects"] = projectItems;

            // Build companies list for modal (admin can see all, others see only their company)
            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> companyItems;
            if (User.IsInRole(Roles.SystemAdmin))
            {
                companyItems = await _context.Companies
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(c.Name, c.Id.ToString()))
                    .ToListAsync();
            }
            else
            {
                companyItems = await _context.Companies
                    .Where(c => c.IsActive && c.Id == project.CompanyId)
                    .OrderBy(c => c.Name)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(c.Name, c.Id.ToString()))
                    .ToListAsync();
            }
            ViewData["ModalCompanies"] = companyItems;

            return View("Index", tasks);
        }

        // Helper methods for project access control
        private async Task<bool> HasProjectAccess(ApplicationUser user, Project project)
        {
            // Admin always has access
            if (User.IsInRole(Roles.SystemAdmin))
                return true;

            // Non-admin users need explicit access to projects
            var hasAccess = await _context.ProjectAccess
                .AnyAsync(pa => pa.ProjectId == project.Id && pa.UserId == user.Id && pa.IsActive);

            return hasAccess;
        }

        private async Task<bool> HasProjectAccessById(ApplicationUser user, int? projectId)
        {
            if (projectId == null)
                return false;

            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                return false;

            return await HasProjectAccess(user, project);
        }

        private string GetStatusDisplayName(TaskStatus status)
        {
            return status switch
            {
                TaskStatus.NotStarted => "شروع نشده",
                TaskStatus.InProgress => "در حال انجام",
                TaskStatus.Completed => "تکمیل شده",
                _ => status.ToString()
            };
        }
    }

    public class TaskQuickEditRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Hours { get; set; }
    }
}