using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;
using TaskManagementMvc.Models.ViewModels;

namespace TaskManagementMvc.Controllers
{
    public partial class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TaskManagementContext _context;
    private readonly RoleManager<ApplicationRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TaskManagementContext context,
            RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Accept username or email
                ApplicationUser? user = null;
                if (!string.IsNullOrWhiteSpace(model.UserName) && model.UserName.Contains('@'))
                {
                    user = await _userManager.FindByEmailAsync(model.UserName);
                }
                if (user == null)
                {
                    user = await _userManager.FindByNameAsync(model.UserName);
                }

                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        user, model.Password, model.RememberMe, lockoutOnFailure: false);

                    if (result.Succeeded)
                    {
                        // Update last login time
                        user.LastLoginAt = DateTime.UtcNow;
                        await _userManager.UpdateAsync(user);

                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }

                ModelState.AddModelError(string.Empty, "نام کاربری/ایمیل یا رمز عبور اشتباه است.");
                return View(model);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewData["UserRoles"] = roles;

            return View(user);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
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
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                if (result.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    TempData["SuccessMessage"] = "رمز عبور با موفقیت تغییر یافت.";
                    return RedirectToAction(nameof(Profile));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }
    }

    // Registration wizard view models
    public class RegisterStep1ViewModel
    {
        [Required(ErrorMessage = "نام و نام خانوادگی الزامی است")]
        [Display(Name = "نام و نام خانوادگی")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ایمیل الزامی است")]
        [EmailAddress(ErrorMessage = "ایمیل نامعتبر است")]
        [Display(Name = "ایمیل")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تکرار رمز عبور الزامی است")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "رمز عبور و تکرار آن یکسان نیستند")]
        [Display(Name = "تکرار رمز عبور")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class RegisterStep2ViewModel
    {
        [Required(ErrorMessage = "نام شرکت الزامی است")]
        [Display(Name = "نام شرکت")]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        [Display(Name = "تلفن")]
        public string? Phone { get; set; }
    }

    public class RegisterPayload
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyDescription { get; set; }
        public string? CompanyPhone { get; set; }
    }

    // Unified registration view model (single page)
    public class RegisterViewModel
    {
        // User info
        [Required(ErrorMessage = "نام و نام خانوادگی الزامی است")]
        [Display(Name = "نام و نام خانوادگی")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ایمیل الزامی است")]
        [EmailAddress(ErrorMessage = "ایمیل نامعتبر است")]
        [Display(Name = "ایمیل")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تکرار رمز عبور الزامی است")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "رمز عبور و تکرار آن یکسان نیستند")]
        [Display(Name = "تکرار رمز عبور")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Company info
        [Required(ErrorMessage = "نام شرکت الزامی است")]
        [Display(Name = "نام شرکت")]
        public string CompanyName { get; set; } = string.Empty;

        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        [Display(Name = "تلفن")]
        public string? Phone { get; set; }
    }

    [AllowAnonymous]
    public partial class AccountController
    {
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Duplicate email check
            var existingByEmail = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (existingByEmail != null)
            {
                ModelState.AddModelError("Email", "این ایمیل قبلاً ثبت شده است.");
                return View(model);
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var company = new Company
                {
                    Name = model.CompanyName.Trim(),
                    Description = model.Description,
                    Phone = model.Phone,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                var user = new ApplicationUser
                {
                    UserName = model.Email.Trim(),
                    Email = model.Email.Trim(),
                    FullName = model.FullName.Trim(),
                    CompanyId = company.Id,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };
                var createRes = await _userManager.CreateAsync(user, model.Password);
                if (!createRes.Succeeded)
                {
                    foreach (var err in createRes.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);

                    await tx.RollbackAsync();
                    return View(model);
                }

                // Ensure CompanyManager role exists and is assigned
                var cmRole = await _roleManager.FindByNameAsync(Roles.CompanyManager);
                if (cmRole == null)
                {
                    var createRoleRes = await _roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = Roles.CompanyManager,
                        Description = "مدیر شرکت",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    if (!createRoleRes.Succeeded)
                    {
                        foreach (var err in createRoleRes.Errors)
                            ModelState.AddModelError(string.Empty, err.Description);

                        await tx.RollbackAsync();
                        return View(model);
                    }
                    cmRole = await _roleManager.FindByNameAsync(Roles.CompanyManager);
                }

                var addToRoleRes = await _userManager.AddToRoleAsync(user, Roles.CompanyManager);
                if (!addToRoleRes.Succeeded)
                {
                    foreach (var err in addToRoleRes.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);

                    await tx.RollbackAsync();
                    return View(model);
                }

                if (cmRole != null)
                {
                    var hasLink = _context.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == cmRole.Id && ur.IsActive);
                    if (!hasLink)
                    {
                        _context.UserRoles.Add(new UserRole
                        {
                            UserId = user.Id,
                            RoleId = cmRole.Id,
                            AssignedAt = DateTime.UtcNow,
                            AssignedById = null,
                            IsActive = true,
                            Notes = "Assigned on registration"
                        });
                        await _context.SaveChangesAsync();
                    }
                }

                await tx.CommitAsync();
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction(nameof(RegisterComplete));
            }
            catch
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, "خطا در ثبت‌نام. لطفاً دوباره تلاش کنید.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult RegisterStep1()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterStep1(RegisterStep1ViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check duplicates
            var existingByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingByEmail != null)
            {
                ModelState.AddModelError("Email", "این ایمیل قبلاً ثبت شده است.");
                return View(model);
            }

            var payload = new RegisterPayload
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Password = model.Password
            };
            TempData["RegisterPayload"] = JsonSerializer.Serialize(payload);
            return RedirectToAction(nameof(RegisterStep2));
        }

        [HttpGet]
        public IActionResult RegisterStep2()
        {
            if (!TempData.ContainsKey("RegisterPayload"))
                return RedirectToAction(nameof(RegisterStep1));

            // Keep TempData for the next request
            TempData.Keep("RegisterPayload");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterStep2(RegisterStep2ViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Keep TempData
                TempData.Keep("RegisterPayload");
                return View(model);
            }

            if (!TempData.TryGetValue("RegisterPayload", out var payloadObj) || payloadObj is not string payloadJson)
            {
                return RedirectToAction(nameof(RegisterStep1));
            }

            var payload = JsonSerializer.Deserialize<RegisterPayload>(payloadJson) ?? new RegisterPayload();
            payload.CompanyName = model.CompanyName.Trim();
            payload.CompanyDescription = model.Description;
            payload.CompanyPhone = model.Phone;

            // Finalize: create Company and User
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var company = new Company
                {
                    Name = payload.CompanyName,
                    Description = payload.CompanyDescription,
                    Phone = payload.CompanyPhone,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                var user = new ApplicationUser
                {
                    UserName = payload.Email, // default to email as username
                    Email = payload.Email,
                    FullName = payload.FullName,
                    CompanyId = company.Id,
                    IsActive = true,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };
                var createRes = await _userManager.CreateAsync(user, payload.Password);
                if (!createRes.Succeeded)
                {
                    foreach (var err in createRes.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);

                    // Rollback company if user creation fails
                    await tx.RollbackAsync();
                    return View(model);
                }

                // Ensure CompanyManager role exists and is assigned (required)
                var cmRole = await _roleManager.FindByNameAsync(Roles.CompanyManager);
                if (cmRole == null)
                {
                    var createRoleRes = await _roleManager.CreateAsync(new ApplicationRole
                    {
                        Name = Roles.CompanyManager,
                        Description = "مدیر شرکت",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    if (!createRoleRes.Succeeded)
                    {
                        foreach (var err in createRoleRes.Errors)
                            ModelState.AddModelError(string.Empty, err.Description);

                        await tx.RollbackAsync();
                        return View(model);
                    }
                    cmRole = await _roleManager.FindByNameAsync(Roles.CompanyManager);
                }

                var addToRoleRes = await _userManager.AddToRoleAsync(user, Roles.CompanyManager);
                if (!addToRoleRes.Succeeded)
                {
                    foreach (var err in addToRoleRes.Errors)
                        ModelState.AddModelError(string.Empty, err.Description);

                    await tx.RollbackAsync();
                    return View(model);
                }

                // Also record in custom UserRoles audit/history table if not exists
                if (cmRole != null)
                {
                    var hasLink = _context.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == cmRole.Id && ur.IsActive);
                    if (!hasLink)
                    {
                        _context.UserRoles.Add(new UserRole
                        {
                            UserId = user.Id,
                            RoleId = cmRole.Id,
                            AssignedAt = DateTime.UtcNow,
                            AssignedById = null,
                            IsActive = true,
                            Notes = "Assigned on registration"
                        });
                        await _context.SaveChangesAsync();
                    }
                }

                await tx.CommitAsync();

                // Sign in and redirect to completion
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction(nameof(RegisterComplete));
            }
            catch
            {
                await tx.RollbackAsync();
                ModelState.AddModelError(string.Empty, "خطا در ثبت‌نام. لطفاً دوباره تلاش کنید.");
                TempData["RegisterPayload"] = JsonSerializer.Serialize(payload);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult RegisterComplete()
        {
            return View();
        }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "نام کاربری یا ایمیل الزامی است")]
        [Display(Name = "نام کاربری یا ایمیل")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "مرا به خاطر بسپار")]
        public bool RememberMe { get; set; }
    }
}