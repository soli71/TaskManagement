using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Controllers
{
    [Authorize]
    public class GradesController : Controller
    {
        private readonly TaskManagementContext _ctx;
        private readonly UserManager<ApplicationUser> _userManager;

        public GradesController(TaskManagementContext ctx, UserManager<ApplicationUser> userManager)
        {
            _ctx = ctx;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole(Roles.SystemAdmin))
            {
                return View(await _ctx.Grades.Include(g => g.Company).AsNoTracking().ToListAsync());
            }
            else if (user?.CompanyId != null)
            {
                return View(await _ctx.Grades.Include(g => g.Company).Where(g => g.CompanyId == user.CompanyId).AsNoTracking().ToListAsync());
            }
            return View(new List<Grade>());
        }

        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole(Roles.SystemAdmin))
            {
                return BadRequest("شما باید به یک شرکت تخصیص داده شده باشید.");
            }

            if (User.IsInRole(Roles.SystemAdmin))
            {
                ViewBag.Companies = new SelectList(await _ctx.Companies.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
            }
            else
            {
                ViewBag.Companies = new SelectList(await _ctx.Companies.Where(c => c.Id == user.CompanyId && c.IsActive).ToListAsync(), "Id", "Name");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Grade model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole(Roles.SystemAdmin))
            {
                return BadRequest("شما باید به یک شرکت تخصیص داده شده باشید.");
            }

            if (!ModelState.IsValid)
            {
                if (User.IsInRole(Roles.SystemAdmin))
                {
                    ViewBag.Companies = new SelectList(await _ctx.Companies.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
                }
                else
                {
                    ViewBag.Companies = new SelectList(await _ctx.Companies.Where(c => c.Id == user.CompanyId && c.IsActive).ToListAsync(), "Id", "Name");
                }
                return View(model);
            }

            // Set company ID based on user role
            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can choose company
                if (!model.CompanyId.HasValue)
                {
                    ModelState.AddModelError("CompanyId", "لطفاً شرکت را انتخاب کنید.");
                    ViewBag.Companies = new SelectList(await _ctx.Companies.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
                    return View(model);
                }
            }
            else
            {
                // Non-admin users use their assigned company
                model.CompanyId = user.CompanyId;
            }

            _ctx.Grades.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var g = await _ctx.Grades.Include(gr => gr.Company).FirstOrDefaultAsync(gr => gr.Id == id);

            if (g is null) return NotFound();

            // Check if user has access to this grade
            if (!User.IsInRole(Roles.SystemAdmin) && g.CompanyId != user?.CompanyId)
            {
                return Forbid();
            }

            if (User.IsInRole(Roles.SystemAdmin))
            {
                ViewBag.Companies = new SelectList(await _ctx.Companies.Where(c => c.IsActive).ToListAsync(), "Id", "Name", g.CompanyId);
            }
            else
            {
                ViewBag.Companies = new SelectList(await _ctx.Companies.Where(c => c.Id == user.CompanyId && c.IsActive).ToListAsync(), "Id", "Name", g.CompanyId);
            }

            return View(g);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Grade model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.CompanyId == null && !User.IsInRole(Roles.SystemAdmin))
            {
                return BadRequest("شما باید به یک شرکت تخصیص داده شده باشید.");
            }

            if (!ModelState.IsValid)
            {
                if (User.IsInRole(Roles.SystemAdmin))
                {
                    ViewBag.Companies = new SelectList(await _ctx.Companies.Where(c => c.IsActive).ToListAsync(), "Id", "Name", model.CompanyId);
                }
                else
                {
                    ViewBag.Companies = new SelectList(await _ctx.Companies.Where(c => c.Id == user.CompanyId && c.IsActive).ToListAsync(), "Id", "Name", model.CompanyId);
                }
                return View(model);
            }

            // Set company ID based on user role
            if (User.IsInRole(Roles.SystemAdmin))
            {
                // Admin can choose company
                if (!model.CompanyId.HasValue)
                {
                    ModelState.AddModelError("CompanyId", "لطفاً شرکت را انتخاب کنید.");
                    ViewBag.Companies = new SelectList(await _ctx.Companies.Where(c => c.IsActive).ToListAsync(), "Id", "Name", model.CompanyId);
                    return View(model);
                }
            }
            else
            {
                // Non-admin users use their assigned company
                model.CompanyId = user.CompanyId;
            }

            _ctx.Update(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var g = await _ctx.Grades.Include(gr => gr.Company).FirstOrDefaultAsync(gr => gr.Id == id);

            if (g is null) return NotFound();

            // Check if user has access to this grade
            if (!User.IsInRole(Roles.SystemAdmin) && g.CompanyId != user?.CompanyId)
            {
                return Forbid();
            }

            return View(g);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var g = await _ctx.Grades.Include(gr => gr.Company).FirstOrDefaultAsync(gr => gr.Id == id);

            if (g is null) return NotFound();

            // Check if user has access to this grade
            if (!User.IsInRole(Roles.SystemAdmin) && g.CompanyId != user?.CompanyId)
            {
                return Forbid();
            }

            if (g != null)
            {
                _ctx.Grades.Remove(g);
                await _ctx.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}