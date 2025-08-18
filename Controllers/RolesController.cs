using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;
using TaskManagementMvc.Models.ViewModels;

namespace TaskManagementMvc.Controllers
{
    [Authorize(Policy = Permissions.ViewRoles)]
    public class RolesController : Controller
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly TaskManagementContext _context;

        public RolesController(RoleManager<ApplicationRole> roleManager, TaskManagementContext context)
        {
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles
                .Select(r => new RoleListViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    UserCount = _context.UserRoles.Count(ur => ur.RoleId == r.Id && ur.IsActive),
                    PermissionCount = _context.RolePermissions.Count(rp => rp.RoleId == r.Id && rp.IsActive)
                })
                .OrderBy(r => r.Name)
                .ToListAsync();

            return View(roles);
        }

        [Authorize(Policy = Permissions.CreateRoles)]
        public async Task<IActionResult> Create()
        {
            var permissions = await GetPermissionsGroupedAsync();
            ViewData["Permissions"] = permissions;
            return View(new CreateRoleViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.CreateRoles)]
        public async Task<IActionResult> Create(CreateRoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                var role = new ApplicationRole
                {
                    Name = model.Name,
                    Description = model.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _roleManager.CreateAsync(role);

                if (result.Succeeded)
                {
                    // Assign permissions
                    if (model.SelectedPermissions.Any())
                    {
                        await AssignPermissionsToRoleAsync(role.Id, model.SelectedPermissions);
                    }

                    TempData["SuccessMessage"] = "نقش با موفقیت ایجاد شد.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            var permissions = await GetPermissionsGroupedAsync();
            ViewData["Permissions"] = permissions;
            return View(model);
        }

        [Authorize(Policy = Permissions.EditRoles)]
        public async Task<IActionResult> Edit(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound();
            }

            var currentPermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == id)
                .Select(rp => new RolePermissionViewModel
                {
                    PermissionId = rp.PermissionId,
                    PermissionName = rp.Permission.Name,
                    Description = rp.Permission.Description,
                    Group = rp.Permission.Group,
                    AssignedAt = rp.AssignedAt,
                    AssignedBy = rp.AssignedBy != null ? rp.AssignedBy.UserName : null,
                    IsActive = rp.IsActive
                })
                .ToListAsync();

            var model = new EditRoleViewModel
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive,
                SelectedPermissions = currentPermissions.Where(cp => cp.IsActive).Select(cp => cp.PermissionId.ToString()).ToList(),
                CurrentPermissions = currentPermissions
            };

            var permissions = await GetPermissionsGroupedAsync();
            ViewData["Permissions"] = permissions;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.EditRoles)]
        public async Task<IActionResult> Edit(int id, EditRoleViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var role = await _roleManager.FindByIdAsync(id.ToString());
                if (role == null)
                {
                    return NotFound();
                }

                role.Name = model.Name;
                role.Description = model.Description;
                role.IsActive = model.IsActive;
                role.UpdatedAt = DateTime.UtcNow;
                role.UpdatedBy = User?.Identity?.Name;

                var result = await _roleManager.UpdateAsync(role);

                if (result.Succeeded)
                {
                    // Update permissions
                    await UpdateRolePermissionsAsync(id.ToString(), model.SelectedPermissions);

                    TempData["SuccessMessage"] = "نقش با موفقیت ویرایش شد.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            var permissions = await GetPermissionsGroupedAsync();
            ViewData["Permissions"] = permissions;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.DeleteRoles)]
        public async Task<IActionResult> Delete(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound();
            }

            // Check if role is a system role (cannot be deleted)
            var systemRoles = new[] { "SystemAdmin", "SystemManager", "CompanyManager" };
            if (systemRoles.Contains(role.Name))
            {
                TempData["ErrorMessage"] = "نقش‌های سیستمی قابل حذف نیستند.";
                return RedirectToAction(nameof(Index));
            }

            // Check if role is assigned to any users
            var userCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == id && ur.IsActive);
            if (userCount > 0)
            {
                TempData["ErrorMessage"] = "این نقش به کاربران تخصیص داده شده و قابل حذف نیست.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _roleManager.DeleteAsync(role);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "نقش با موفقیت حذف شد.";
            }
            else
            {
                TempData["ErrorMessage"] = "خطا در حذف نقش.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound();
            }

            role.IsActive = !role.IsActive;
            role.UpdatedAt = DateTime.UtcNow;
            role.UpdatedBy = User?.Identity?.Name;

            var result = await _roleManager.UpdateAsync(role);

            if (result.Succeeded)
            {
                var message = role.IsActive ? "نقش فعال شد." : "نقش غیرفعال شد.";
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = "خطا در تغییر وضعیت نقش.";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return NotFound();
            }

            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == id)
                .Include(rp => rp.Permission)
                .ToListAsync();

            var users = await _context.UserRoles
                .Where(ur => ur.RoleId == id)
                .Include(ur => ur.User)
                .ToListAsync();

            ViewData["Permissions"] = permissions;
            ViewData["Users"] = users;
            return View(role);
        }

        private async Task<List<PermissionGroupViewModel>> GetPermissionsGroupedAsync()
        {
            var permissions = await _context.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Group)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var grouped = permissions
                .GroupBy(p => p.Group ?? "عمومی")
                .Select(g => new PermissionGroupViewModel
                {
                    Group = g.Key,
                    Permissions = g.Select(p => new PermissionViewModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Code = p.Code,
                        Description = p.Description,
                        Group = p.Group,
                        IsActive = p.IsActive,
                        IsSelected = false
                    }).ToList()
                })
                .ToList();

            return grouped;
        }

        private async Task AssignPermissionsToRoleAsync(int roleId, List<string> permissionIds)
        {
            var rolePermissions = permissionIds.Select(pid => new RolePermission
            {
                RoleId = roleId,
                PermissionId = int.Parse(pid),
                AssignedAt = DateTime.UtcNow,
                AssignedById = null,
                IsActive = true
            }).ToList();

            _context.RolePermissions.AddRange(rolePermissions);
            await _context.SaveChangesAsync();
        }

        private async Task UpdateRolePermissionsAsync(string roleId, List<string> selectedPermissionIds)
        {
            // Get current permissions
            var currentPermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == int.Parse(roleId))
                .ToListAsync();

            // Deactivate all current permissions
            foreach (var cp in currentPermissions)
            {
                cp.IsActive = false;
            }

            // Add new permissions
            var newPermissions = selectedPermissionIds.Select(pid => new RolePermission
            {
                RoleId = int.Parse(roleId),
                PermissionId = int.Parse(pid),
                AssignedAt = DateTime.UtcNow,
                AssignedById = null,
                IsActive = true
            }).ToList();

            _context.RolePermissions.AddRange(newPermissions);
            await _context.SaveChangesAsync();
        }
    }
}