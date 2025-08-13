using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskManagementMvc.Data;
using TaskManagementMvc.Models;

namespace TaskManagementMvc.Controllers
{
    [Authorize]
    public class PerformersController : Controller
    {
        private readonly TaskManagementContext _ctx;

        public PerformersController(TaskManagementContext ctx) => _ctx = ctx;

        public async Task<IActionResult> Index()
            => View(await _ctx.Performers.Include(p => p.Grade).AsNoTracking().ToListAsync());

        public async Task<IActionResult> Create()
        {
            ViewBag.Grades = new SelectList(await _ctx.Grades.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Performer model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Grades = new SelectList(await _ctx.Grades.ToListAsync(), "Id", "Name", model.GradeId);
                return View(model);
            }
            _ctx.Performers.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var p = await _ctx.Performers.FindAsync(id);
            if (p is null) return NotFound();
            ViewBag.Grades = new SelectList(await _ctx.Grades.ToListAsync(), "Id", "Name", p.GradeId);
            return View(p);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Performer model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Grades = new SelectList(await _ctx.Grades.ToListAsync(), "Id", "Name", model.GradeId);
                return View(model);
            }
            _ctx.Update(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var p = await _ctx.Performers.Include(x => x.Grade).FirstOrDefaultAsync(x => x.Id == id);
            return p is null ? NotFound() : View(p);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var p = await _ctx.Performers.FindAsync(id);
            if (p != null) { _ctx.Performers.Remove(p); await _ctx.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index));
        }
    }
}