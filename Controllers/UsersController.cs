using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;
using TaskManagementMvc.Models.ViewModels;

namespace TaskManagementMvc.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly TaskManagementContext _context;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            TaskManagementContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .Where(u => u.IsActive)
                .Select(u => new UserListViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    UserName = u.UserName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    IsActive = u.IsActive,
                    Roles = _userManager.GetRolesAsync(u).Result.ToList()
                })
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(users);
        }

        public async Task<IActionResult> Create()
        {
            var roles = await _roleManager.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewData["Roles"] = roles;
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Notes = model.Notes,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign roles
                    if (model.SelectedRoles.Any())
                    {
                        var roleResult = await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                        if (!roleResult.Succeeded)
                        {
                            foreach (var error in roleResult.Errors)
                            {
                                ModelState.AddModelError("", error.Description);
                            }
                            var rolesList = await _roleManager.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
                            ViewData["Roles"] = rolesList;
                            return View(model);
                        }

                        // Also create UserRole audit records
                        var roleEntities = await _roleManager.Roles.Where(r => model.SelectedRoles.Contains(r.Name!)).ToListAsync();
                        var userIdStr = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                        int? assignedById = int.TryParse(userIdStr, out var uid) ? uid : (int?)null;
                        foreach (var role in roleEntities)
                        {
                            _context.UserRoles.Add(new UserRole
                            {
                                UserId = user.Id,
                                RoleId = role.Id,
                                AssignedAt = DateTime.UtcNow,
                                AssignedById = assignedById,
                                IsActive = true
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "کاربر با موفقیت ایجاد شد.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            var roles = await _roleManager.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewData["Roles"] = roles;
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            var currentRoles = await _context.UserRoles
                .Where(ur => ur.UserId == id && ur.IsActive)
                .Include(ur => ur.Role)
                .Select(ur => new UserRoleViewModel
                {
                    RoleId = ur.RoleId.ToString(),
                    RoleName = ur.Role.Name!,
                    Description = ur.Role.Description ?? string.Empty,
                    AssignedAt = ur.AssignedAt,
                    AssignedBy = ur.AssignedBy != null ? ur.AssignedBy.UserName : null,
                    IsActive = ur.IsActive
                })
                .ToListAsync();

            var model = new EditUserViewModel
            {
                Id = user.Id.ToString(),
                FullName = user.FullName,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                Notes = user.Notes,
                SelectedRoles = userRoles.ToList(),
                CurrentRoles = currentRoles
            };

            ViewData["Roles"] = allRoles;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditUserViewModel model)
        {
            if (id.ToString() != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound();
                }

                user.FullName = model.FullName;
                user.UserName = model.UserName;
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;
                user.IsActive = model.IsActive;
                user.Notes = model.Notes;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // Update roles
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var rolesToRemove = currentRoles.Except(model.SelectedRoles).ToList();
                    var rolesToAdd = model.SelectedRoles.Except(currentRoles).ToList();

                    if (rolesToRemove.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

                        var roleEntitiesToRemove = await _roleManager.Roles.Where(r => rolesToRemove.Contains(r.Name!)).ToListAsync();
                        foreach (var role in roleEntitiesToRemove)
                        {
                            var rels = await _context.UserRoles
                                .Where(ur => ur.UserId == user.Id && ur.RoleId == role.Id && ur.IsActive)
                                .ToListAsync();
                            foreach (var rel in rels)
                            {
                                rel.IsActive = false;
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    if (rolesToAdd.Any())
                    {
                        await _userManager.AddToRolesAsync(user, rolesToAdd);

                        var roleEntitiesToAdd = await _roleManager.Roles.Where(r => rolesToAdd.Contains(r.Name!)).ToListAsync();
                        var userIdStr2 = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                        int? assignedById2 = int.TryParse(userIdStr2, out var uid2) ? uid2 : (int?)null;
                        foreach (var role in roleEntitiesToAdd)
                        {
                            _context.UserRoles.Add(new UserRole
                            {
                                UserId = user.Id,
                                RoleId = role.Id,
                                AssignedAt = DateTime.UtcNow,
                                AssignedById = assignedById2,
                                IsActive = true
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "کاربر با موفقیت ویرایش شد.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            var allRoles = await _roleManager.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            ViewData["Roles"] = allRoles;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            // Soft delete - mark as inactive
            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "کاربر با موفقیت غیرفعال شد.";
            }
            else
            {
                TempData["ErrorMessage"] = "خطا در غیرفعال کردن کاربر.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                var message = user.IsActive ? "کاربر فعال شد." : "کاربر غیرفعال شد.";
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = "خطا در تغییر وضعیت کاربر.";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ChangePassword(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            var model = new ChangePasswordViewModel
            {
                UserId = user.Id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId.ToString());
                if (user == null)
                {
                    return NotFound();
                }

                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "رمز عبور با موفقیت تغییر یافت.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var roles = await _roleManager.Roles
                .Where(r => userRoles.Contains(r.Name!))
                .ToListAsync();

            ViewData["UserRoles"] = roles;
            return View(user);
        }
    }
}