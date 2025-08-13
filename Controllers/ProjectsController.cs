using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;
using TaskManagementMvc.Models.ViewModels;

namespace TaskManagementMvc.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly TaskManagementContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectsController(TaskManagementContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            IQueryable<Project> projectsQuery;

            if (User.IsInRole("Admin"))
            {
                // Admin can see all projects
                projectsQuery = _context.Projects
                    .Include(p => p.Company)
                    .Include(p => p.ProjectManager)
                    .Include(p => p.Tasks);
            }
            else if (user?.CompanyId != null)
            {
                // Company users can only see their company's projects
                projectsQuery = _context.Projects
                    .Include(p => p.Company)
                    .Include(p => p.ProjectManager)
                    .Include(p => p.Tasks)
                    .Where(p => p.CompanyId == user.CompanyId);
            }
            else
            {
                // Users without company can't see any projects
                projectsQuery = _context.Projects.Where(p => false);
            }

            var projects = await projectsQuery
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(projects);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var project = await _context.Projects
                .Include(p => p.Company)
                .Include(p => p.ProjectManager)
                .Include(p => p.Tasks)
                .ThenInclude(t => t.Performer)
                .ThenInclude(pf => pf.Grade)
                .Include(p => p.Invoices)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            // Check if user has access to this project
            if (!User.IsInRole("Admin") && user?.CompanyId != project.CompanyId)
            {
                return Forbid();
            }

            return View(project);
        }

        // GET: Projects/Board/5
        public async Task<IActionResult> Board(int? id)
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

            if (!User.IsInRole("Admin") && user?.CompanyId != project.CompanyId)
            {
                return Forbid();
            }

            var tasks = await _context.Tasks
                .Include(t => t.Performer).ThenInclude(p => p.Grade)
                .Include(t => t.CreatedBy)
                .Where(t => t.ProjectId == id && !t.IsArchived)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewData["Project"] = project;
            return View(tasks);
        }

        // GET: Projects/Create
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole("Admin"))
            {
                return Forbid("شما باید به یک شرکت تخصیص داده شده باشید.");
            }

            var vm = new ProjectFormViewModel
            {
                Companies = await GetCompaniesForUser(),
                ProjectManagers = await GetProjectManagersForUser(),
                CompanyId = user?.CompanyId ?? 0
            };
            return View(vm);
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectFormViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole("Admin"))
            {
                return Forbid("شما باید به یک شرکت تخصیص داده شده باشید.");
            }

            if (ModelState.IsValid)
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
                    CompanyId = User.IsInRole("Admin") ? vm.CompanyId : user!.CompanyId!.Value,
                    ProjectManagerId = vm.ProjectManagerId,
                    CreatedAt = DateTime.Now,
                    CreatedBy = User.Identity?.Name,
                    Status = ProjectStatus.Active,
                    IsActive = true
                };

                _context.Add(project);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "پروژه با موفقیت ایجاد شد.";
                return RedirectToAction(nameof(Index));
            }

            vm.Companies = await GetCompaniesForUser();
            vm.ProjectManagers = await GetProjectManagersForUser();
            return View(vm);
        }

        // GET: Projects/Edit/5
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
            if (!User.IsInRole("Admin") && user?.CompanyId != project.CompanyId)
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
            return View(vm);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
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
            if (!User.IsInRole("Admin") && user?.CompanyId != existingProject.CompanyId)
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
                    existingProject.CompanyId = User.IsInRole("Admin") ? vm.CompanyId : user!.CompanyId!.Value;
                    existingProject.ProjectManagerId = vm.ProjectManagerId;
                    existingProject.UpdatedAt = DateTime.Now;
                    existingProject.UpdatedBy = User.Identity?.Name;

                    _context.Update(existingProject);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "پروژه با موفقیت به‌روزرسانی شد.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
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
            if (!User.IsInRole("Admin") && user?.CompanyId != project.CompanyId)
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
            if (!User.IsInRole("Admin") && user?.CompanyId != project.CompanyId)
            {
                return Forbid();
            }

            return View(project);
        }

        private async Task<List<Company>> GetCompaniesForUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Admin"))
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
            if (User.IsInRole("Admin"))
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
    }
}