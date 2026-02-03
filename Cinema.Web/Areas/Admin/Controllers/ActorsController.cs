using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cinema.Infrastructure.Entities;
using System.Threading.Tasks;

namespace Cinema.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class ActorsController : Controller
{
    private readonly AppDbContext _context;

    public ActorsController(AppDbContext context)
    {
        _context = context;
    }

    // ================== HELPERS ==================

    private bool ValidateActor(Actor actor)
    {
        if (actor.Fullname != null)
        {
            actor.Fullname = actor.Fullname.Trim();
        }

        if (string.IsNullOrWhiteSpace(actor.Fullname))
        {
            ModelState.AddModelError(
                nameof(actor.Fullname),
                "Імʼя актора обовʼязкове"
            );
            return false;
        }

        return true;
    }

    // ================== INDEX ==================

    public async Task<IActionResult> Index()
    {
        var actors = await _context.Actors.ToListAsync();
        return View(actors);
    }

    // ================== CREATE (GET) ==================

    public IActionResult Create()
    {
        return View();
    }

    // ================== CREATE (POST) ==================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Actor actor)
    {
        if (!ValidateActor(actor))
        {
            return View(actor);
        }

        try
        {
            _context.Actors.Add(actor);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                nameof(actor.Fullname),
                "Такий актор вже існує"
            );
            return View(actor);
        }

        return RedirectToAction(nameof(Index));
    }

    // ================== EDIT (GET) ==================

    public async Task<IActionResult> Edit(int id)
    {
        var actor = await _context.Actors.FindAsync(id);
        if (actor == null)
            return NotFound();

        return View(actor);
    }

    // ================== EDIT (POST) ==================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Actor actor)
    {
        if (id != actor.Id)
            return BadRequest();

        if (!ValidateActor(actor))
        {
            return View(actor);
        }

        try
        {
            _context.Update(actor);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                nameof(actor.Fullname),
                "Такий актор вже існує"
            );
            return View(actor);
        }

        return RedirectToAction(nameof(Index));
    }

    // ================== DELETE ==================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var actor = await _context.Actors.FindAsync(id);
        if (actor == null)
            return NotFound();

        _context.Actors.Remove(actor);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
