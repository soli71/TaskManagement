using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;
using TaskManagementMvc.Models.ViewModels;
using TaskStatus = TaskManagementMvc.Models.TaskStatus;

namespace TaskManagementMvc.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly TaskManagementContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TasksController(TaskManagementContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Tasks
        [Authorize(Policy = "Tasks.View")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> tasksQuery;

            if (User.IsInRole("Admin"))
            {
                // Admin can see all tasks
                tasksQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Include(t => t.CreatedBy);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's tasks
                tasksQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Include(t => t.CreatedBy)
                    .Where(t => t.Project.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any tasks
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

            if (User.IsInRole("Admin"))
            {
                // Admin can see all archived tasks
                tasksQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Include(t => t.CreatedBy);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's archived tasks
                tasksQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Include(t => t.CreatedBy)
                    .Where(t => t.Project.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any tasks
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
            IQueryable<TaskItem> taskQuery;

            if (User.IsInRole("Admin"))
            {
                // Admin can see all tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Include(t => t.CreatedBy)
                    .Include(t => t.UpdatedBy)
                    .Include(t => t.Attachments)
                    .Include(t => t.HistoryEntries)
                    .ThenInclude(h => h.ChangedBy);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Include(t => t.CreatedBy)
                    .Include(t => t.UpdatedBy)
                    .Include(t => t.Attachments)
                    .Include(t => t.HistoryEntries)
                    .ThenInclude(h => h.ChangedBy)
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

        // GET: Tasks/Create
        [Authorize(Policy = "Tasks.Create")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole("Admin"))
            {
                return Forbid("شما باید به یک شرکت تخصیص داده شده باشید.");
            }

            var vm = new TaskFormViewModel
            {
                Performers = (await GetPerformersForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList(),
                Projects = (await GetProjectsForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList()
            };
            return View(vm);
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Tasks.Create")]
        public async Task<IActionResult> Create(TaskFormViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole("Admin"))
            {
                return Forbid("شما باید به یک شرکت تخصیص داده شده باشید.");
            }

            if (ModelState.IsValid)
            {
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

                // If user is not admin, ensure the project belongs to user's company
                if (!User.IsInRole("Admin") && task.ProjectId.HasValue)
                {
                    var project = await _context.Projects.FindAsync(task.ProjectId.Value);
                    if (project?.CompanyId != user.CompanyId)
                    {
                        ModelState.AddModelError("ProjectId", "شما فقط می‌توانید تسک‌هایی در پروژه‌های شرکت خود ایجاد کنید.");
                        vm.Performers = (await GetPerformersForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
                        vm.Projects = (await GetProjectsForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
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
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "تسک با موفقیت ایجاد شد.";
                return RedirectToAction(nameof(Index));
            }

            vm.Performers = (await GetPerformersForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
            vm.Projects = (await GetProjectsForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToList();
            return View(vm);
        }

        // GET: Tasks/Edit/5
        [Authorize(Policy = "Tasks.Edit")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> taskQuery;

            if (User.IsInRole("Admin"))
            {
                // Admin can see all tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Include(t => t.Attachments)
                    .Include(t => t.HistoryEntries)
                    .ThenInclude(h => h.ChangedBy);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's tasks
                taskQuery = _context.Tasks
                    .Include(t => t.Performer)
                    .ThenInclude(p => p.Grade)
                    .Include(t => t.Project)
                    .ThenInclude(p => p.Company)
                    .Include(t => t.Attachments)
                    .Include(t => t.HistoryEntries)
                    .ThenInclude(h => h.ChangedBy)
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
                Performers = (await GetPerformersForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString(), p.Id == task.PerformerId)).ToList(),
                Projects = (await GetProjectsForUser()).Select(p => new SelectListItem(p.Name, p.Id.ToString(), p.Id == task.ProjectId)).ToList()
            };

            return View(vm);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Tasks.Edit")]
        public async Task<IActionResult> Edit(int id, TaskFormViewModel vm)
        {
            if (id != vm.Id)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole("Admin"))
            {
                return Forbid("شما باید به یک شرکت تخصیص داده شده باشید.");
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
                    if (!User.IsInRole("Admin") && vm.ProjectId.HasValue)
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
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "تسک با موفقیت به‌روزرسانی شد.";
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
            return View(vm);
        }

        // GET: Tasks/Delete/5
        [Authorize(Policy = "Tasks.Delete")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> taskQuery;

            if (User.IsInRole("Admin"))
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

            if (User.IsInRole("Admin"))
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

            TempData["SuccessMessage"] = "تسک با موفقیت حذف شد.";
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

            if (User.IsInRole("Admin"))
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

            TempData["SuccessMessage"] = "تسک با موفقیت آرشیو شد.";
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

            if (User.IsInRole("Admin"))
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

            TempData["SuccessMessage"] = "تسک با موفقیت از آرشیو خارج شد.";
            return RedirectToAction(nameof(Archived));
        }

        // GET: Tasks/QuickView/5
        [Authorize(Policy = "Tasks.View")]
        public async Task<IActionResult> QuickView(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> taskQuery;

            if (User.IsInRole("Admin"))
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

            if (User.IsInRole("Admin"))
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

            return Json(new { success = true, message = "تسک با موفقیت به‌روزرسانی شد." });
        }

        // POST: Tasks/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Tasks.Edit")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> taskQuery;

            if (User.IsInRole("Admin"))
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
            task.Status = request.Status;
            task.UpdatedAt = DateTime.Now;
            task.UpdatedById = user.Id;

            // Log the status change
            await LogTaskChange(id, "Status", oldStatus.ToString(), request.Status.ToString(), user.Id);

            _context.Update(task);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "وضعیت تسک با موفقیت به‌روزرسانی شد." });
        }

        // POST: Tasks/ChangeStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "Tasks.Edit")]
        public async Task<IActionResult> ChangeStatus(int id, TaskStatus newStatus)
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> taskQuery;

            if (User.IsInRole("Admin"))
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
            task.Status = newStatus;
            task.UpdatedAt = DateTime.Now;
            task.UpdatedById = user.Id;

            // Log the status change
            await LogTaskChange(id, "Status", oldStatus.ToString(), newStatus.ToString(), user.Id);

            _context.Update(task);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "وضعیت تسک با موفقیت تغییر کرد." });
        }

        // GET: Tasks/CompletedUninvoiced
        [Authorize(Policy = "Tasks.View")]
        public async Task<IActionResult> CompletedUninvoiced()
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskItem> tasksQuery;

            if (User.IsInRole("Admin"))
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

            if (User.IsInRole("Admin"))
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
                return BadRequest("فایل انتخاب نشده است.");
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

            return Json(new { success = true, message = "فایل با موفقیت آپلود شد." });
        }

        // GET: Tasks/DownloadAttachment/5
        [Authorize(Policy = "Tasks.View")]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<TaskAttachment> attachmentQuery;

            if (User.IsInRole("Admin"))
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

        private async Task<List<Performer>> GetPerformersForUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Admin"))
            {
                return await _context.Performers.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
            }
            else if (user?.CompanyId != null)
            {
                return await _context.Performers.Where(p => p.CompanyId == user.CompanyId && p.IsActive).OrderBy(p => p.Name).ToListAsync();
            }
            return new List<Performer>();
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

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }

    public class TaskQuickEditRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double Hours { get; set; }
    }

    public class UpdateStatusRequest
    {
        public TaskStatus Status { get; set; }
    }
}