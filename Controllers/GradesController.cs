using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Controllers
{
    [Authorize]
    public class GradesController : Controller
    {
        private readonly TaskManagementContext _ctx;

        public GradesController(TaskManagementContext ctx) => _ctx = ctx;

        public async Task<IActionResult> Index() => View(await _ctx.Grades.AsNoTracking().ToListAsync());

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Grade model)
        {
            if (!ModelState.IsValid) return View(model);
            _ctx.Grades.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var g = await _ctx.Grades.FindAsync(id);
            return g is null ? NotFound() : View(g);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Grade model)
        {
            if (!ModelState.IsValid) return View(model);
            _ctx.Update(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var g = await _ctx.Grades.FindAsync(id);
            return g is null ? NotFound() : View(g);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var g = await _ctx.Grades.FindAsync(id);
            if (g != null) { _ctx.Grades.Remove(g); await _ctx.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }
    }
}