using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;
using TaskManagementMvc.Models.ViewModels;
using TaskManagementMvc.Services;

namespace TaskManagementMvc.Controllers
{
    [Authorize(Policy = Permissions.ViewUsers)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly TaskManagementContext _context;
        private readonly ProjectAccessService _projectAccessService;
        private readonly IScalableNotificationService _notificationService;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            TaskManagementContext context,
            ProjectAccessService projectAccessService,
            IScalableNotificationService notificationService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _projectAccessService = projectAccessService;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            IQueryable<ApplicationUser> usersQuery = _userManager.Users
                .Where(u => u.IsActive)
                .Include(u => u.Company);
            
            // If not admin, show only company users
            if (!User.IsInRole(Roles.SystemAdmin))
            {
                if (currentUser?.CompanyId == null)
                {
                    return Forbid();
                }
                usersQuery = usersQuery.Where(u => u.CompanyId == currentUser.CompanyId);
            }
            // Load users first
            var userEntities = await usersQuery
                .OrderBy(u => u.FullName)
                .ToListAsync();

            // Batch load active roles for these users (avoids N+1 and invalid translation)
            var userIds = userEntities.Select(u => u.Id).ToList();
            var userRoleLinks = await _context.UserRoles
                .Where(ur => ur.IsActive && userIds.Contains(ur.UserId))
                .Include(ur => ur.Role)
                .ToListAsync();

            var rolesByUser = userRoleLinks
                .GroupBy(ur => ur.UserId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var roleNames = g
                            .Select(r => r.Role.Name ?? string.Empty)
                            .Where(n => !string.IsNullOrEmpty(n))
                            .Distinct()
                            .OrderBy(n => n)
                            .ToList();
                        return roleNames;
                    }
                );

