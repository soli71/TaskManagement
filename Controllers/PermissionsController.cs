using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Controllers
{
    [Authorize(Roles = Roles.SystemAdmin)]
    public class PermissionsController : Controller
    {
        private readonly TaskManagementContext _context;

        public PermissionsController(TaskManagementContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var permissions = await _context.Permissions
                .OrderBy(p => p.Group)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var groupedPermissions = permissions
                .GroupBy(p => p.Group ?? "عمومی")
                .OrderBy(g => g.Key)
                .ToList();

            return View(groupedPermissions);
        }

        public IActionResult Create()
        {
            return View(new Permission());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Permission permission)
        {
            if (ModelState.IsValid)
            {
                permission.CreatedAt = DateTime.UtcNow;
                permission.IsActive = true;

                _context.Permissions.Add(permission);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "دسترسی با موفقیت ایجاد شد.";
                return RedirectToAction(nameof(Index));
            }

            return View(permission);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound();
            }

            return View(permission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Permission permission)
        {
            if (id != permission.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Permissions.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.Name = permission.Name;
                    existing.Code = permission.Code;
                    existing.Description = permission.Description;
                    existing.Group = permission.Group;
                    existing.IsActive = permission.IsActive;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "دسترسی با موفقیت ویرایش شد.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PermissionExists(permission.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(permission);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound();
            }

            // Check if permission is assigned to any roles
            var roleCount = await _context.RolePermissions.CountAsync(rp => rp.PermissionId == id && rp.IsActive);
            if (roleCount > 0)
            {
                TempData["ErrorMessage"] = "این دسترسی به نقش‌ها تخصیص داده شده و قابل حذف نیست.";
                return RedirectToAction(nameof(Index));
            }

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "دسترسی با موفقیت حذف شد.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound();
            }

            permission.IsActive = !permission.IsActive;
            await _context.SaveChangesAsync();

            var message = permission.IsActive ? "دسترسی فعال شد." : "دسترسی غیرفعال شد.";
            TempData["SuccessMessage"] = message;

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound();
            }

            var roles = await _context.RolePermissions
                .Where(rp => rp.PermissionId == id)
                .Include(rp => rp.Role)
                .ToListAsync();

            ViewData["Roles"] = roles;
            return View(permission);
        }

        private bool PermissionExists(int id)
        {
            return _context.Permissions.Any(e => e.Id == id);
        }
    }
}