            var list = userEntities.Select(u => new UserListViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber,
                CompanyName = u.Company?.Name,
                IsActive = u.IsActive,
                Roles = rolesByUser.TryGetValue(u.Id, out var roles) ? roles : new List<string>()
            }).ToList();

            return View(list);
        }

        [Authorize(Policy = Permissions.CreateUsers)]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            // Check if user can create users
            if (!User.IsInRole(Roles.SystemAdmin) && !User.IsInRole(Roles.CompanyManager))
            {
                return Forbid();
            }
            var model = new CreateUserViewModel();
            if (!User.IsInRole(Roles.SystemAdmin) && !User.IsInRole("SystemManager"))
            {
                model.CompanyId = currentUser?.CompanyId;
            }

            await PopulateCreateViewModel(model, currentUser);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.CreateUsers)]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                // Check if user can create users
                if (!User.IsInRole(Roles.SystemAdmin) && !User.IsInRole(Roles.CompanyManager))
                {
                    return Forbid();
                }

                // Ensure managers can only create users for their own company
                if (!User.IsInRole(Roles.SystemAdmin))
                {
                    if (currentUser?.CompanyId == null || model.CompanyId != currentUser.CompanyId)
                    {
                        ModelState.AddModelError("", "شما فقط می‌توانید برای سازمان خود کاربر ایجاد کنید.");
                        await LoadCreateViewData(currentUser);
                        return View(model);
                    }
                }

                if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Notes = model.Notes,
                    CompanyId = model.CompanyId,
                    GradeId = model.GradeId,
                    IbanNumber = model.IbanNumber,
                    CardNumber = model.CardNumber,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Validate role assignments based on user's level
                    if (model.SelectedRoles.Any())
                    {
                        var currentUserRole = GetCurrentUserHighestRole();
                        var forbiddenRoles = model.SelectedRoles.Where(r => !Roles.CanAssignRole(currentUserRole, r)).ToList();
                        if (forbiddenRoles.Any())
                        {
                            ModelState.AddModelError("", $"شما اجازه تخصیص نقش‌های {string.Join(", ", forbiddenRoles)} را ندارید.");
                            await LoadCreateViewData(currentUser);
                            return View(model);
                        }
                    }

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

                        // اگر نقش Manager اضافه شده، دسترسی به تمام پروژه‌های شرکت بده
                        if (model.SelectedRoles.Contains("CompanyManager") && user.CompanyId.HasValue)
                        {
                            await _projectAccessService.GrantAllCompanyProjectsAccessToManager(
                                user.Id, 
                                user.CompanyId.Value, 
                                assignedById
                            );
                        }
                    }

                    TempData["SuccessMessage"] = "کاربر با موفقیت ایجاد شد.";
                    
                    // Send success notification
                    try
                    {
                        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrEmpty(currentUserId))
                        {
                            await _notificationService.SendToUserAsync(
                                currentUserId,
                                new ScalableNotificationMessage
                                {
                                    Type = "success",
                                    Title = "کاربر جدید ایجاد شد",
                                    Message = $"کاربر '{user.FullName}' با موفقیت ایجاد شد.",
                                    ActionUrl = Url.Action("Details", new { id = user.Id }),
                                    ActionText = "مشاهده کاربر"
                                }
                            );
                        }
                    }
                    catch { /* Ignore notification errors */ }
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            await PopulateCreateViewModel(model, currentUser);
            return View(model);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "خطا در ایجاد کاربر: " + ex.Message);
            
            // Send error notification
            try
            {
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    await _notificationService.SendToUserAsync(
                        currentUserId,
                        new ScalableNotificationMessage
                        {
                            Type = "error",
                            Title = "خطا در ایجاد کاربر",
                            Message = $"خطا در ایجاد کاربر '{model.FullName}': {ex.Message}",
                            ActionUrl = Url.Action("Create"),
                            ActionText = "تلاش مجدد"
                        }
                    );
                }
            }
            catch { /* Ignore notification errors */ }
            
            var currentUser = await _userManager.GetUserAsync(User);
            await PopulateCreateViewModel(model, currentUser);
            return View(model);
        }
    }

        [Authorize(Policy = Permissions.EditUsers)]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            // محدودیت: مدیر شرکت فقط کاربران شرکت خودش را ببیند/ویرایش کند
            if (!User.IsInRole(Roles.SystemAdmin))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.CompanyId == null || currentUser.CompanyId != user.CompanyId)
                {
                    return Forbid();
                }
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            // Filter roles based on user's level
            var currentUserRole = GetCurrentUserHighestRole();
            var assignableRoles = Roles.GetAssignableRoles(currentUserRole);
            allRoles = allRoles.Where(r => assignableRoles.Contains(r.Name!)).ToList();

            var currentRoles = await _context.UserRoles
                .Where(ur => ur.UserId == id && ur.IsActive)
                .Include(ur => ur.Role)
                .Select(ur => new UserRoleViewModel
                {
                    RoleId = ur.RoleId,
                    RoleName = ur.Role.Name!,
                    Description = ur.Role.Description ?? string.Empty,
                    AssignedAt = ur.AssignedAt,
                    AssignedBy = ur.AssignedBy != null ? ur.AssignedBy.UserName : null,
                    IsActive = ur.IsActive
                })
                .ToListAsync();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                Notes = user.Notes,
                CompanyId = user.CompanyId,
                GradeId = user.GradeId,
                IbanNumber = user.IbanNumber,
                CardNumber = user.CardNumber,
                SelectedRoles = userRoles.ToList(),
                CurrentRoles = currentRoles
            };

            // Load dropdown data into ViewModel
            await LoadEditViewData(model);
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.EditUsers)]
        public async Task<IActionResult> Edit(int id, EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var currentUserId = int.Parse(User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                
                // Prevent managers from editing their own roles
                if (!User.IsInRole(Roles.SystemAdmin) && model.Id == currentUserId)
                {
                    ModelState.AddModelError("", "شما نمی‌توانید نقش‌های خود را تغییر دهید.");
                    await LoadEditViewData(model);
                    return View(model);
                }

                // Validate role assignments based on user's level
                if (model.SelectedRoles.Any())
                {
                    var currentUserRole = GetCurrentUserHighestRole();
                    var forbiddenRoles = model.SelectedRoles.Where(r => !Roles.CanAssignRole(currentUserRole, r)).ToList();
                    if (forbiddenRoles.Any())
                    {
                        ModelState.AddModelError("", $"شما اجازه تخصیص نقش‌های {string.Join(", ", forbiddenRoles)} را ندارید.");
                        await LoadEditViewData(model);
                        return View(model);
                    }
                }

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound();
                }

                // محدودیت شرکت برای غیر ادمین
                if (!User.IsInRole(Roles.SystemAdmin))
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser?.CompanyId == null || currentUser.CompanyId != user.CompanyId)
                    {
                        return Forbid();
                    }
                    // جلوگیری از تغییر شرکت توسط مدیر شرکت
                    model.CompanyId = user.CompanyId;
                }

                // Update scalar fields
                user.FullName = model.FullName;
                user.UserName = model.UserName;
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;
                if (Request.Form.ContainsKey("IsActive")) // جلوگیری از غیرفعال شدن ناخواسته
                {
                    user.IsActive = model.IsActive;
                }
                user.Notes = model.Notes;
                user.CompanyId = model.CompanyId;
                user.GradeId = model.GradeId;
                user.IbanNumber = model.IbanNumber;
                user.CardNumber = model.CardNumber;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // Update roles only if the roles field was posted, to avoid clearing accidentally
                    var rolesFieldPosted = Request.Form.ContainsKey("SelectedRoles");
                    if (model.SelectedRoles == null)
                        model.SelectedRoles = new List<string>();
                    if (rolesFieldPosted)
                    {
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

                            // اگر نقش Manager حذف شده، دسترسی‌های پروژه را حذف کن
                            if (rolesToRemove.Contains("CompanyManager") && user.CompanyId.HasValue)
                            {
                                var executorUserId = int.Parse(User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
                                await _projectAccessService.RevokeAllCompanyProjectsAccessFromUser(
                                    user.Id, 
                                    user.CompanyId.Value, 
                                    executorUserId
                                );
                            }
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

                            // اگر نقش Manager اضافه شده، دسترسی به تمام پروژه‌های شرکت بده
                            if (rolesToAdd.Contains("CompanyManager") && user.CompanyId.HasValue)
                            {
                                await _projectAccessService.GrantAllCompanyProjectsAccessToManager(
                                    user.Id, 
                                    user.CompanyId.Value, 
                                    assignedById2
                                );
                            }
                        }
                    } // end roles update block

                    TempData["SuccessMessage"] = "کاربر با موفقیت ویرایش شد.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            // Re-populate the ViewModel data properly
            await LoadEditViewData(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.DeleteUsers)]
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

        // GET: Users/ProjectAccess/5
        public async Task<IActionResult> ProjectAccess(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound();
            }

            var projectAccess = await _context.ProjectAccess
                .Where(pa => pa.UserId == id)
                .Include(pa => pa.Project)
                .ThenInclude(p => p.Company)
                .Include(pa => pa.GrantedBy)
                .OrderByDescending(pa => pa.GrantedAt)
                .ToListAsync();

            ViewData["UserName"] = user.FullName;
            return View(projectAccess);
        }

        private async Task LoadCreateViewData(ApplicationUser? currentUser)
        {
            var roles = await _roleManager.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            // Filter roles based on user's level
            var currentUserRole = GetCurrentUserHighestRole();
            var assignableRoles = Roles.GetAssignableRoles(currentUserRole);
            roles = roles.Where(r => assignableRoles.Contains(r.Name!)).ToList();

            ViewData["Roles"] = roles;
            
            // Get grades for selection
            var grades = await _context.Grades
                .Where(g => g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync();
            ViewData["Grades"] = grades;
            
            // Get companies for selection
            if (User.IsInRole(Roles.SystemAdmin))
            {
                var companies = await _context.Companies
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                ViewData["Companies"] = companies;
            }
            else
            {
                // For managers, set their company only
                ViewData["Companies"] = new List<Company>();
                if (currentUser?.CompanyId != null)
                {
                    var userCompany = await _context.Companies.FindAsync(currentUser.CompanyId);
                    if (userCompany != null)
                    {
                        ViewData["Companies"] = new List<Company> { userCompany };
                    }
                }
            }
        }

        // نسخه جدید برای پر کردن ViewModel ایجاد کاربر (استفاده به جای ViewData)
        private async Task PopulateCreateViewModel(CreateUserViewModel model, ApplicationUser? currentUser)
        {
            var roles = await _roleManager.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            var currentUserRole = GetCurrentUserHighestRole();
            var assignableRoles = Roles.GetAssignableRoles(currentUserRole);
            model.Roles = roles.Where(r => assignableRoles.Contains(r.Name!)).ToList();

            // Grades
            model.Grades = await _context.Grades
                .Where(g => g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync();

            if (User.IsInRole(Roles.SystemAdmin))
            {
                model.Companies = await _context.Companies
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }
            else
            {
                model.Companies = new List<Company>();
                if (currentUser?.CompanyId != null)
                {
                    var userCompany = await _context.Companies.FindAsync(currentUser.CompanyId);
                    if (userCompany != null)
                    {
                        model.Companies.Add(userCompany);
                    }
                }
            }
        }

        private async Task LoadEditViewData(EditUserViewModel model)
        {
            var roles = await _roleManager.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            // Filter roles based on user's level
            var currentUserRole = GetCurrentUserHighestRole();
            var assignableRoles = Roles.GetAssignableRoles(currentUserRole);
            model.Roles = roles.Where(r => assignableRoles.Contains(r.Name!)).ToList();
            
            // Get companies for admin selection
            model.Companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            
            // Get grades for selection
            model.Grades = await _context.Grades
                .Where(g => g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        private async Task LoadEditViewData(int userId)
        {
            var roles = await _roleManager.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();

            // Filter roles based on user's level
            var currentUserRole = GetCurrentUserHighestRole();
            var assignableRoles = Roles.GetAssignableRoles(currentUserRole);
            roles = roles.Where(r => assignableRoles.Contains(r.Name!)).ToList();

            ViewData["Roles"] = roles;
            
            // Get companies for admin selection
            var companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewData["Companies"] = companies;
            
            // Get grades for selection
            var grades = await _context.Grades
                .Where(g => g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync();
            ViewData["Grades"] = grades;
        }

        /// <summary>
        /// Get the highest role of the current user
        /// </summary>
        /// <returns>The highest role name</returns>
        private string GetCurrentUserHighestRole()
        {
            if (User.IsInRole(Roles.SystemAdmin)) return Roles.SystemAdmin;
            if (User.IsInRole(Roles.CompanyManager)) return Roles.CompanyManager;
            if (User.IsInRole(Roles.ProjectManager)) return Roles.ProjectManager;
            if (User.IsInRole(Roles.Employee)) return Roles.Employee;
            return string.Empty;
        }
    }
}